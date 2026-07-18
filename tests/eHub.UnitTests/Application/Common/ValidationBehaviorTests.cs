using eHub.Application.Common.Behaviors;
using eHub.Application.Weather.Queries.GetWeatherForecast;
using FluentValidation;
using MediatR;

namespace eHub.UnitTests.Application.Common;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenValid_CallsNext()
    {
        var behavior = new ValidationBehavior<GetWeatherForecastQuery, GetWeatherForecastResult>(
            [new GetWeatherForecastQueryValidator()]);

        var nextCalled = false;
        RequestHandlerDelegate<GetWeatherForecastResult> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(new GetWeatherForecastResult
            {
                GeneratedAtUtc = DateTime.UtcNow,
                Count = 1,
                Items = []
            });
        };

        var result = await behavior.Handle(
            new GetWeatherForecastQuery { Days = 5 },
            next,
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenInvalid_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<GetWeatherForecastQuery, GetWeatherForecastResult>(
            [new GetWeatherForecastQueryValidator()]);

        var act = () => behavior.Handle(
            new GetWeatherForecastQuery { Days = 99 },
            _ => Task.FromResult(new GetWeatherForecastResult
            {
                GeneratedAtUtc = DateTime.UtcNow,
                Count = 0,
                Items = []
            }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
