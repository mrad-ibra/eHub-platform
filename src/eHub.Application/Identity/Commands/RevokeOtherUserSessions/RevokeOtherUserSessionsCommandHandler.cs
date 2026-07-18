using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Application.Identity.Commands.RevokeOtherUserSessions;

public sealed class RevokeOtherUserSessionsCommandHandler(
    ICurrentUser currentUser,
    IClientContext clientContext,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<RevokeOtherUserSessionsCommand>
{
    public async Task Handle(RevokeOtherUserSessionsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (currentUser.SessionId is not { } currentSessionId)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.SessionIdRequired));
        }

        await refreshTokens.RevokeAllForUserExceptAsync(
            userId,
            currentSessionId,
            clock.UtcNow,
            clientContext.IpAddress,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
