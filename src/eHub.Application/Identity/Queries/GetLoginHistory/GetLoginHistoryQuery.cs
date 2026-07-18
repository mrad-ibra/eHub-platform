using eHub.Application.Common.Messaging;

namespace eHub.Application.Identity.Queries.GetLoginHistory;

public sealed record GetLoginHistoryQuery(int? Take = null) : IQuery<IReadOnlyList<LoginHistoryDto>>;

public sealed record LoginHistoryDto(
    Guid Id,
    string Email,
    bool Succeeded,
    string? FailureReason,
    Guid? SessionId,
    string? IpAddress,
    string? UserAgent,
    DateTime OccurredAtUtc);
