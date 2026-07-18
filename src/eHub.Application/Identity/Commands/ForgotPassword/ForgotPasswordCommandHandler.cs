using eHub.Application.Common.Messaging;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Services;
using Microsoft.Extensions.Options;

namespace eHub.Application.Identity.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IPasswordResetService passwordReset,
    IOptions<EmailOptions> emailOptions) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!emailOptions.Value.Enabled)
        {
            return;
        }

        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            // Silent success — do not leak whether the email exists.
            return;
        }

        await passwordReset.IssueAndSendAsync(user, cancellationToken);
    }
}
