using eHub.Application.Common.Context;

namespace eHub.Api.Common;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly Lazy<ClaimsCurrentUser> _inner = new(
        () => new ClaimsCurrentUser(httpContextAccessor.HttpContext?.User));

    private ClaimsCurrentUser Inner => _inner.Value;

    public Guid? UserId => Inner.UserId;
    public Guid? SessionId => Inner.SessionId;
    public string? Email => Inner.Email;
    public string? AccountKind => Inner.AccountKind;
    public bool IsAuthenticated => Inner.IsAuthenticated;
    public IReadOnlyList<string> Roles => Inner.Roles;

    public bool IsInRole(string role) => Inner.IsInRole(role);

    public bool HasPermission(string permission) => Inner.HasPermission(permission);

    public Guid RequireUserId() => Inner.RequireUserId();
}
