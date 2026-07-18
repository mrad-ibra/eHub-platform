using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.ResendEmailVerification;
using eHub.Application.Identity.Services;
using eHub.Domain.Identity;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Application.Identity;

public sealed class ResendEmailVerificationCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmailVerificationService _emailVerification = Substitute.For<IEmailVerificationService>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_WhenUnconfirmedUserExists_SendsEmail()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, _now);
        _users.GetByEmailAsync("user@ehub.local", Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        await handler.Handle(new ResendEmailVerificationCommand("user@ehub.local"), CancellationToken.None);

        await _emailVerification.Received(1).IssueAndSendAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserMissing_DoesNothing()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = CreateHandler();
        await handler.Handle(new ResendEmailVerificationCommand("missing@ehub.local"), CancellationToken.None);

        await _emailVerification.DidNotReceive().IssueAndSendAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_DoesNothing()
    {
        var user = User.SeedAdmin("admin@ehub.local", "hash", "Admin", _now);
        _users.GetByEmailAsync("admin@ehub.local", Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        await handler.Handle(new ResendEmailVerificationCommand("admin@ehub.local"), CancellationToken.None);

        await _emailVerification.DidNotReceive().IssueAndSendAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private ResendEmailVerificationCommandHandler CreateHandler()
        => new(
            _users,
            _emailVerification,
            Options.Create(new EmailOptions { Enabled = true }));
}
