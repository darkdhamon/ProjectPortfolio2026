using NUnit.Framework;
using ProjectPortfolio2026.Server.Services.Implementations;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class DeferredResumeParserServiceTests
{
    [Test]
    public async Task ParseAsync_ReturnsPlaceholderResultWithoutThrowing()
    {
        var service = new DeferredResumeParserService();

        using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.ParseAsync(stream, "resume.pdf");

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceFileName, Is.EqualTo("resume.pdf"));
            Assert.That(result.ParserName, Is.EqualTo(nameof(DeferredResumeParserService)));
            Assert.That(result.Person, Is.Null);
            Assert.That(result.WorkHistory, Is.Empty);
        });
    }
}
