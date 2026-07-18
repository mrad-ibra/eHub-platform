using eHub.Application.Common.Persistence;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
