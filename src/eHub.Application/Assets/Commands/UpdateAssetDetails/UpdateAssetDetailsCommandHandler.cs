using eHub.Application.Assets.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;
using FluentValidation;

namespace eHub.Application.Assets.Commands.UpdateAssetDetails;

public sealed class UpdateAssetDetailsCommandValidator : AbstractValidator<UpdateAssetDetailsCommand>
{
    public UpdateAssetDetailsCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateAssetDetailsCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateAssetDetailsCommand>
{
    public async Task Handle(UpdateAssetDetailsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var asset = await assets.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        if (asset.OwnerId != userId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.AssetAccessDenied));
        }

        asset.UpdateDetails(
            request.Title,
            request.Description,
            request.SubCategoryId,
            request.BrandId,
            request.ModelId,
            clock.UtcNow,
            userId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
