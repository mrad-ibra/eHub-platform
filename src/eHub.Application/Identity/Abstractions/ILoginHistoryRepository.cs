using eHub.Domain.Identity;

namespace eHub.Application.Identity.Abstractions;

public interface ILoginHistoryRepository
{
    Task AddAsync(LoginHistoryEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoginHistoryEntry>> ListByUserIdAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default);
}
