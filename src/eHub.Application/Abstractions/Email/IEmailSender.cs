namespace eHub.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendVerificationEmailAsync(
        string toEmail,
        string fullName,
        string verificationUrl,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(
        string toEmail,
        string fullName,
        string resetUrl,
        CancellationToken cancellationToken = default);
}
