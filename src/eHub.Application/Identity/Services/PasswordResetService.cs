using eHub.Application.Abstractions.Email;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Domain.Identity;
using Microsoft.Extensions.Options;

namespace eHub.Application.Identity.Services;

public interface IPasswordResetService
{
    Task IssueAndSendAsync(User user, CancellationToken cancellationToken = default);
}

public sealed class PasswordResetService(
    IEmailSender emailSender,
    IClock clock,
    IUnitOfWork unitOfWork,
    IOptions<EmailOptions> emailOptions,
    IOptions<SiteOptions> siteOptions) : IPasswordResetService
{
    public async Task IssueAndSendAsync(User user, CancellationToken cancellationToken = default)
    {
        var options = emailOptions.Value;
        var token = SecureTokenFactory.Create();
        var expiresAt = clock.UtcNow.AddHours(options.PasswordResetTokenExpiryHours);

        user.SetPasswordResetToken(token, expiresAt, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var resetUrl = BuildResetUrl(user.Id, token);
        await emailSender.SendPasswordResetEmailAsync(
            user.Email,
            user.FullName,
            resetUrl,
            cancellationToken);
    }

    private string BuildResetUrl(Guid userId, string token)
    {
        var baseUrl = siteOptions.Value.PublicAppUrl.TrimEnd('/');
        var path = emailOptions.Value.PasswordResetPath.StartsWith('/')
            ? emailOptions.Value.PasswordResetPath
            : "/" + emailOptions.Value.PasswordResetPath;

        return $"{baseUrl}{path}?userId={userId:D}&token={Uri.EscapeDataString(token)}";
    }
}
