using eHub.Application.Common.Messaging;

namespace eHub.Application.Weather.Queries.GetWeatherForecast;

public sealed class GetWeatherForecastQuery : IQuery<GetWeatherForecastResult>
{
    public int Days { get; init; } = 5;
}

public sealed class GetWeatherForecastResult
{
    public required DateTime GeneratedAtUtc { get; init; }
    public required int Count { get; init; }
    public required IReadOnlyList<WeatherForecastDto> Items { get; init; }
}

public sealed class WeatherForecastDto
{
    public required DateOnly Date { get; init; }
    public required int TemperatureC { get; init; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public required string? Summary { get; init; }
    public required string Unit { get; init; }
}
