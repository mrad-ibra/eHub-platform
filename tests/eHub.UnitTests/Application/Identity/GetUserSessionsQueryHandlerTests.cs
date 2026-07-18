using eHub.Application.Common.Context;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Queries.GetUserSessions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Application.Identity;

public sealed class GetUserSessionsQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public GetUserSessionsQueryHandlerTests()
    {
        _clock.UtcNow.Returns(_now);
        _currentUser.RequireUserId().Returns(_userId);
    }

    [Fact]
    public async Task Handle_MarksCurrentSession()
    {
        var current = RefreshToken.Issue(_userId, "hash-1", _now.AddDays(14), _now, "127.0.0.1", "Chrome");
        var other = RefreshToken.Issue(_userId, "hash-2", _now.AddDays(14), _now.AddMinutes(-5), "10.0.0.1", "Safari");
        _currentUser.SessionId.Returns(current.Id);
        _refreshTokens.ListActiveByUserIdAsync(_userId, _now, Arg.Any<CancellationToken>())
            .Returns([current, other]);

        var handler = new GetUserSessionsQueryHandler(_currentUser, _refreshTokens, _clock);
        var result = await handler.Handle(new GetUserSessionsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(s => s.SessionId == current.Id).IsCurrent.Should().BeTrue();
        result.Single(s => s.SessionId == other.Id).IsCurrent.Should().BeFalse();
        result.Single(s => s.SessionId == current.Id).IpAddress.Should().Be("127.0.0.1");
    }
}
