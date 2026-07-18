using eHub.Application.Assets.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Assets;
using eHub.Domain.Exceptions;
using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Assets.Commands.SetAssetLocation;

public sealed class SetAssetLocationCommandValidator : AbstractValidator<SetAssetLocationCommand>
{
    public SetAssetLocationCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.CountryId).NotEmpty();
        RuleFor(x => x.CityId).NotEmpty();
    }
}

public sealed class SetAssetLocationCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<SetAssetLocationCommand>
{
    public async Task Handle(SetAssetLocationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var asset = await assets.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        if (asset.OwnerId != userId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.AssetAccessDenied));
        }

        asset.SetLocation(
            AssetLocation.Create(
                request.CountryId,
                request.CityId,
                request.DistrictId,
                request.AddressLine,
                request.Latitude,
                request.Longitude),
            clock.UtcNow,
            userId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
