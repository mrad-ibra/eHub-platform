using eHub.Domain.Assets;

namespace eHub.Application.Assets.Abstractions;

public interface IAssetRepository
{
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);

    Task<Asset?> GetByIdAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> ListPublishedAsync(CancellationToken cancellationToken = default);
}
