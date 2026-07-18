using eHub.Application.Weather.Queries.GetWeatherForecast;

namespace eHub.UnitTests.Application.Weather;

public sealed class GetWeatherForecastQueryHandlerTests
{
    private readonly GetWeatherForecastQueryHandler _handler = new();

    [Fact]
    public async Task Handle_ReturnsRequestedNumberOfItems()
    {
        var query = new GetWeatherForecastQuery { Days = 3 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Count.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(item => item.Unit == "C");
        result.GeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
