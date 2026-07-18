using eHub.Domain.Assets;
using eHub.Domain.Exceptions;

namespace eHub.UnitTests.Domain.Assets;

public sealed class AssetTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CurrencyId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PeriodId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid CountryId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid CityId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    public void Create_StartsAsDraftWithVersionHistory()
    {
        var asset = Asset.Create(OwnerId, CategoryId, "Toyota Corolla", Now);

        asset.StatusCode.Should().Be(AssetStatusCodes.Draft);
        asset.Title.Should().Be("Toyota Corolla");
        asset.VersionNumber.Should().Be(1);
        asset.VersionHistory.Should().ContainSingle();
    }

    [Fact]
    public void Publish_RequiresPricingLocationAndImage()
    {
        var asset = Asset.Create(OwnerId, CategoryId, "Toyota Corolla", Now);

        var act = () => asset.Publish(Now.AddMinutes(1));

        act.Should().Throw<ValidationFailedException>();
    }

    [Fact]
    public void Publish_WhenReady_Succeeds()
    {
        var asset = ReadyAsset();

        asset.Publish(Now.AddMinutes(10));

        asset.StatusCode.Should().Be(AssetStatusCodes.Published);
        asset.PublishedAtUtc.Should().Be(Now.AddMinutes(10));
    }

    [Fact]
    public void ApprovalFlow_Works()
    {
        var asset = ReadyAsset();
        asset.SubmitForApproval(Now.AddMinutes(1));
        asset.StatusCode.Should().Be(AssetStatusCodes.PendingApproval);

        asset.Approve(Now.AddMinutes(2));
        asset.StatusCode.Should().Be(AssetStatusCodes.Published);
    }

    [Fact]
    public void Reject_SetsReason()
    {
        var asset = ReadyAsset();
        asset.SubmitForApproval(Now.AddMinutes(1));
        asset.Reject("Incomplete docs", Now.AddMinutes(2));

        asset.StatusCode.Should().Be(AssetStatusCodes.Rejected);
        asset.RejectionReason.Should().Be("Incomplete docs");
    }

    [Fact]
    public void MediaTagsAndFeatures_AreTracked()
    {
        var asset = Asset.Create(OwnerId, CategoryId, "Boat listing", Now);
        var featureId = Guid.NewGuid();

        asset.AddMedia(AssetMediaKind.Image, "https://cdn/img.jpg", Now.AddMinutes(1), isPrimary: true);
        asset.AddMedia(AssetMediaKind.Video, "https://cdn/v.mp4", Now.AddMinutes(2));
        asset.AddMedia(AssetMediaKind.Document, "https://cdn/doc.pdf", Now.AddMinutes(3));
        asset.AddTag("family", Now.AddMinutes(4));
        asset.AddFeature(featureId, Now.AddMinutes(5));
        asset.SetSupportOptions(AssetSupportOptions.Create(driverSupport: true, gpsDevice: true), Now.AddMinutes(6));
        asset.SetSecurityDeposit(AssetSecurityDeposit.Create(200, CurrencyId), Now.AddMinutes(7));
        asset.SetRentalRules(AssetRentalRules.Create(minRentalDays: 1, requiresLicense: true), Now.AddMinutes(8));
        asset.BlockAvailability(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5), Now.AddMinutes(9));

        asset.Images.Should().ContainSingle();
        asset.Videos.Should().ContainSingle();
        asset.Documents.Should().ContainSingle();
        asset.Tags.Should().ContainSingle(t => t.Tag == "family");
        asset.Features.Should().ContainSingle(f => f.FeatureDefinitionId == featureId);
        asset.SupportOptions.DriverSupport.Should().BeTrue();
        asset.SupportOptions.GpsDevice.Should().BeTrue();
        asset.SecurityDeposit.Required.Should().BeTrue();
        asset.AvailabilityBlocks.Should().ContainSingle();
    }

    [Fact]
    public void Archive_LocksFurtherPublish()
    {
        var asset = ReadyAsset();
        asset.Publish(Now.AddMinutes(1));
        asset.Archive(Now.AddMinutes(2));

        var act = () => asset.Publish(Now.AddMinutes(3));

        act.Should().Throw<ConflictException>();
        asset.StatusCode.Should().Be(AssetStatusCodes.Archived);
    }

    private static Asset ReadyAsset()
    {
        var asset = Asset.Create(OwnerId, CategoryId, "Ready asset", Now);
        asset.SetPricing(AssetPricing.Create(CurrencyId, PeriodId, 100), Now.AddMinutes(1));
        asset.SetLocation(AssetLocation.Create(CountryId, CityId), Now.AddMinutes(2));
        asset.AddMedia(AssetMediaKind.Image, "https://cdn/a.jpg", Now.AddMinutes(3), isPrimary: true);
        return asset;
    }
}
