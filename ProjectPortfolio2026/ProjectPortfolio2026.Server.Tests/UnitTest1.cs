using ProjectPortfolio2026.Server.Controllers;

namespace ProjectPortfolio2026.Server.Tests;

public class UnitTest1
{
    [Fact]
    public void Get_ReturnsFiveForecastEntries()
    {
        var controller = new WeatherForecastController();

        var result = controller.Get().ToArray();

        Assert.Equal(5, result.Length);
    }
}
