using NUnit.Framework;
using ProjectPortfolio2026.ResumeParser.Implementations;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class DeferredResumeDocumentParserTests
{
    [Test]
    public void ParseAsync_ThrowsClearError_WhenNoConcreteParserIsRegistered()
    {
        var parser = new DeferredResumeDocumentParser();

        using var stream = new MemoryStream([1, 2, 3]);

        var exception = Assert.ThrowsAsync<NotSupportedException>(async () =>
            await parser.ParseAsync(stream, "resume.pdf"));

        Assert.That(exception?.Message, Does.Contain("No resume document parser implementation is configured yet."));
    }

    [Test]
    public void ParseAsync_RejectsMissingFileName()
    {
        var parser = new DeferredResumeDocumentParser();

        using var stream = new MemoryStream([1, 2, 3]);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await parser.ParseAsync(stream, ""));
    }
}
