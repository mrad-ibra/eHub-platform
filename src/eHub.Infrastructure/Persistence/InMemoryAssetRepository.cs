using System.Collections.Concurrent;
using eHub.Application.Assets.Abstractions;
using eHub.Domain.Assets;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryAssetRepository : IAssetRepository
{
    private readonly ConcurrentDictionary<Guid, Asset> _assets = new();

    public Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        if (!_assets.TryAdd(asset.Id, asset))
        {
            throw new InvalidOperationException($"Asset '{asset.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<Asset?> GetByIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        if (!_assets.TryGetValue(assetId, out var asset) || asset.IsDeleted)
        {
            return Task.FromResult<Asset?>(null);
        }

        return Task.FromResult<Asset?>(asset);
    }

    public Task<IReadOnlyList<Asset>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Asset> items = _assets.Values
            .Where(a => !a.IsDeleted && a.OwnerId == ownerId)
            .OrderByDescending(a => a.UpdatedAtUtc)
            .ToArray();

        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<Asset>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Asset> items = _assets.Values
            .Where(a => !a.IsDeleted && a.Status == AssetStatusCode.Published)
            .OrderByDescending(a => a.PublishedAtUtc)
            .ToArray();

        return Task.FromResult(items);
    }
}
