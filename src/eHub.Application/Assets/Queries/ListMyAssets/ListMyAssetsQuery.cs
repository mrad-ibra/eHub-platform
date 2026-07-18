using eHub.Application.Assets.Abstractions;
using eHub.Application.Assets.Queries.GetAsset;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;

namespace eHub.Application.Assets.Queries.ListMyAssets;

public sealed record ListMyAssetsQuery : IQuery<IReadOnlyList<AssetDto>>;

public sealed class ListMyAssetsQueryHandler(
    ICurrentUser currentUser,
    IAssetRepository assets) : IQueryHandler<ListMyAssetsQuery, IReadOnlyList<AssetDto>>
{
    public async Task<IReadOnlyList<AssetDto>> Handle(
        ListMyAssetsQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = currentUser.RequireUserId();
        var list = await assets.ListByOwnerAsync(ownerId, cancellationToken);
        return list.Select(GetAssetQueryHandler.Map).ToArray();
    }
}
