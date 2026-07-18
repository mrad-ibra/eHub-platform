using eHub.Application.Assets.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Exceptions;
using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Assets.Commands.AddAssetMedia;

public sealed class AddAssetMediaCommandValidator : AbstractValidator<AddAssetMediaCommand>
{
    public AddAssetMediaCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
    }
}

public sealed class AddAssetMediaCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<AddAssetMediaCommand, Guid>
{
    public async Task<Guid> Handle(AddAssetMediaCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var asset = await assets.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        if (asset.OwnerId != userId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.AssetAccessDenied));
        }

        var media = asset.AddMedia(
            request.Kind,
            request.Url,
            clock.UtcNow,
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            request.IsPrimary,
            userId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return media.Id;
    }
}
