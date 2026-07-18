using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Identity;

/// <summary>
/// Persisted refresh credential. Only a one-way hash of the raw token is stored.
/// </summary>
public sealed class RefreshToken : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? UserAgent { get; private set; }

    private RefreshToken()
    {
    }

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    public static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime nowUtc,
        string? createdByIp = null,
        string? userAgent = null)
    {
        AppGuard.NotEmpty(userId, nameof(userId));
        AppGuard.NotEmpty(tokenHash, nameof(tokenHash));

        if (expiresAtUtc <= nowUtc)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.RefreshTokenInvalidExpiry));
        }

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash.Trim(),
            ExpiresAtUtc = expiresAtUtc,
            CreatedByIp = NormalizeOptional(createdByIp),
            UserAgent = NormalizeOptional(userAgent)
        };

        token.SetCreatedAudit(nowUtc, userId);
        return token;
    }

    public void EnsureActive(DateTime nowUtc)
    {
        if (IsRevoked)
        {
            throw new AuthenticationFailedException(ErrorResources.Get(ErrorCodes.RefreshTokenRevoked));
        }

        if (IsExpired(nowUtc))
        {
            throw new AuthenticationFailedException(ErrorResources.Get(ErrorCodes.RefreshTokenExpired));
        }
    }

    public void Revoke(DateTime nowUtc, string? revokedByIp = null, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = nowUtc;
        RevokedByIp = NormalizeOptional(revokedByIp);
        ReplacedByTokenHash = NormalizeOptional(replacedByTokenHash);
        SetUpdatedAudit(nowUtc, UserId);
    }

    /// <summary>
    /// Revokes this token and returns a replacement token (rotation).
    /// </summary>
    public RefreshToken Rotate(
        string newTokenHash,
        DateTime newExpiresAtUtc,
        DateTime nowUtc,
        string? ipAddress = null,
        string? userAgent = null)
    {
        EnsureActive(nowUtc);

        var replacement = Issue(
            UserId,
            newTokenHash,
            newExpiresAtUtc,
            nowUtc,
            ipAddress ?? CreatedByIp,
            userAgent ?? UserAgent);

        Revoke(nowUtc, ipAddress, replacement.TokenHash);
        return replacement;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
