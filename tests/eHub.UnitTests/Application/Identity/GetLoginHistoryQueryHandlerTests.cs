using eHub.Application.Common.Context;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Queries.GetLoginHistory;
using eHub.Domain.Identity;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Application.Identity;

public sealed class GetLoginHistoryQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ILoginHistoryRepository _history = Substitute.For<ILoginHistoryRepository>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public GetLoginHistoryQueryHandlerTests()
    {
        _currentUser.RequireUserId().Returns(_userId);
    }

    [Fact]
    public async Task Handle_ReturnsMappedHistory()
    {
        var success = LoginHistoryEntry.SucceededLogin(
            _userId,
            "user@ehub.local",
            Guid.NewGuid(),
            _now,
            "127.0.0.1",
            "Chrome");
        var failure = LoginHistoryEntry.FailedLogin(
            "user@ehub.local",
            LoginFailureReason.InvalidCredentials,
            _now.AddMinutes(-5),
            _userId,
            "10.0.0.1",
            "Safari");

        _history.ListByUserIdAsync(_userId, 50, Arg.Any<CancellationToken>())
            .Returns([success, failure]);

        var handler = new GetLoginHistoryQueryHandler(
            _currentUser,
            _history,
            Options.Create(new AuthOptions()));

        var result = await handler.Handle(new GetLoginHistoryQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Succeeded.Should().BeTrue();
        result[0].IpAddress.Should().Be("127.0.0.1");
        result[1].Succeeded.Should().BeFalse();
        result[1].FailureReason.Should().Be(nameof(LoginFailureReason.InvalidCredentials));
    }

    [Fact]
    public async Task Handle_ClampsTakeToMax()
    {
        _history.ListByUserIdAsync(_userId, 100, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetLoginHistoryQueryHandler(
            _currentUser,
            _history,
            Options.Create(new AuthOptions
            {
                LoginHistory = new LoginHistoryOptions { DefaultTake = 50, MaxTake = 100 }
            }));

        await handler.Handle(new GetLoginHistoryQuery(Take: 500), CancellationToken.None);

        await _history.Received(1).ListByUserIdAsync(_userId, 100, Arg.Any<CancellationToken>());
    }
}
