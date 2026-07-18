using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.Login;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Application.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(
    IUserRepository users,
    IAuthSessionFactory sessionFactory,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<ConfirmEmailCommand, AuthSessionResult>
{
    public async Task<AuthSessionResult> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(ErrorResources.Get(ErrorCodes.UserNotFound));
        }

        if (!user.TryConfirmEmailWithToken(request.Token, clock.UtcNow))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.InvalidVerificationLink));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await sessionFactory.CreateAsync(user, cancellationToken);
    }
}
