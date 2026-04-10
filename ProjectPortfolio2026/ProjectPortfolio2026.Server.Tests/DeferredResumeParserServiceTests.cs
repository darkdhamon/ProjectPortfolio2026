using NUnit.Framework;
using ProjectPortfolio2026.Server.Services.Implementations;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class DeferredResumeParserServiceTests
{
    [Test]
    public void ParseAsync_ThrowsClearError_WhenNoConcreteParserIsRegistered()
    {
        var service = new DeferredResumeParserService();

        using var stream = new MemoryStream([1, 2, 3]);

        var exception = Assert.ThrowsAsync<NotSupportedException>(async () =>
            await service.ParseAsync(stream, "resume.pdf"));

        Assert.That(exception?.Message, Does.Contain("No resume parser implementation is configured yet."));
    }

    [Test]
    public void ParseAsync_RejectsMissingFileName()
    {
        var service = new DeferredResumeParserService();

        using var stream = new MemoryStream([1, 2, 3]);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ParseAsync(stream, ""));
    }
}
