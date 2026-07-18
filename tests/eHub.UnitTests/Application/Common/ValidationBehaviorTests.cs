using eHub.Application.Common.Behaviors;
using eHub.Application.Identity.Commands.Login;
using FluentValidation;
using MediatR;

namespace eHub.UnitTests.Application.Common;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenValid_CallsNext()
    {
        var behavior = new ValidationBehavior<LoginCommand, AuthSessionResult>(
            [new LoginCommandValidator()]);

        var nextCalled = false;
        var expected = CreateSessionResult();
        RequestHandlerDelegate<AuthSessionResult> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(expected);
        };

        var result = await behavior.Handle(
            new LoginCommand { Email = "user@ehub.local", Password = "secret1" },
            next,
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_WhenInvalid_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<LoginCommand, AuthSessionResult>(
            [new LoginCommandValidator()]);

        var act = () => behavior.Handle(
            new LoginCommand { Email = "not-an-email", Password = "x" },
            _ => Task.FromResult(CreateSessionResult()),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static AuthSessionResult CreateSessionResult()
        => new(
            Guid.NewGuid(),
            "user@ehub.local",
            "User",
            "Personal",
            ["Customer"],
            "access",
            DateTime.UtcNow.AddHours(1),
            Guid.NewGuid(),
            "refresh",
            DateTime.UtcNow.AddDays(7));
}
