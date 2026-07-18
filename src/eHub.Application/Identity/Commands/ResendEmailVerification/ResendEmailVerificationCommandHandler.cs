using eHub.Application.Common.Messaging;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Services;
using Microsoft.Extensions.Options;

namespace eHub.Application.Identity.Commands.ResendEmailVerification;

public sealed class ResendEmailVerificationCommandHandler(
    IUserRepository users,
    IEmailVerificationService emailVerification,
    IOptions<EmailOptions> emailOptions) : ICommandHandler<ResendEmailVerificationCommand>
{
    public async Task Handle(ResendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        if (!emailOptions.Value.Enabled)
        {
            return;
        }

        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || user.IsEmailConfirmed)
        {
            // Silent success — do not leak whether the email exists.
            return;
        }

        await emailVerification.IssueAndSendAsync(user, cancellationToken);
    }
}
