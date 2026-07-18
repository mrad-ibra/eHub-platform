using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.Login;
using eHub.Application.Identity.Commands.ResetPassword;
using eHub.Domain.Exceptions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Application.Identity;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthSessionFactory _sessionFactory = Substitute.For<IAuthSessionFactory>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientContext _clientContext = Substitute.For<IClientContext>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    public ResetPasswordCommandHandlerTests()
    {
        _clock.UtcNow.Returns(_now);
        _clientContext.IpAddress.Returns("127.0.0.1");
        _passwordHasher.Hash("NewPass123!").Returns("new-hash");
    }

    [Fact]
    public async Task Handle_WithValidToken_ResetsPasswordRevokesSessionsAndReturnsSession()
    {
        var user = User.SeedAdmin("admin@ehub.local", "old-hash", "Admin", _now);
        user.SetPasswordResetToken("reset-token", _now.AddHours(1), _now);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _sessionFactory.CreateAsync(user, Arg.Any<CancellationToken>())
            .Returns(new AuthSessionResult(
                user.Id,
                user.Email,
                user.FullName,
                "Admin",
                ["Admin"],
                "access-token",
                _now.AddHours(1),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "refresh-token",
                _now.AddDays(14)));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ResetPasswordCommand(user.Id, "reset-token", "NewPass123!"),
            CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
        user.PasswordResetToken.Should().BeNull();
        result.AccessToken.Should().Be("access-token");
        await _refreshTokens.Received(1).RevokeAllForUserAsync(
            user.Id,
            _now,
            "127.0.0.1",
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidToken_Throws()
    {
        var user = User.SeedAdmin("admin@ehub.local", "old-hash", "Admin", _now);
        user.SetPasswordResetToken("reset-token", _now.AddHours(1), _now);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new ResetPasswordCommand(user.Id, "wrong-token", "NewPass123!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationFailedException>();
    }

    [Fact]
    public async Task Handle_WhenUserMissing_Throws()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new ResetPasswordCommand(Guid.NewGuid(), "token", "NewPass123!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private ResetPasswordCommandHandler CreateHandler()
        => new(
            _users,
            _passwordHasher,
            _refreshTokens,
            _sessionFactory,
            _unitOfWork,
            _clientContext,
            _clock);
}
