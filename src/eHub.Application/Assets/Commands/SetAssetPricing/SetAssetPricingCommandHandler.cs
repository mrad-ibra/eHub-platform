using eHub.Application.Assets.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Assets;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;
using FluentValidation;

namespace eHub.Application.Assets.Commands.SetAssetPricing;

public sealed class SetAssetPricingCommandValidator : AbstractValidator<SetAssetPricingCommand>
{
    public SetAssetPricingCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.RentalPeriodTypeId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}

public sealed class SetAssetPricingCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<SetAssetPricingCommand>
{
    public async Task Handle(SetAssetPricingCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var asset = await assets.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        if (asset.OwnerId != userId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.AssetAccessDenied));
        }

        asset.SetPricing(
            AssetPricing.Create(
                request.CurrencyId,
                request.RentalPeriodTypeId,
                request.Amount,
                request.WeekendAmount,
                request.WeeklyAmount,
                request.MonthlyAmount),
            clock.UtcNow,
            userId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
