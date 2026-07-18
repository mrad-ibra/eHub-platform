using eHub.Domain.Exceptions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Domain.Identity;

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Issue_CreatesActiveToken()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", Now.AddDays(14), Now, "127.0.0.1", "tests");

        token.UserId.Should().Be(UserId);
        token.TokenHash.Should().Be("hash-1");
        token.IsActive(Now).Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        token.CreatedByIp.Should().Be("127.0.0.1");
    }

    [Fact]
    public void Issue_WhenExpiryNotInFuture_Throws()
    {
        var act = () => RefreshToken.Issue(UserId, "hash-1", Now, Now);

        act.Should().Throw<ValidationFailedException>();
    }

    [Fact]
    public void EnsureActive_WhenExpired_Throws()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", Now.AddMinutes(1), Now);

        var act = () => token.EnsureActive(Now.AddMinutes(2));

        act.Should().Throw<AuthenticationFailedException>();
    }

    [Fact]
    public void Rotate_RevokesCurrentAndReturnsReplacement()
    {
        var current = RefreshToken.Issue(UserId, "hash-old", Now.AddDays(14), Now);

        var next = current.Rotate("hash-new", Now.AddDays(14), Now.AddMinutes(1), "10.0.0.1");

        current.IsRevoked.Should().BeTrue();
        current.ReplacedByTokenHash.Should().Be("hash-new");
        current.RevokedByIp.Should().Be("10.0.0.1");
        next.TokenHash.Should().Be("hash-new");
        next.IsActive(Now.AddMinutes(1)).Should().BeTrue();
        next.UserId.Should().Be(UserId);
    }

    [Fact]
    public void Rotate_WhenAlreadyRevoked_Throws()
    {
        var current = RefreshToken.Issue(UserId, "hash-old", Now.AddDays(14), Now);
        current.Revoke(Now.AddSeconds(1));

        var act = () => current.Rotate("hash-new", Now.AddDays(14), Now.AddMinutes(1));

        act.Should().Throw<AuthenticationFailedException>();
    }
}
