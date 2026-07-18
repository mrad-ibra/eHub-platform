using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.Login;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Application.Identity.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IRefreshTokenRepository refreshTokens,
    IAuthSessionFactory sessionFactory,
    IUnitOfWork unitOfWork,
    IClientContext clientContext,
    IClock clock) : ICommandHandler<ResetPasswordCommand, AuthSessionResult>
{
    public async Task<AuthSessionResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(ErrorResources.Get(ErrorCodes.UserNotFound));
        }

        if (!user.IsActive)
        {
            throw new AuthenticationFailedException(ErrorResources.Get(ErrorCodes.AccountInactive));
        }

        var newHash = passwordHasher.Hash(request.NewPassword);
        if (!user.TryResetPasswordWithToken(request.Token, newHash, clock.UtcNow))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.InvalidPasswordResetLink));
        }

        await refreshTokens.RevokeAllForUserAsync(
            user.Id,
            clock.UtcNow,
            clientContext.IpAddress,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await sessionFactory.CreateAsync(user, cancellationToken);
    }
}
