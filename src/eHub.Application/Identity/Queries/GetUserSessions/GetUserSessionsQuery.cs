using eHub.Application.Common.Messaging;

namespace eHub.Application.Identity.Queries.GetUserSessions;

public sealed record GetUserSessionsQuery : IQuery<IReadOnlyList<UserSessionDto>>;

public sealed record UserSessionDto(
    Guid SessionId,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    string? IpAddress,
    string? UserAgent,
    bool IsCurrent);
