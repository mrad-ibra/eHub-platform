using System.Collections.Concurrent;
using eHub.Application.Catalog.Abstractions;
using eHub.Domain.Catalog;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryCatalogStore : ICatalogStore
{
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, CatalogEntity>> _bags = new();

    public Task AddAsync<T>(T item, CancellationToken cancellationToken = default)
        where T : CatalogEntity
    {
        var bag = BagFor<T>();
        if (!bag.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException($"Catalog item '{item.Code}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<T>> ListAsync<T>(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
        where T : CatalogEntity
    {
        IReadOnlyList<T> items = BagFor<T>().Values
            .OfType<T>()
            .Where(x => !x.IsDeleted)
            .Where(x => !activeOnly || x.IsActive)
            .ToArray();

        return Task.FromResult(items);
    }

    public Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default)
        where T : CatalogEntity
    {
        if (!BagFor<T>().TryGetValue(id, out var entity) || entity.IsDeleted)
        {
            return Task.FromResult<T?>(null);
        }

        return Task.FromResult(entity as T);
    }

    public Task<T?> GetByCodeAsync<T>(string code, CancellationToken cancellationToken = default)
        where T : CatalogEntity
    {
        var normalized = code.Trim().ToUpperInvariant();
        var match = BagFor<T>().Values
            .OfType<T>()
            .FirstOrDefault(x => !x.IsDeleted && x.Code == normalized);

        return Task.FromResult(match);
    }

    private ConcurrentDictionary<Guid, CatalogEntity> BagFor<T>()
        where T : CatalogEntity
        => _bags.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Guid, CatalogEntity>());
}
