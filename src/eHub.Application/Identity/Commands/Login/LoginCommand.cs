using eHub.Application.Common.Messaging;

namespace eHub.Application.Identity.Commands.Login;

public sealed class LoginCommand : ICommand<AuthSessionResult>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public sealed record AuthSessionResult(
    Guid UserId,
    string Email,
    string FullName,
    string AccountKind,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    Guid SessionId,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
