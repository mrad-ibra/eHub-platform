using eHub.Application.Common.Messaging;

namespace eHub.Application.Assets.Commands.SetAssetLocation;

public sealed record SetAssetLocationCommand(
    Guid AssetId,
    Guid CountryId,
    Guid CityId,
    Guid? DistrictId = null,
    string? AddressLine = null,
    double? Latitude = null,
    double? Longitude = null) : ICommand;
