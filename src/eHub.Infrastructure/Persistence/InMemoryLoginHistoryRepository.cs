using System.Collections.Concurrent;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Identity;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryLoginHistoryRepository : ILoginHistoryRepository
{
    private readonly ConcurrentBag<LoginHistoryEntry> _entries = [];

    public Task AddAsync(LoginHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LoginHistoryEntry>> ListByUserIdAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);

        IReadOnlyList<LoginHistoryEntry> result = _entries
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Take(take)
            .ToArray();

        return Task.FromResult(result);
    }
}
