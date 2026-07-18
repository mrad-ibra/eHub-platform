using eHub.Application.Common.Messaging;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Services;
using eHub.Domain.Exceptions;
using eHub.Domain.Identity;
using eHub.Domain.Resources;
using Microsoft.Extensions.Options;

namespace eHub.Application.Identity.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAuthSessionFactory sessionFactory,
    ILoginHistoryRecorder loginHistory,
    IOptions<EmailOptions> emailOptions)
    : ICommandHandler<LoginCommand, AuthSessionResult>
{
    public async Task<AuthSessionResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await loginHistory.RecordFailureAsync(
                email,
                LoginFailureReason.InvalidCredentials,
                user?.Id,
                cancellationToken);
            throw new AuthenticationFailedException(ErrorResources.Get(ErrorCodes.InvalidCredentials));
        }

        if (!user.IsActive)
        {
            await loginHistory.RecordFailureAsync(
                email,
                LoginFailureReason.AccountInactive,
                user.Id,
                cancellationToken);
            throw new AuthenticationFailedException(ErrorResources.Get(ErrorCodes.AccountInactive));
        }

        if (emailOptions.Value.RequireConfirmation && !user.IsEmailConfirmed)
        {
            await loginHistory.RecordFailureAsync(
                email,
                LoginFailureReason.EmailNotConfirmed,
                user.Id,
                cancellationToken);
            throw new AuthenticationFailedException(ErrorResources.Get(ErrorCodes.EmailNotConfirmed));
        }

        var session = await sessionFactory.CreateAsync(user, cancellationToken);
        await loginHistory.RecordSuccessAsync(
            user.Id,
            user.Email,
            session.SessionId,
            cancellationToken);

        return session;
    }
}
