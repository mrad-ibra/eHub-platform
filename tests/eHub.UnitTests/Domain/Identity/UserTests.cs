using eHub.Domain.Identity;

namespace eHub.UnitTests.Domain.Identity;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Register_CreatesActiveUnconfirmedUser()
    {
        var user = User.Register("Alex@eHub.local", "hash", "Alex Host", AccountKind.Personal, Now);

        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be("alex@ehub.local");
        user.FullName.Should().Be("Alex Host");
        user.AccountKind.Should().Be(AccountKind.Personal);
        user.IsEmailConfirmed.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        user.CreatedAtUtc.Should().Be(Now);
        user.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void RegisterFromGoogle_ConfirmsEmailAndStoresSubject()
    {
        var user = User.RegisterFromGoogle(
            "host@ehub.local",
            "unusable",
            "Host User",
            "google-sub-1",
            "https://cdn.example/avatar.png",
            AccountKind.Business,
            Now);

        user.IsEmailConfirmed.Should().BeTrue();
        user.GoogleSubjectId.Should().Be("google-sub-1");
        user.ProfilePictureUrl.Should().Be("https://cdn.example/avatar.png");
        user.AccountKind.Should().Be(AccountKind.Business);
    }

    [Fact]
    public void TryConfirmEmailWithToken_WhenValid_Confirms()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        user.SetEmailVerificationToken("token-123", Now.AddHours(1), Now);

        var confirmed = user.TryConfirmEmailWithToken("token-123", Now.AddMinutes(10));

        confirmed.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
    }

    [Fact]
    public void TryConfirmEmailWithToken_WhenExpired_ReturnsFalse()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        user.SetEmailVerificationToken("token-123", Now.AddMinutes(-1), Now);

        var confirmed = user.TryConfirmEmailWithToken("token-123", Now);

        confirmed.Should().BeFalse();
        user.IsEmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void TryResetPasswordWithToken_WhenValid_ChangesPasswordAndClearsToken()
    {
        var user = User.Register("user@ehub.local", "old-hash", "User", AccountKind.Personal, Now);
        user.SetPasswordResetToken("reset-123", Now.AddHours(1), Now);

        var reset = user.TryResetPasswordWithToken("reset-123", "new-hash", Now.AddMinutes(5));

        reset.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiresUtc.Should().BeNull();
    }

    [Fact]
    public void TryResetPasswordWithToken_WhenExpired_ReturnsFalse()
    {
        var user = User.Register("user@ehub.local", "old-hash", "User", AccountKind.Personal, Now);
        user.SetPasswordResetToken("reset-123", Now.AddMinutes(-1), Now);

        var reset = user.TryResetPasswordWithToken("reset-123", "new-hash", Now);

        reset.Should().BeFalse();
        user.PasswordHash.Should().Be("old-hash");
    }

    [Fact]
    public void UpdateProfile_UpdatesNameAndPhone()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        var updatedAt = Now.AddMinutes(5);

        user.UpdateProfile("New Name", " +994501112233 ", updatedAt);

        user.FullName.Should().Be("New Name");
        user.Phone.Should().Be("+994501112233");
        user.UpdatedAtUtc.Should().Be(updatedAt);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);

        user.Deactivate(Now.AddSeconds(1));

        user.IsActive.Should().BeFalse();
    }
}
