using eHub.Domain.Identity;

namespace eHub.Application.Identity.Abstractions;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> ListActiveByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(
        Guid userId,
        DateTime nowUtc,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserExceptAsync(
        Guid userId,
        Guid exceptSessionId,
        DateTime nowUtc,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default);
}
