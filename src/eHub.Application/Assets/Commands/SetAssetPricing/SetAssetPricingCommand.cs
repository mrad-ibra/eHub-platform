using eHub.Application.Common.Messaging;

namespace eHub.Application.Assets.Commands.SetAssetPricing;

public sealed record SetAssetPricingCommand(
    Guid AssetId,
    Guid CurrencyId,
    Guid RentalPeriodTypeId,
    decimal Amount,
    decimal? WeekendAmount = null,
    decimal? WeeklyAmount = null,
    decimal? MonthlyAmount = null) : ICommand;
