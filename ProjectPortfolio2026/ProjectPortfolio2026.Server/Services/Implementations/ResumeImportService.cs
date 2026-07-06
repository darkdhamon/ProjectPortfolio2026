using Microsoft.AspNetCore.Http;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Repositories;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;
using System.Net;
using System.Net.Sockets;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class ResumeImportService(
    IResumeImportFileStore resumeImportFileStore,
    IResumeParserService resumeParserService,
    IResumeConfigurationRepository resumeConfigurationRepository,
    HttpClient httpClient) : IResumeImportService
{
    private const string ConfiguredSourceDownloadFailureMessage = "Unable to download the configured resume source.";
    private const string ConfiguredSourceDownloadTimeoutMessage = "The configured resume source download timed out.";
    private const int MaxConfiguredSourceRedirects = 5;
    private const long MaxConfiguredSourceFileBytes = 10 * 1024 * 1024;
    private const string PublicHostValidationMessage = "Configured resume source URLs must resolve to a public host.";
    private static readonly TimeSpan ConfiguredSourceDownloadTimeout = TimeSpan.FromMinutes(2);
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx"
    };
    private static readonly Dictionary<string, string> ContentTypeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = ".pdf",
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx"
    };

    public static HttpMessageHandler CreateConfiguredSourceHttpMessageHandler()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = ConnectValidatedPublicHostAsync
        };
    }

    public async Task<ResumeImportCandidateResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var stagedFile = await resumeImportFileStore.StageAsync(file, cancellationToken);
        return await ParseStagedFileAsync(stagedFile, cancellationToken);
    }

    public async Task<ResumeImportCandidateResult> ParseConfiguredSourceAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await resumeConfigurationRepository.GetAsync(cancellationToken)
            ?? throw new ResumeImportValidationException("No resume configuration is available.");

        if (!ResumeConfigurationRules.HasCompletePublicConfiguration(configuration))
        {
            throw new ResumeImportValidationException("A complete resume source configuration is required before parsing.");
        }

        if (ResumeSourceTypes.Normalize(configuration.SourceType) != ResumeSourceTypes.HostedFile)
        {
            throw new ResumeImportValidationException("Only hosted file resume sources can be parsed from the configured source workflow.");
        }

        var sourceUrl = configuration.SourceUrl?.Trim();
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri))
        {
            throw new ResumeImportValidationException("The configured resume source URL is invalid.");
        }

        if (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ResumeImportValidationException("Only http and https configured resume source URLs are supported.");
        }

        await using var stagedFile = await DownloadConfiguredSourceAsync(sourceUri, cancellationToken);
        return await ParseStagedFileAsync(stagedFile, cancellationToken);
    }

    private async Task<ResumeImportCandidateResult> ParseStagedFileAsync(
        StagedResumeFile stagedFile,
        CancellationToken cancellationToken)
    {
        await using var content = File.OpenRead(stagedFile.StoredFilePath);

        var result = await resumeParserService.ParseAsync(content, stagedFile.OriginalFileName, cancellationToken);
        result.SourceFileName ??= stagedFile.OriginalFileName;

        return ResumeImportCandidateNormalizer.Normalize(result);
    }

    private async Task<StagedResumeFile> DownloadConfiguredSourceAsync(Uri sourceUri, CancellationToken cancellationToken)
    {
        using var downloadTimeoutCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        downloadTimeoutCancellationSource.CancelAfter(ConfiguredSourceDownloadTimeout);
        var downloadCancellationToken = downloadTimeoutCancellationSource.Token;

        try
        {
            return await DownloadConfiguredSourceCoreAsync(sourceUri, downloadCancellationToken);
        }
        catch (ResumeImportValidationException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            throw new ResumeImportValidationException(ConfiguredSourceDownloadTimeoutMessage, exception);
        }
        catch (HttpRequestException exception) when (string.Equals(exception.Message, PublicHostValidationMessage, StringComparison.Ordinal))
        {
            throw new ResumeImportValidationException(PublicHostValidationMessage, exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or SocketException)
        {
            throw new ResumeImportValidationException(ConfiguredSourceDownloadFailureMessage, exception);
        }
    }

    private async Task<StagedResumeFile> DownloadConfiguredSourceCoreAsync(Uri sourceUri, CancellationToken cancellationToken)
    {
        var currentUri = sourceUri;

        for (var redirectCount = 0; redirectCount <= MaxConfiguredSourceRedirects; redirectCount++)
        {
            await EnsurePublicConfiguredSourceHostAsync(currentUri, cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (IsRedirectStatusCode(response.StatusCode))
            {
                if (redirectCount == MaxConfiguredSourceRedirects)
                {
                    throw new ResumeImportValidationException("The configured resume source redirected too many times.");
                }

                currentUri = GetRedirectUri(currentUri, response.Headers.Location);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ResumeImportValidationException($"The configured resume source returned {(int)response.StatusCode}.");
            }

            var fileName = GetConfiguredSourceFileName(currentUri, response);
            return await StageConfiguredSourceDownloadAsync(fileName, response, cancellationToken);
        }

        throw new ResumeImportValidationException("The configured resume source redirected too many times.");
    }

    private static async Task EnsurePublicConfiguredSourceHostAsync(Uri sourceUri, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(sourceUri.DnsSafeHost, out var literalAddress))
        {
            if (IsPrivateOrReservedAddress(literalAddress))
            {
                throw new ResumeImportValidationException(PublicHostValidationMessage);
            }

            return;
        }

        var resolvedAddresses = await Dns.GetHostAddressesAsync(sourceUri.DnsSafeHost, cancellationToken);
        if (resolvedAddresses.Length == 0 || resolvedAddresses.Any(IsPrivateOrReservedAddress))
        {
            throw new ResumeImportValidationException(PublicHostValidationMessage);
        }
    }

    private async Task<StagedResumeFile> StageConfiguredSourceDownloadAsync(
        string fileName,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var normalizedFileName = ExtractFileName(fileName);
        if (string.IsNullOrWhiteSpace(normalizedFileName))
        {
            throw new ResumeImportValidationException("A resume file is required.");
        }

        var extension = Path.GetExtension(normalizedFileName);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new ResumeImportValidationException("Only PDF and DOCX resume files are supported.");
        }

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength is <= 0)
        {
            throw new ResumeImportValidationException("The configured resume source file is empty.");
        }

        if (contentLength > MaxConfiguredSourceFileBytes)
        {
            throw new ResumeImportValidationException("The configured resume source exceeds the 10 MB download limit.");
        }

        var stagingRootPath = Path.Combine(Path.GetTempPath(), "ProjectPortfolio2026", "resume-import-downloads");
        Directory.CreateDirectory(stagingRootPath);

        var stagedFilePath = Path.Combine(
            stagingRootPath,
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");

        long totalBytes = 0;

        try
        {
            await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var targetStream = new FileStream(
                stagedFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            var buffer = new byte[81920];
            while (true)
            {
                var bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytes += bytesRead;
                if (totalBytes > MaxConfiguredSourceFileBytes)
                {
                    throw new ResumeImportValidationException("The configured resume source exceeds the 10 MB download limit.");
                }

                await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }
        }
        catch
        {
            if (File.Exists(stagedFilePath))
            {
                File.Delete(stagedFilePath);
            }

            throw;
        }

        if (totalBytes <= 0)
        {
            if (File.Exists(stagedFilePath))
            {
                File.Delete(stagedFilePath);
            }

            throw new ResumeImportValidationException("The configured resume source file is empty.");
        }

        return new StagedResumeFile(
            stagedFilePath,
            normalizedFileName,
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            totalBytes);
    }

    private static string GetConfiguredSourceFileName(Uri sourceUri, HttpResponseMessage response)
    {
        var contentDisposition = response.Content.Headers.ContentDisposition;
        var dispositionFileName = NormalizeContentDispositionFileName(contentDisposition?.FileNameStar)
            ?? NormalizeContentDispositionFileName(contentDisposition?.FileName);
        var dispositionCandidate = ExtractFileName(dispositionFileName ?? string.Empty);
        if (HasSupportedExtension(dispositionCandidate))
        {
            return dispositionCandidate;
        }

        var path = sourceUri.AbsolutePath;
        var lastSegment = ExtractFileName(Path.GetFileName(path));
        if (HasSupportedExtension(lastSegment))
        {
            return lastSegment;
        }

        var inferredExtension = GetSupportedExtensionForContentType(response.Content.Headers.ContentType?.MediaType);
        if (inferredExtension is not null)
        {
            var baseFileName = Path.GetFileNameWithoutExtension(string.IsNullOrWhiteSpace(dispositionCandidate) ? lastSegment : dispositionCandidate);
            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                baseFileName = "configured-resume";
            }

            return $"{baseFileName}{inferredExtension}";
        }

        return string.IsNullOrWhiteSpace(lastSegment)
            ? "configured-resume"
            : lastSegment;
    }

    private static string? NormalizeContentDispositionFileName(string? value)
    {
        var trimmedValue = value?.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            return null;
        }

        var encodedMarkerIndex = trimmedValue.IndexOf("''", StringComparison.Ordinal);
        if (encodedMarkerIndex >= 0)
        {
            return Uri.UnescapeDataString(trimmedValue[(encodedMarkerIndex + 2)..]);
        }

        return trimmedValue;
    }

    private static Uri GetRedirectUri(Uri currentUri, Uri? redirectUri)
    {
        if (redirectUri is null)
        {
            throw new ResumeImportValidationException("The configured resume source returned a redirect without a destination.");
        }

        var nextUri = redirectUri.IsAbsoluteUri
            ? redirectUri
            : new Uri(currentUri, redirectUri);

        if (nextUri.Scheme != Uri.UriSchemeHttp && nextUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ResumeImportValidationException("Only http and https configured resume source URLs are supported.");
        }

        return nextUri;
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.RedirectKeepVerb
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
    }

    private static bool IsPrivateOrReservedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                return IsPrivateOrReservedAddress(address.MapToIPv4());
            }

            if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal)
            {
                return true;
            }

            var ipv6Bytes = address.GetAddressBytes();
            return address.Equals(IPAddress.IPv6None)
                || address.Equals(IPAddress.IPv6Any)
                || IsIPv6DocumentationAddress(ipv6Bytes)
                || (ipv6Bytes[0] & 0xFE) == 0xFC;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 0
            || bytes[0] == 10
            || bytes[0] == 127
            || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
            || (bytes[0] == 169 && bytes[1] == 254)
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 0 && (bytes[2] == 0 || bytes[2] == 2 || (bytes[2] == 88 && bytes[3] == 99)))
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19))
            || (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100)
            || (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113)
            || bytes[0] >= 224;
    }

    private static bool HasSupportedExtension(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName)
            && SupportedExtensions.Contains(Path.GetExtension(fileName));
    }

    private static string? GetSupportedExtensionForContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? null
            : ContentTypeExtensions.GetValueOrDefault(contentType);
    }

    private static string ExtractFileName(string fileName)
    {
        var lastSeparatorIndex = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
        return lastSeparatorIndex >= 0
            ? fileName[(lastSeparatorIndex + 1)..]
            : fileName;
    }

    private static async ValueTask<Stream> ConnectValidatedPublicHostAsync(
        SocketsHttpConnectionContext connectionContext,
        CancellationToken cancellationToken)
    {
        var port = connectionContext.DnsEndPoint.Port;
        var host = connectionContext.DnsEndPoint.Host;

        if (IPAddress.TryParse(host, out var literalAddress))
        {
            if (IsPrivateOrReservedAddress(literalAddress))
            {
                throw new HttpRequestException(PublicHostValidationMessage);
            }

            return await ConnectToAddressAsync(literalAddress, port, cancellationToken);
        }

        var resolvedAddresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        var publicAddresses = resolvedAddresses
            .Where(address => !IsPrivateOrReservedAddress(address))
            .ToArray();

        if (publicAddresses.Length == 0)
        {
            throw new HttpRequestException(PublicHostValidationMessage);
        }

        Exception? lastConnectionException = null;
        foreach (var address in publicAddresses)
        {
            try
            {
                return await ConnectToAddressAsync(address, port, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is SocketException or IOException)
            {
                lastConnectionException = exception;
            }
        }

        throw new HttpRequestException(ConfiguredSourceDownloadFailureMessage, lastConnectionException);
    }

    private static async ValueTask<Stream> ConnectToAddressAsync(
        IPAddress address,
        int port,
        CancellationToken cancellationToken)
    {
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            await socket.ConnectAsync(address, port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsIPv6DocumentationAddress(byte[] addressBytes)
    {
        return addressBytes.Length >= 4
            && addressBytes[0] == 0x20
            && addressBytes[1] == 0x01
            && addressBytes[2] == 0x0D
            && addressBytes[3] == 0xB8;
    }
}
