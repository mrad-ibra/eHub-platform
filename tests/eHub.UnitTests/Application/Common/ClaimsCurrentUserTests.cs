using System.Security.Claims;
using eHub.Application.Common.Context;
using eHub.Domain.Exceptions;

namespace eHub.UnitTests.Application.Common;

public sealed class ClaimsCurrentUserTests
{
    [Fact]
    public void ReadsIdentityClaims()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sessionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var principal = CreatePrincipal(
            authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "user@ehub.local"),
            new Claim("account_kind", "Personal"),
            new Claim(ClaimsCurrentUser.SessionIdClaimType, sessionId.ToString()),
            new Claim(ClaimTypes.Role, "Customer"),
            new Claim(ClaimTypes.Role, "Host"));

        var current = new ClaimsCurrentUser(principal);

        current.IsAuthenticated.Should().BeTrue();
        current.UserId.Should().Be(userId);
        current.SessionId.Should().Be(sessionId);
        current.Email.Should().Be("user@ehub.local");
        current.AccountKind.Should().Be("Personal");
        current.Roles.Should().BeEquivalentTo(["Customer", "Host"]);
        current.IsInRole("Customer").Should().BeTrue();
        current.IsInRole("Admin").Should().BeFalse();
        current.RequireUserId().Should().Be(userId);
    }

    [Fact]
    public void RequireUserId_WhenAnonymous_Throws()
    {
        var current = new ClaimsCurrentUser(null);

        var act = () => current.RequireUserId();

        act.Should().Throw<AuthenticationFailedException>();
        current.IsAuthenticated.Should().BeFalse();
        current.Roles.Should().BeEmpty();
    }

    private static ClaimsPrincipal CreatePrincipal(bool authenticated, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticated ? "TestAuth" : null);
        return new ClaimsPrincipal(identity);
    }
}
