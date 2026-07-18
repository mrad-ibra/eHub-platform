using eHub.Domain.Catalog;

namespace eHub.Application.Catalog.Abstractions;

public interface ICatalogStore
{
    Task AddAsync<T>(T item, CancellationToken cancellationToken = default)
        where T : CatalogEntity;

    Task<IReadOnlyList<T>> ListAsync<T>(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
        where T : CatalogEntity;

    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default)
        where T : CatalogEntity;

    Task<T?> GetByCodeAsync<T>(string code, CancellationToken cancellationToken = default)
        where T : CatalogEntity;
}
