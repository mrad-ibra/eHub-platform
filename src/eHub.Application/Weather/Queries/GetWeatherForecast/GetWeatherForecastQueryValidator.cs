using eHub.Domain.Resources;
using FluentValidation;

namespace eHub.Application.Weather.Queries.GetWeatherForecast;

public sealed class GetWeatherForecastQueryValidator : AbstractValidator<GetWeatherForecastQuery>
{
    public GetWeatherForecastQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 14)
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.WeatherDaysOutOfRange));
    }
}
