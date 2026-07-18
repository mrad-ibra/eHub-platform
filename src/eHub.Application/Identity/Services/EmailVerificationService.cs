using eHub.Application.Abstractions.Email;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Domain.Identity;
using Microsoft.Extensions.Options;

namespace eHub.Application.Identity.Services;

public interface IEmailVerificationService
{
    Task IssueAndSendAsync(User user, CancellationToken cancellationToken = default);
}

public sealed class EmailVerificationService(
    IEmailSender emailSender,
    IClock clock,
    IUnitOfWork unitOfWork,
    IOptions<EmailOptions> emailOptions,
    IOptions<SiteOptions> siteOptions) : IEmailVerificationService
{
    public async Task IssueAndSendAsync(User user, CancellationToken cancellationToken = default)
    {
        var options = emailOptions.Value;
        var token = SecureTokenFactory.Create();
        var expiresAt = clock.UtcNow.AddHours(options.VerificationTokenExpiryHours);

        user.SetEmailVerificationToken(token, expiresAt, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var verificationUrl = BuildVerificationUrl(user.Id, token);
        await emailSender.SendVerificationEmailAsync(
            user.Email,
            user.FullName,
            verificationUrl,
            cancellationToken);
    }

    private string BuildVerificationUrl(Guid userId, string token)
    {
        var baseUrl = siteOptions.Value.PublicAppUrl.TrimEnd('/');
        var path = emailOptions.Value.VerificationPath.StartsWith('/')
            ? emailOptions.Value.VerificationPath
            : "/" + emailOptions.Value.VerificationPath;

        return $"{baseUrl}{path}?userId={userId:D}&token={Uri.EscapeDataString(token)}";
    }
}
