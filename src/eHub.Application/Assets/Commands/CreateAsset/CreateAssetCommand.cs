using eHub.Application.Common.Messaging;
using eHub.Domain.Assets;

namespace eHub.Application.Assets.Commands.CreateAsset;

public sealed record CreateAssetCommand(
    Guid CategoryId,
    string Title,
    Guid? SubCategoryId = null,
    Guid? BrandId = null,
    Guid? ModelId = null,
    string? Description = null) : ICommand<Guid>;
