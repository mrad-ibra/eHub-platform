using eHub.Application.Weather.Queries.GetWeatherForecast;
using FluentValidation.TestHelper;

namespace eHub.UnitTests.Application.Weather;

public sealed class GetWeatherForecastQueryValidatorTests
{
    private readonly GetWeatherForecastQueryValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(14)]
    public async Task Days_WithinRange_IsValid(int days)
    {
        var result = await _validator.TestValidateAsync(new GetWeatherForecastQuery { Days = days });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(-1)]
    public async Task Days_OutsideRange_IsInvalid(int days)
    {
        var result = await _validator.TestValidateAsync(new GetWeatherForecastQuery { Days = days });

        result.ShouldHaveValidationErrorFor(x => x.Days);
    }
}
