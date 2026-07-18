using eHub.Application.Common.Messaging;
using eHub.Domain.Assets;

namespace eHub.Application.Assets.Queries.GetAsset;

public sealed record GetAssetQuery(Guid AssetId) : IQuery<AssetDto>;

public sealed record AssetDto(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    Guid? SubCategoryId,
    Guid? BrandId,
    Guid? ModelId,
    string Title,
    string? Description,
    string StatusCode,
    string? RejectionReason,
    int VersionNumber,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PublishedAtUtc,
    AssetPricingDto? Pricing,
    AssetLocationDto? Location,
    AssetRentalRulesDto? RentalRules,
    AssetSecurityDepositDto SecurityDeposit,
    AssetSupportOptionsDto SupportOptions,
    IReadOnlyList<AssetMediaDto> Media,
    IReadOnlyList<AssetAvailabilityDto> Availability,
    IReadOnlyList<string> Tags,
    IReadOnlyList<Guid> FeatureIds,
    IReadOnlyList<AssetVersionDto> VersionHistory);

public sealed record AssetPricingDto(
    Guid CurrencyId,
    Guid RentalPeriodTypeId,
    decimal Amount,
    decimal? WeekendAmount,
    decimal? WeeklyAmount,
    decimal? MonthlyAmount);

public sealed record AssetLocationDto(
    Guid CountryId,
    Guid CityId,
    Guid? DistrictId,
    string? AddressLine,
    double? Latitude,
    double? Longitude);

public sealed record AssetRentalRulesDto(
    int? MinRentalDays,
    int? MaxRentalDays,
    int? MinDriverAge,
    bool RequiresLicense,
    string? Notes);

public sealed record AssetSecurityDepositDto(bool Required, decimal? Amount, Guid? CurrencyId);

public sealed record AssetSupportOptionsDto(
    bool DriverSupport,
    bool DeliverySupport,
    bool GpsDevice,
    decimal? DriverFeeAmount,
    decimal? DeliveryFeeAmount);

public sealed record AssetMediaDto(
    Guid Id,
    AssetMediaKind Kind,
    string Url,
    string? FileName,
    bool IsPrimary,
    int SortOrder);

public sealed record AssetAvailabilityDto(Guid Id, DateOnly StartDate, DateOnly EndDate, string? Note);

public sealed record AssetVersionDto(
    int VersionNumber,
    string StatusCode,
    string? ChangeSummary,
    Guid? ChangedBy,
    DateTime ChangedAtUtc);
