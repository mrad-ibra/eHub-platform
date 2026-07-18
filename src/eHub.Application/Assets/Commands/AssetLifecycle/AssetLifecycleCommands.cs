using eHub.Application.Common.Messaging;

namespace eHub.Application.Assets.Commands.AssetLifecycle;

public sealed record PublishAssetCommand(Guid AssetId) : ICommand;

public sealed record ArchiveAssetCommand(Guid AssetId) : ICommand;

public sealed record SubmitAssetForApprovalCommand(Guid AssetId) : ICommand;

public sealed record ApproveAssetCommand(Guid AssetId) : ICommand;

public sealed record RejectAssetCommand(Guid AssetId, string Reason) : ICommand;
