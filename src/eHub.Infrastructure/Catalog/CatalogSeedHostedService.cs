using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Domain.Catalog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eHub.Infrastructure.Catalog;

public sealed class CatalogSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CatalogOptions> options,
    ILogger<CatalogSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Seed.Enabled)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogStore>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        if (await catalog.GetByCodeAsync<Category>("VEHICLE", cancellationToken) is not null)
        {
            return;
        }

        logger.LogInformation("Seeding catalog dictionaries");

        var vehicle = Category.Create("VEHICLE", "Vehicles", now, 1);
        var equipment = Category.Create("EQUIPMENT", "Equipment", now, 2);
        await catalog.AddAsync(vehicle, cancellationToken);
        await catalog.AddAsync(equipment, cancellationToken);

        await catalog.AddAsync(SubCategory.Create(vehicle.Id, "CAR", "Car", now, 1), cancellationToken);
        await catalog.AddAsync(SubCategory.Create(vehicle.Id, "MOTORCYCLE", "Motorcycle", now, 2), cancellationToken);
        await catalog.AddAsync(SubCategory.Create(equipment.Id, "EXCAVATOR", "Excavator", now, 1), cancellationToken);

        var toyota = Brand.Create("TOYOTA", "Toyota", now, 1);
        var bmw = Brand.Create("BMW", "BMW", now, 2);
        await catalog.AddAsync(toyota, cancellationToken);
        await catalog.AddAsync(bmw, cancellationToken);
        await catalog.AddAsync(Model.Create(toyota.Id, "COROLLA", "Corolla", now, 1), cancellationToken);
        await catalog.AddAsync(Model.Create(bmw.Id, "X5", "X5", now, 1), cancellationToken);

        var az = Country.Create("AZ", "Azerbaijan", now, 1);
        await catalog.AddAsync(az, cancellationToken);
        var baku = City.Create(az.Id, "BAKU", "Baku", now, 1);
        await catalog.AddAsync(baku, cancellationToken);
        await catalog.AddAsync(District.Create(baku.Id, "NASIMI", "Nasimi", now, 1), cancellationToken);
        await catalog.AddAsync(District.Create(baku.Id, "YASAMAL", "Yasamal", now, 2), cancellationToken);

        await catalog.AddAsync(Currency.Create("AZN", "Azerbaijani Manat", "₼", now, 2, 1), cancellationToken);
        await catalog.AddAsync(Currency.Create("USD", "US Dollar", "$", now, 2, 2), cancellationToken);
        await catalog.AddAsync(Currency.Create("EUR", "Euro", "€", now, 2, 3), cancellationToken);

        await catalog.AddAsync(Language.Create("AZ", "Azerbaijani", "az-AZ", now, 1), cancellationToken);
        await catalog.AddAsync(Language.Create("EN", "English", "en-US", now, 2), cancellationToken);

        await SeedFlat(catalog, cancellationToken,
            Transmission.Create("MANUAL", "Manual", now, 1),
            Transmission.Create("AUTOMATIC", "Automatic", now, 2));

        await SeedFlat(catalog, cancellationToken,
            FuelType.Create("PETROL", "Petrol", now, 1),
            FuelType.Create("DIESEL", "Diesel", now, 2),
            FuelType.Create("ELECTRIC", "Electric", now, 3),
            FuelType.Create("HYBRID", "Hybrid", now, 4));

        await SeedFlat(catalog, cancellationToken,
            VehicleType.Create("SEDAN", "Sedan", now, 1),
            VehicleType.Create("SUV", "SUV", now, 2),
            VehicleType.Create("VAN", "Van", now, 3));

        await SeedFlat(catalog, cancellationToken,
            EquipmentType.Create("CONSTRUCTION", "Construction", now, 1),
            EquipmentType.Create("GENERATOR", "Generator", now, 2));

        await catalog.AddAsync(FeatureDefinition.Create("GPS", "GPS", now, "SAFETY", 1), cancellationToken);
        await catalog.AddAsync(FeatureDefinition.Create("AC", "Air Conditioning", now, "COMFORT", 2), cancellationToken);

        await catalog.AddAsync(Color.Create("WHITE", "White", now, "#FFFFFF", 1), cancellationToken);
        await catalog.AddAsync(Color.Create("BLACK", "Black", now, "#000000", 2), cancellationToken);
        await catalog.AddAsync(Color.Create("SILVER", "Silver", now, "#C0C0C0", 3), cancellationToken);

        await SeedFlat(catalog, cancellationToken,
            DocumentType.Create("DRIVERS_LICENSE", "Driver's License", now, 1),
            DocumentType.Create("PASSPORT", "Passport", now, 2),
            DocumentType.Create("ID_CARD", "ID Card", now, 3));

        await SeedFlat(catalog, cancellationToken,
            MediaType.Create("IMAGE", "Image", now, 1),
            MediaType.Create("VIDEO", "Video", now, 2),
            MediaType.Create("DOCUMENT", "Document", now, 3));

        await SeedFlat(catalog, cancellationToken,
            RentalPeriodType.Create("HOURLY", "Hourly", now, 1),
            RentalPeriodType.Create("DAILY", "Daily", now, 2),
            RentalPeriodType.Create("WEEKLY", "Weekly", now, 3),
            RentalPeriodType.Create("MONTHLY", "Monthly", now, 4));

        await SeedFlat(catalog, cancellationToken,
            PaymentMethod.Create("CARD", "Card", now, 1),
            PaymentMethod.Create("CASH", "Cash", now, 2),
            PaymentMethod.Create("BANK_TRANSFER", "Bank Transfer", now, 3));

        await SeedFlat(catalog, cancellationToken,
            BookingStatus.Create("PENDING", "Pending", now, 1),
            BookingStatus.Create("CONFIRMED", "Confirmed", now, 2),
            BookingStatus.Create("CANCELLED", "Cancelled", now, 3),
            BookingStatus.Create("COMPLETED", "Completed", now, 4));

        await SeedFlat(catalog, cancellationToken,
            AssetStatus.Create("DRAFT", "Draft", now, 1),
            AssetStatus.Create("PENDING_APPROVAL", "Pending Approval", now, 2),
            AssetStatus.Create("PUBLISHED", "Published", now, 3),
            AssetStatus.Create("REJECTED", "Rejected", now, 4),
            AssetStatus.Create("SUSPENDED", "Suspended", now, 5),
            AssetStatus.Create("ARCHIVED", "Archived", now, 6));

        await SeedFlat(catalog, cancellationToken,
            ReviewStatus.Create("PENDING", "Pending", now, 1),
            ReviewStatus.Create("APPROVED", "Approved", now, 2),
            ReviewStatus.Create("REJECTED", "Rejected", now, 3));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedFlat<T>(
        ICatalogStore catalog,
        CancellationToken cancellationToken,
        params T[] items)
        where T : CatalogEntity
    {
        foreach (var item in items)
        {
            await catalog.AddAsync(item, cancellationToken);
        }
    }
}
