using System.Security.Claims;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Application.Common.Context;

public sealed class ClaimsCurrentUser : ICurrentUser
{
    public const string SessionIdClaimType = "sid";

    private readonly ClaimsPrincipal? _principal;

    public ClaimsCurrentUser(ClaimsPrincipal? principal)
    {
        _principal = principal;
    }

    public Guid? UserId
    {
        get
        {
            var raw = FindFirst(ClaimTypes.NameIdentifier)
                ?? FindFirst(ClaimTypes.Name)
                ?? FindFirst("sub");

            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public Guid? SessionId
    {
        get
        {
            var raw = FindFirst(SessionIdClaimType);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email
        => FindFirst(ClaimTypes.Email) ?? FindFirst("email");

    public string? AccountKind
        => FindFirst("account_kind");

    public bool IsAuthenticated
        => _principal?.Identity?.IsAuthenticated == true;

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (_principal is null)
            {
                return [];
            }

            return _principal.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || _principal is null)
        {
            return false;
        }

        return _principal.IsInRole(role)
            || Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public Guid RequireUserId()
    {
        if (UserId is not { } userId)
        {
            throw new AuthenticationFailedException(ErrorResources.Get(ErrorCodes.Unauthorized));
        }

        return userId;
    }

    private string? FindFirst(string claimType)
    {
        var value = _principal?.FindFirst(claimType)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
