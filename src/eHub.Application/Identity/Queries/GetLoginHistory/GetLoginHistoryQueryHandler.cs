using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using Microsoft.Extensions.Options;

namespace eHub.Application.Identity.Queries.GetLoginHistory;

public sealed class GetLoginHistoryQueryHandler(
    ICurrentUser currentUser,
    ILoginHistoryRepository history,
    IOptions<AuthOptions> authOptions)
    : IQueryHandler<GetLoginHistoryQuery, IReadOnlyList<LoginHistoryDto>>
{
    public async Task<IReadOnlyList<LoginHistoryDto>> Handle(
        GetLoginHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var options = authOptions.Value.LoginHistory;
        var take = Math.Clamp(
            request.Take ?? options.DefaultTake,
            1,
            Math.Max(1, options.MaxTake));

        var entries = await history.ListByUserIdAsync(userId, take, cancellationToken);

        return entries
            .Select(entry => new LoginHistoryDto(
                entry.Id,
                entry.Email,
                entry.Succeeded,
                entry.FailureReason?.ToString(),
                entry.SessionId,
                entry.IpAddress,
                entry.UserAgent,
                entry.OccurredAtUtc))
            .ToArray();
    }
}
