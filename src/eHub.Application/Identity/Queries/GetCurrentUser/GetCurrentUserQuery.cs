using eHub.Application.Common.Messaging;

namespace eHub.Application.Identity.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string AccountKind,
    bool IsEmailConfirmed,
    string? ProfilePictureUrl,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc);
