namespace eHub.Application.Identity.Abstractions;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(
        Guid userId,
        string email,
        string accountKind,
        IEnumerable<string> roles,
        Guid? sessionId = null);
}
