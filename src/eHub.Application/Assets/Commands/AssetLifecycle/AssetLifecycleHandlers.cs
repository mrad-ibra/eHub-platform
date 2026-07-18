using eHub.Application.Assets.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Exceptions;
using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Assets.Commands.AssetLifecycle;

public sealed class PublishAssetCommandValidator : AbstractValidator<PublishAssetCommand>
{
    public PublishAssetCommandValidator() => RuleFor(x => x.AssetId).NotEmpty();
}

public sealed class ArchiveAssetCommandValidator : AbstractValidator<ArchiveAssetCommand>
{
    public ArchiveAssetCommandValidator() => RuleFor(x => x.AssetId).NotEmpty();
}

public sealed class SubmitAssetForApprovalCommandValidator : AbstractValidator<SubmitAssetForApprovalCommand>
{
    public SubmitAssetForApprovalCommandValidator() => RuleFor(x => x.AssetId).NotEmpty();
}

public sealed class ApproveAssetCommandValidator : AbstractValidator<ApproveAssetCommand>
{
    public ApproveAssetCommandValidator() => RuleFor(x => x.AssetId).NotEmpty();
}

public sealed class RejectAssetCommandValidator : AbstractValidator<RejectAssetCommand>
{
    public RejectAssetCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class PublishAssetCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<PublishAssetCommand>
{
    public async Task Handle(PublishAssetCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var asset = await RequireOwnedAsset(assets, request.AssetId, userId, cancellationToken);
        asset.Publish(clock.UtcNow, userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    internal static async Task<Domain.Assets.Asset> RequireOwnedAsset(
        IAssetRepository assets,
        Guid assetId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var asset = await assets.GetByIdAsync(assetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        if (asset.OwnerId != userId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.AssetAccessDenied));
        }

        return asset;
    }
}

public sealed class ArchiveAssetCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<ArchiveAssetCommand>
{
    public async Task Handle(ArchiveAssetCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var asset = await PublishAssetCommandHandler.RequireOwnedAsset(assets, request.AssetId, userId, cancellationToken);
        asset.Archive(clock.UtcNow, userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class SubmitAssetForApprovalCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<SubmitAssetForApprovalCommand>
{
    public async Task Handle(SubmitAssetForApprovalCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var asset = await PublishAssetCommandHandler.RequireOwnedAsset(assets, request.AssetId, userId, cancellationToken);
        asset.SubmitForApproval(clock.UtcNow, userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ApproveAssetCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<ApproveAssetCommand>
{
    public async Task Handle(ApproveAssetCommand request, CancellationToken cancellationToken)
    {
        // Admin/moderator gate can be tightened later via permission catalog.
        var actorId = currentUser.RequireUserId();
        var asset = await assets.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        asset.Approve(clock.UtcNow, actorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class RejectAssetCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<RejectAssetCommand>
{
    public async Task Handle(RejectAssetCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentUser.RequireUserId();
        var asset = await assets.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        asset.Reject(request.Reason, clock.UtcNow, actorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
