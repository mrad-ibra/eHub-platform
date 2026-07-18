using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.Login;
using eHub.Application.Identity.Services;
using eHub.Domain.Exceptions;
using eHub.Domain.Identity;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Application.Identity;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAuthSessionFactory _sessionFactory = Substitute.For<IAuthSessionFactory>();
    private readonly ILoginHistoryRecorder _loginHistory = Substitute.For<ILoginHistoryRecorder>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _sessionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Handle_WithValidCredentials_RecordsSuccessAndReturnsSession()
    {
        var user = User.SeedAdmin("admin@ehub.local", "hash", "Admin", _now);
        _users.GetByEmailAsync("admin@ehub.local", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("ChangeMe123!", "hash").Returns(true);
        _sessionFactory.CreateAsync(user, Arg.Any<CancellationToken>())
            .Returns(new AuthSessionResult(
                user.Id,
                user.Email,
                user.FullName,
                "Admin",
                ["Admin"],
                "access-token",
                _now.AddHours(1),
                _sessionId,
                "raw-refresh-token",
                _now.AddDays(14)));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new LoginCommand { Email = "admin@ehub.local", Password = "ChangeMe123!" },
            CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        await _loginHistory.Received(1).RecordSuccessAsync(
            user.Id,
            user.Email,
            _sessionId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_RecordsFailureAndThrows()
    {
        var user = User.SeedAdmin("admin@ehub.local", "hash", "Admin", _now);
        _users.GetByEmailAsync("admin@ehub.local", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", "hash").Returns(false);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new LoginCommand { Email = "admin@ehub.local", Password = "wrong" },
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        await _loginHistory.Received(1).RecordFailureAsync(
            "admin@ehub.local",
            LoginFailureReason.InvalidCredentials,
            user.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_RecordsFailureAndThrows()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, _now);
        _users.GetByEmailAsync("user@ehub.local", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("ChangeMe123!", "hash").Returns(true);

        var handler = CreateHandler(requireConfirmation: true);
        var act = () => handler.Handle(
            new LoginCommand { Email = "user@ehub.local", Password = "ChangeMe123!" },
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        await _loginHistory.Received(1).RecordFailureAsync(
            "user@ehub.local",
            LoginFailureReason.EmailNotConfirmed,
            user.Id,
            Arg.Any<CancellationToken>());
    }

    private LoginCommandHandler CreateHandler(bool requireConfirmation = true)
        => new(
            _users,
            _passwordHasher,
            _sessionFactory,
            _loginHistory,
            Options.Create(new EmailOptions { RequireConfirmation = requireConfirmation }));
}
