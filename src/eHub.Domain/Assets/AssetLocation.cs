using eHub.Domain.Common;

namespace eHub.Domain.Assets;

public sealed class AssetLocation
{
    public Guid CountryId { get; private set; }
    public Guid CityId { get; private set; }
    public Guid? DistrictId { get; private set; }
    public string? AddressLine { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private AssetLocation()
    {
    }

    public static AssetLocation Create(
        Guid countryId,
        Guid cityId,
        Guid? districtId = null,
        string? addressLine = null,
        double? latitude = null,
        double? longitude = null)
    {
        return new AssetLocation
        {
            CountryId = AppGuard.NotEmpty(countryId, nameof(countryId)),
            CityId = AppGuard.NotEmpty(cityId, nameof(cityId)),
            DistrictId = districtId is null || districtId == Guid.Empty ? null : districtId,
            AddressLine = string.IsNullOrWhiteSpace(addressLine) ? null : addressLine.Trim(),
            Latitude = latitude,
            Longitude = longitude
        };
    }
}
