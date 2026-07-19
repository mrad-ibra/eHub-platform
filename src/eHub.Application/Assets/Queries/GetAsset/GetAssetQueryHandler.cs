using eHub.Application.Assets.Abstractions;
using eHub.Application.Common.Messaging;
using eHub.Domain.Assets;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Application.Assets.Queries.GetAsset;

public sealed class GetAssetQueryHandler(IAssetRepository assets)
    : IQueryHandler<GetAssetQuery, AssetDto>
{
    public async Task<AssetDto> Handle(GetAssetQuery request, CancellationToken cancellationToken)
    {
        var asset = await assets.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        return Map(asset);
    }

    public static AssetDto Map(Asset asset)
        => new(
            asset.Id,
            asset.OwnerId,
            asset.CategoryId,
            asset.SubCategoryId,
            asset.BrandId,
            asset.ModelId,
            asset.Title,
            asset.Description,
            asset.Status.Value,
            asset.RejectionReason,
            asset.VersionNumber,
            asset.CreatedAtUtc,
            asset.UpdatedAtUtc,
            asset.PublishedAtUtc,
            asset.Pricing is null
                ? null
                : new AssetPricingDto(
                    asset.Pricing.CurrencyId,
                    asset.Pricing.RentalPeriodTypeId,
                    asset.Pricing.Amount,
                    asset.Pricing.WeekendAmount,
                    asset.Pricing.WeeklyAmount,
                    asset.Pricing.MonthlyAmount),
            asset.Location is null
                ? null
                : new AssetLocationDto(
                    asset.Location.CountryId,
                    asset.Location.CityId,
                    asset.Location.DistrictId,
                    asset.Location.AddressLine,
                    asset.Location.Latitude,
                    asset.Location.Longitude),
            asset.RentalRules is null
                ? null
                : new AssetRentalRulesDto(
                    asset.RentalRules.MinRentalDays,
                    asset.RentalRules.MaxRentalDays,
                    asset.RentalRules.MinDriverAge,
                    asset.RentalRules.RequiresLicense,
                    asset.RentalRules.Notes,
                    asset.RentalRules.PreparationBufferDays),
            new AssetSecurityDepositDto(
                asset.SecurityDeposit.Required,
                asset.SecurityDeposit.Amount,
                asset.SecurityDeposit.CurrencyId),
            new AssetSupportOptionsDto(
                asset.SupportOptions.DriverSupport,
                asset.SupportOptions.DeliverySupport,
                asset.SupportOptions.GpsDevice,
                asset.SupportOptions.DriverFeeAmount,
                asset.SupportOptions.DeliveryFeeAmount),
            asset.Media
                .OrderBy(m => m.SortOrder)
                .Select(m => new AssetMediaDto(m.Id, m.Kind, m.Url, m.FileName, m.IsPrimary, m.SortOrder))
                .ToArray(),
            asset.AvailabilityBlocks
                .Select(b => new AssetAvailabilityDto(b.Id, b.StartDate, b.EndDate, b.Note))
                .ToArray(),
            asset.Tags.Select(t => t.Tag).ToArray(),
            asset.Features.Select(f => f.FeatureDefinitionId).ToArray(),
            asset.VersionHistory
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new AssetVersionDto(
                    v.VersionNumber,
                    v.Status.Value,
                    v.ChangeSummary,
                    v.ChangedBy,
                    v.ChangedAtUtc))
                .ToArray());
}
