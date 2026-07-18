using eHub.Domain.Identity;

namespace eHub.UnitTests.Domain.Identity;

public sealed class LoginHistoryEntryTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void SucceededLogin_NormalizesEmailAndStoresSession()
    {
        var entry = LoginHistoryEntry.SucceededLogin(
            UserId,
            "Admin@eHub.local",
            SessionId,
            Now,
            " 127.0.0.1 ",
            "Chrome");

        entry.UserId.Should().Be(UserId);
        entry.Email.Should().Be("admin@ehub.local");
        entry.Succeeded.Should().BeTrue();
        entry.FailureReason.Should().BeNull();
        entry.SessionId.Should().Be(SessionId);
        entry.IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public void FailedLogin_StoresReasonWithoutSession()
    {
        var entry = LoginHistoryEntry.FailedLogin(
            "user@ehub.local",
            LoginFailureReason.AccountInactive,
            Now,
            UserId);

        entry.Succeeded.Should().BeFalse();
        entry.FailureReason.Should().Be(LoginFailureReason.AccountInactive);
        entry.SessionId.Should().BeNull();
    }
}
