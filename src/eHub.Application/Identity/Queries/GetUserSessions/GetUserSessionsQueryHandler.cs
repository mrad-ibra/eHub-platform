using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;

namespace eHub.Application.Identity.Queries.GetUserSessions;

public sealed class GetUserSessionsQueryHandler(
    ICurrentUser currentUser,
    IRefreshTokenRepository refreshTokens,
    IClock clock) : IQueryHandler<GetUserSessionsQuery, IReadOnlyList<UserSessionDto>>
{
    public async Task<IReadOnlyList<UserSessionDto>> Handle(
        GetUserSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var currentSessionId = currentUser.SessionId;
        var tokens = await refreshTokens.ListActiveByUserIdAsync(userId, clock.UtcNow, cancellationToken);

        return tokens
            .Select(token => new UserSessionDto(
                token.Id,
                token.CreatedAtUtc,
                token.ExpiresAtUtc,
                token.CreatedByIp,
                token.UserAgent,
                currentSessionId.HasValue && token.Id == currentSessionId.Value))
            .ToArray();
    }
}
