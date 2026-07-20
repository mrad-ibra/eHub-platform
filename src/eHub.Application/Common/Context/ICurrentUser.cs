namespace eHub.Application.Common.Context;

/// <summary>
/// Authenticated caller identity derived from the request principal (JWT claims).
/// Keeps handlers/controllers free of <c>ClaimsPrincipal</c> details.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? SessionId { get; }
    string? Email { get; }
    string? AccountKind { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string role);
    bool HasPermission(string permission);
    Guid RequireUserId();
}
