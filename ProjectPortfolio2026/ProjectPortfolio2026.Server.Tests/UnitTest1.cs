using NUnit.Framework;
using ProjectPortfolio2026.Server.Controllers;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public class UnitTest1
{
    [Test]
    public void Get_ReturnsFiveForecastEntries()
    {
        var controller = new WeatherForecastController();

        var result = controller.Get().ToArray();

        Assert.That(result, Has.Length.EqualTo(5));
    }
}
