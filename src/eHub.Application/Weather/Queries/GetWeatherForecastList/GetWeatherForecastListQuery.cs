using eHub.Application.Common.Messaging;

namespace eHub.Application.Weather.Queries.GetWeatherForecastList;

public sealed record GetWeatherForecastListQuery : IQuery<IReadOnlyList<WeatherForecastListItemDto>>;

public sealed class WeatherForecastListItemDto
{
    public required DateOnly Date { get; init; }
    public required int TemperatureC { get; init; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public required string? Summary { get; init; }
}
