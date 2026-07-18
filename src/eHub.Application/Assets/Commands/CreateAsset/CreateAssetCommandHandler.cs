using eHub.Application.Assets.Abstractions;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Assets;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;
using FluentValidation;

namespace eHub.Application.Assets.Commands.CreateAsset;

public sealed class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateAssetCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    ICategoryRepository categories,
    ISubCategoryRepository subCategories,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateAssetCommand, Guid>
{
    public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        var ownerId = currentUser.RequireUserId();

        _ = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.CatalogItemNotFound));

        if (request.SubCategoryId is { } subId)
        {
            var sub = await subCategories.GetByIdAsync(subId, cancellationToken)
                ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.CatalogItemNotFound));
            if (sub.CategoryId != request.CategoryId)
            {
                throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BadRequest));
            }
        }

        var asset = Asset.Create(
            ownerId,
            request.CategoryId,
            request.Title,
            clock.UtcNow,
            request.SubCategoryId,
            request.BrandId,
            request.ModelId,
            request.Description,
            ownerId);

        await assets.AddAsync(asset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return asset.Id;
    }
}
