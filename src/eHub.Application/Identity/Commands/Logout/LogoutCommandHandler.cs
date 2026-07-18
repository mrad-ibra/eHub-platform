using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;

namespace eHub.Application.Identity.Commands.Logout;

public sealed class LogoutCommandHandler(
    ICurrentUser currentUser,
    IClientContext clientContext,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        if (currentUser.SessionId is { } sessionId)
        {
            var session = await refreshTokens.GetByIdAsync(sessionId, cancellationToken);
            if (session is not null && session.UserId == userId && !session.IsRevoked)
            {
                session.Revoke(clock.UtcNow, clientContext.IpAddress);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        // Access token without sid — revoke all sessions as a safe fallback.
        await refreshTokens.RevokeAllForUserAsync(
            userId,
            clock.UtcNow,
            clientContext.IpAddress,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
