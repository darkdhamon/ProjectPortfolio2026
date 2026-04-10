using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ProjectPortfolio2026.ResumeParser.Interfaces;
using UglyToad.PdfPig;

namespace ProjectPortfolio2026.ResumeParser.Implementations;

internal sealed class DocxResumeTextExtractor : IResumeTextExtractor
{
    public bool CanExtract(string fileName)
    {
        return Path.GetExtension(fileName).Equals(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Seek(0, SeekOrigin.Begin);

        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null)
        {
            return string.Empty;
        }

        using var entryStream = entry.Open();
        var document = XDocument.Load(entryStream);
        XNamespace wordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var paragraphTexts = document
            .Descendants(wordNamespace + "p")
            .Select(paragraph => string.Concat(paragraph.Descendants(wordNamespace + "t").Select(node => node.Value)))
            .ToList();

        return string.Join(Environment.NewLine, paragraphTexts);
    }
}

internal sealed class PdfResumeTextExtractor : IResumeTextExtractor
{
    public bool CanExtract(string fileName)
    {
        return Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Seek(0, SeekOrigin.Begin);

        byte[] rawBytes = buffer.ToArray();
        using var document = PdfDocument.Open(buffer);
        var pageText = document.GetPages()
            .Select(page => page.Text)
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        if (pageText.Count > 0)
        {
            return string.Join(Environment.NewLine + Environment.NewLine, pageText);
        }

        var rawPdf = Encoding.ASCII.GetString(rawBytes);
        var extracted = ResumeParsingUtilities.ExtractPdfLiteralText(rawPdf);
        if (string.IsNullOrWhiteSpace(extracted))
        {
            throw new NotSupportedException("The PDF did not expose readable text through the configured PDF extraction paths.");
        }

        return extracted;
    }
}

internal sealed class PlainTextResumeTextExtractor : IResumeTextExtractor
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md"
    };

    public bool CanExtract(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return TextExtensions.Contains(extension) || string.IsNullOrEmpty(extension);
    }

    public async Task<string> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Seek(0, SeekOrigin.Begin);

        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(extension) && !TextExtensions.Contains(extension))
        {
            throw new NotSupportedException($"The resume parser does not support '{extension}' files yet.");
        }

        if (string.IsNullOrEmpty(extension) && !ResumeParsingUtilities.LooksLikeUtf8TextContent(buffer))
        {
            throw new NotSupportedException("The resume parser only accepts extensionless inputs when they contain valid UTF-8 text.");
        }

        return ResumeParsingUtilities.ReadTextBuffer(buffer);
    }
}
