using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.RevokeUserSession;
using eHub.Domain.Exceptions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Application.Identity;

public sealed class RevokeUserSessionCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IClientContext _clientContext = Substitute.For<IClientContext>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public RevokeUserSessionCommandHandlerTests()
    {
        _clock.UtcNow.Returns(_now);
        _currentUser.RequireUserId().Returns(_userId);
        _clientContext.IpAddress.Returns("127.0.0.1");
    }

    [Fact]
    public async Task Handle_RevokesOwnSession()
    {
        var session = RefreshToken.Issue(_userId, "hash-1", _now.AddDays(14), _now);
        _refreshTokens.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = CreateHandler();
        await handler.Handle(new RevokeUserSessionCommand(session.Id), CancellationToken.None);

        session.IsRevoked.Should().BeTrue();
        session.RevokedByIp.Should().Be("127.0.0.1");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToOtherUser_Throws()
    {
        var otherUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var session = RefreshToken.Issue(otherUserId, "hash-1", _now.AddDays(14), _now);
        _refreshTokens.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = CreateHandler();
        var act = () => handler.Handle(new RevokeUserSessionCommand(session.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private RevokeUserSessionCommandHandler CreateHandler()
        => new(_currentUser, _clientContext, _refreshTokens, _unitOfWork, _clock);
}
