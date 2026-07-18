using eHub.Application.Common.Messaging;
using eHub.Domain.Assets;

namespace eHub.Application.Assets.Commands.UpdateAssetDetails;

public sealed record UpdateAssetDetailsCommand(
    Guid AssetId,
    string Title,
    string? Description,
    Guid? SubCategoryId,
    Guid? BrandId,
    Guid? ModelId) : ICommand;
