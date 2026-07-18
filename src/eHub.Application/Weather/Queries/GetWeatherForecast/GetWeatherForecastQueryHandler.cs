using eHub.Application.Common.Messaging;

namespace eHub.Application.Weather.Queries.GetWeatherForecast;

public sealed class GetWeatherForecastQueryHandler
    : IQueryHandler<GetWeatherForecastQuery, GetWeatherForecastResult>
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public Task<GetWeatherForecastResult> Handle(
        GetWeatherForecastQuery request,
        CancellationToken cancellationToken)
    {
        var items = Enumerable.Range(1, request.Days)
            .Select(index => new WeatherForecastDto
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                Unit = "C"
            })
            .ToArray();

        var result = new GetWeatherForecastResult
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Count = items.Length,
            Items = items
        };

        return Task.FromResult(result);
    }
}
