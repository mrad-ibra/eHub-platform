using eHub.Domain.Common;

namespace eHub.Domain.Identity;

/// <summary>
/// Immutable record of a login attempt (success or failure) for security auditing.
/// </summary>
public sealed class LoginHistoryEntry : Entity
{
    public Guid? UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public bool Succeeded { get; private set; }
    public LoginFailureReason? FailureReason { get; private set; }
    public Guid? SessionId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private LoginHistoryEntry()
    {
    }

    public static LoginHistoryEntry SucceededLogin(
        Guid userId,
        string email,
        Guid sessionId,
        DateTime occurredAtUtc,
        string? ipAddress = null,
        string? userAgent = null)
    {
        AppGuard.NotEmpty(userId, nameof(userId));
        AppGuard.NotEmpty(sessionId, nameof(sessionId));

        return Create(
            userId,
            email,
            succeeded: true,
            failureReason: null,
            sessionId,
            occurredAtUtc,
            ipAddress,
            userAgent);
    }

    public static LoginHistoryEntry FailedLogin(
        string email,
        LoginFailureReason failureReason,
        DateTime occurredAtUtc,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return Create(
            userId,
            email,
            succeeded: false,
            failureReason,
            sessionId: null,
            occurredAtUtc,
            ipAddress,
            userAgent);
    }

    private static LoginHistoryEntry Create(
        Guid? userId,
        string email,
        bool succeeded,
        LoginFailureReason? failureReason,
        Guid? sessionId,
        DateTime occurredAtUtc,
        string? ipAddress,
        string? userAgent)
    {
        var normalizedEmail = AppGuard.NotEmpty(email, nameof(email)).Trim().ToLowerInvariant();

        return new LoginHistoryEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = normalizedEmail,
            Succeeded = succeeded,
            FailureReason = failureReason,
            SessionId = sessionId,
            IpAddress = NormalizeOptional(ipAddress),
            UserAgent = NormalizeOptional(userAgent),
            OccurredAtUtc = occurredAtUtc
        };
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
