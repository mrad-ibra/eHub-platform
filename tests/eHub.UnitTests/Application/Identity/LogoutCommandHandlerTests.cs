using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.Logout;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Application.Identity;

public sealed class LogoutCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IClientContext _clientContext = Substitute.For<IClientContext>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public LogoutCommandHandlerTests()
    {
        _clock.UtcNow.Returns(_now);
        _currentUser.RequireUserId().Returns(_userId);
        _clientContext.IpAddress.Returns("127.0.0.1");
    }

    [Fact]
    public async Task Handle_WithSessionId_RevokesCurrentSession()
    {
        var session = RefreshToken.Issue(_userId, "hash-1", _now.AddDays(14), _now);
        _currentUser.SessionId.Returns(session.Id);
        _refreshTokens.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new LogoutCommandHandler(_currentUser, _clientContext, _refreshTokens, _unitOfWork, _clock);
        await handler.Handle(new LogoutCommand(), CancellationToken.None);

        session.IsRevoked.Should().BeTrue();
        await _refreshTokens.DidNotReceive().RevokeAllForUserAsync(
            Arg.Any<Guid>(),
            Arg.Any<DateTime>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutSessionId_RevokesAll()
    {
        _currentUser.SessionId.Returns((Guid?)null);

        var handler = new LogoutCommandHandler(_currentUser, _clientContext, _refreshTokens, _unitOfWork, _clock);
        await handler.Handle(new LogoutCommand(), CancellationToken.None);

        await _refreshTokens.Received(1).RevokeAllForUserAsync(
            _userId,
            _now,
            "127.0.0.1",
            Arg.Any<CancellationToken>());
    }
}
