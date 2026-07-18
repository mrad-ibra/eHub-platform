using eHub.Application.Common.Messaging;
using eHub.Domain.Assets;

namespace eHub.Application.Assets.Commands.AddAssetMedia;

public sealed record AddAssetMediaCommand(
    Guid AssetId,
    AssetMediaKind Kind,
    string Url,
    string? FileName = null,
    string? ContentType = null,
    long? SizeBytes = null,
    bool IsPrimary = false) : ICommand<Guid>;
