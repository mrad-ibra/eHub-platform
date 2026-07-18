using eHub.Application.Common.Messaging;

namespace eHub.Application.Weather.Queries.GetWeatherForecastList;

public sealed class GetWeatherForecastListQueryHandler
    : IQueryHandler<GetWeatherForecastListQuery, IReadOnlyList<WeatherForecastListItemDto>>
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public Task<IReadOnlyList<WeatherForecastListItemDto>> Handle(
        GetWeatherForecastListQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WeatherForecastListItemDto> result = Enumerable.Range(1, 5)
            .Select(index => new WeatherForecastListItemDto
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

        return Task.FromResult(result);
    }
}
