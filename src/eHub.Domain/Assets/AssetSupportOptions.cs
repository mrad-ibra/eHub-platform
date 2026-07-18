namespace eHub.Domain.Assets;

public sealed class AssetSupportOptions
{
    public bool DriverSupport { get; private set; }
    public bool DeliverySupport { get; private set; }
    public bool GpsDevice { get; private set; }
    public decimal? DriverFeeAmount { get; private set; }
    public decimal? DeliveryFeeAmount { get; private set; }

    private AssetSupportOptions()
    {
    }

    public static AssetSupportOptions Create(
        bool driverSupport = false,
        bool deliverySupport = false,
        bool gpsDevice = false,
        decimal? driverFeeAmount = null,
        decimal? deliveryFeeAmount = null)
    {
        return new AssetSupportOptions
        {
            DriverSupport = driverSupport,
            DeliverySupport = deliverySupport,
            GpsDevice = gpsDevice,
            DriverFeeAmount = driverFeeAmount,
            DeliveryFeeAmount = deliveryFeeAmount
        };
    }
}
