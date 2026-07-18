using eHub.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace eHub.Infrastructure.Email;

/// <summary>Dev fallback — logs email links when SMTP is not configured.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendVerificationEmailAsync(
        string toEmail,
        string fullName,
        string verificationUrl,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Email delivery (log-only) — verification link for {Email} ({Name}): {Url}",
            toEmail,
            fullName,
            verificationUrl);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string fullName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Email delivery (log-only) — password reset link for {Email} ({Name}): {Url}",
            toEmail,
            fullName,
            resetUrl);
        return Task.CompletedTask;
    }
}
