using System.Collections.Concurrent;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Identity;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ConcurrentDictionary<Guid, RefreshToken> _tokens = new();

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _tokens[refreshToken.Id] = refreshToken;
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _tokens.TryGetValue(sessionId, out var token);
        return Task.FromResult(token);
    }

    public Task<IReadOnlyList<RefreshToken>> ListActiveByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RefreshToken> tokens = _tokens.Values
            .Where(token => token.UserId == userId && token.IsActive(nowUtc))
            .OrderByDescending(token => token.CreatedAtUtc)
            .ToArray();

        return Task.FromResult(tokens);
    }

    public Task RevokeAllForUserAsync(
        Guid userId,
        DateTime nowUtc,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var token in _tokens.Values.Where(t => t.UserId == userId && !t.IsRevoked))
        {
            token.Revoke(nowUtc, revokedByIp);
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllForUserExceptAsync(
        Guid userId,
        Guid exceptSessionId,
        DateTime nowUtc,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var token in _tokens.Values.Where(
                     t => t.UserId == userId && t.Id != exceptSessionId && !t.IsRevoked))
        {
            token.Revoke(nowUtc, revokedByIp);
        }

        return Task.CompletedTask;
    }
}
