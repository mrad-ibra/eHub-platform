using eHub.Application.Assets.Abstractions;
using eHub.Application.Bookings.Abstractions;
using eHub.Application.Bookings.Services;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Assets;
using eHub.Domain.Bookings;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Bookings.Commands.CreateBooking;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after start date.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class CreateBookingCommandHandler(
    ICurrentUser currentUser,
    IAssetRepository assets,
    IBookingRepository bookings,
    IBookingNumberGenerator bookingNumbers,
    IBookingIdempotencyStore idempotency,
    IBrandRepository brands,
    IModelRepository models,
    BookingAvailabilityService availability,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateBookingCommand, CreateBookingResult>
{
    public async Task<CreateBookingResult> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken)
    {
        var renterId = currentUser.RequireUserId();
        var key = request.IdempotencyKey.Trim();

        var existingId = await idempotency.FindBookingIdAsync(renterId, key, cancellationToken);
        if (existingId is { } replayId)
        {
            var existing = await bookings.GetByIdAsync(replayId, cancellationToken)
                ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));
            return Map(existing);
        }

        var asset = await assets.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetNotFound));

        if (asset.Pricing is null)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingPricingRequired));
        }

        var period = BookingPeriod.Create(request.StartDate, request.EndDate);
        var bufferDays = BookingDefaults.DefaultPreparationBufferDays;
        var terms = BuildTerms(asset, bufferDays);

        var blocking = await bookings.ListBlockingByAssetAsync(asset.Id, cancellationToken);
        availability.EnsureCanBook(asset, period, bufferDays, blocking);

        var unitPrice = Money.Create(asset.Pricing.Amount, asset.Pricing.CurrencyId);
        var snapshot = await BuildSnapshotAsync(asset, clock.UtcNow, cancellationToken);
        var driver = BuildDriver(request.DriverRequested, asset, unitPrice.CurrencyId);
        var delivery = BuildDelivery(request.DeliveryRequested, asset, unitPrice.CurrencyId, request.DropoffAddressLine);
        var pickup = request.PickupUseAssetLocation
            ? PickupInformation.UseAsset()
            : PickupInformation.Custom(request.PickupAddressLine);
        var dropoff = request.DropoffUseAssetLocation
            ? DropoffInformation.UseAsset()
            : DropoffInformation.Custom(request.DropoffAddressLine);

        var number = await bookingNumbers.NextAsync(cancellationToken);
        var booking = Booking.CreateRequest(
            number,
            asset.Id,
            renterId,
            asset.OwnerId,
            period,
            unitPrice,
            snapshot,
            terms,
            clock.UtcNow,
            instantBook: false,
            pickup,
            dropoff,
            driver,
            delivery,
            request.Notes);

        // Re-check under soft race: list again before insert (InMemory uses lock in Add).
        blocking = await bookings.ListBlockingByAssetAsync(asset.Id, cancellationToken);
        availability.EnsureCanBook(asset, period, bufferDays, blocking);

        await bookings.AddAsync(booking, cancellationToken);
        await idempotency.SaveAsync(renterId, key, booking.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(booking);
    }

    private static BookingTerms BuildTerms(Asset asset, int bufferDays)
    {
        var rules = asset.RentalRules;
        return BookingTerms.Create(
            bufferDays,
            rules?.MinRentalDays,
            rules?.MaxRentalDays,
            rules?.MinDriverAge,
            rules?.RequiresLicense ?? false,
            rules?.Notes);
    }

    private async Task<BookingAssetSnapshot> BuildSnapshotAsync(
        Asset asset,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        string? brandName = null;
        string? modelName = null;

        if (asset.BrandId is { } brandId)
        {
            brandName = (await brands.GetByIdAsync(brandId, cancellationToken))?.Name;
        }

        if (asset.ModelId is { } modelId)
        {
            modelName = (await models.GetByIdAsync(modelId, cancellationToken))?.Name;
        }

        var images = asset.Images
            .OrderByDescending(i => i.IsPrimary)
            .Select(i => i.Url)
            .Take(3)
            .ToArray();

        return BookingAssetSnapshot.Create(
            asset.Title,
            asset.OwnerId,
            nowUtc,
            brandName,
            modelName,
            images);
    }

    private static DriverOption BuildDriver(bool requested, Asset asset, Guid currencyId)
    {
        if (!requested)
        {
            return DriverOption.None();
        }

        if (!asset.SupportOptions.DriverSupport)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BadRequest));
        }

        var fee = Money.Create(asset.SupportOptions.DriverFeeAmount ?? 0m, currencyId);
        return DriverOption.Request(fee);
    }

    private static DeliveryOption BuildDelivery(
        bool requested,
        Asset asset,
        Guid currencyId,
        string? addressLine)
    {
        if (!requested)
        {
            return DeliveryOption.None();
        }

        if (!asset.SupportOptions.DeliverySupport)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BadRequest));
        }

        var fee = Money.Create(asset.SupportOptions.DeliveryFeeAmount ?? 0m, currencyId);
        return DeliveryOption.Request(fee, addressLine);
    }

    private static CreateBookingResult Map(Booking booking)
        => new(
            booking.Id,
            booking.BookingNumber,
            booking.Status.Value,
            booking.AssetId,
            booking.Period.StartDate,
            booking.Period.EndDate,
            booking.BufferDays,
            booking.TotalPrice.Amount,
            booking.TotalPrice.CurrencyId,
            booking.ExpiresAtUtc,
            booking.Version,
            booking.AssetSnapshot.Name);
}
