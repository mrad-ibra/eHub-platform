using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.ForgotPassword;
using eHub.Application.Identity.Services;
using eHub.Domain.Identity;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Application.Identity;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordResetService _passwordReset = Substitute.For<IPasswordResetService>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_WhenActiveUserExists_SendsResetEmail()
    {
        var user = User.SeedAdmin("admin@ehub.local", "hash", "Admin", _now);
        _users.GetByEmailAsync("admin@ehub.local", Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        await handler.Handle(new ForgotPasswordCommand("admin@ehub.local"), CancellationToken.None);

        await _passwordReset.Received(1).IssueAndSendAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserMissing_DoesNothing()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = CreateHandler();
        await handler.Handle(new ForgotPasswordCommand("missing@ehub.local"), CancellationToken.None);

        await _passwordReset.DidNotReceive().IssueAndSendAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserInactive_DoesNothing()
    {
        var user = User.SeedAdmin("admin@ehub.local", "hash", "Admin", _now);
        user.Deactivate(_now);
        _users.GetByEmailAsync("admin@ehub.local", Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        await handler.Handle(new ForgotPasswordCommand("admin@ehub.local"), CancellationToken.None);

        await _passwordReset.DidNotReceive().IssueAndSendAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private ForgotPasswordCommandHandler CreateHandler()
        => new(
            _users,
            _passwordReset,
            Options.Create(new EmailOptions { Enabled = true }));
}
