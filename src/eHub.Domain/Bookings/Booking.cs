using eHub.Domain.Bookings.Events;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Bookings;

/// <summary>
/// Booking aggregate root. Soft Hold on create (PendingOwnerApproval).
/// Payment TTL starts only on Approve. Occupancy uses period + BufferDays.
/// </summary>
public sealed class Booking : AggregateRoot
{
    private readonly List<BookingTimelineEntry> _timeline = [];
    private readonly List<BookingStatusHistoryEntry> _statusHistory = [];

    public string BookingNumber { get; private set; } = string.Empty;
    public Guid AssetId { get; private set; }
    public Guid RenterId { get; private set; }
    public Guid HostId { get; private set; }
    public BookingPeriod Period { get; private set; } = null!;
    public int BufferDays { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public Money TotalPrice { get; private set; } = null!;
    public BookingAssetSnapshot AssetSnapshot { get; private set; } = null!;
    public BookingTerms Terms { get; private set; } = null!;
    public PickupInformation Pickup { get; private set; } = null!;
    public DropoffInformation Dropoff { get; private set; } = null!;
    public DriverOption Driver { get; private set; } = null!;
    public DeliveryOption Delivery { get; private set; } = null!;
    public BookingStatusCode Status { get; private set; } = BookingStatusCode.Draft;
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? PaymentId { get; private set; }

    /// <summary>
    /// Aggregate mutation counter (future optimistic concurrency token — not snapshot schema version).
    /// </summary>
    public int AggregateVersion { get; private set; }

    public IReadOnlyCollection<BookingTimelineEntry> Timeline => _timeline;
    public IReadOnlyCollection<BookingStatusHistoryEntry> StatusHistory => _statusHistory;

    public DateOnly OccupiedEnd => Period.OccupiedEnd(BufferDays);

    /// <summary>
    /// True when this booking still occupies the calendar at <paramref name="nowUtc"/>
    /// (pending holds past ExpiresAtUtc do not block).
    /// </summary>
    public bool BlocksCalendar(DateTime nowUtc)
    {
        if (!Status.IsBlocking)
        {
            return false;
        }

        if (Status.IsOneOf(
                BookingStatusCode.PendingOwnerApproval,
                BookingStatusCode.PendingPayment))
        {
            return ExpiresAtUtc is null || nowUtc < ExpiresAtUtc;
        }

        return true;
    }

    private Booking()
    {
    }

    /// <summary>
    /// Creates Soft Hold (PendingOwnerApproval) unless <paramref name="instantBook"/>.
    /// </summary>
    public static Booking CreateRequest(
        string bookingNumber,
        Guid assetId,
        Guid renterId,
        Guid hostId,
        BookingPeriod period,
        Money unitPrice,
        BookingAssetSnapshot assetSnapshot,
        BookingTerms terms,
        DateTime nowUtc,
        bool instantBook = false,
        PickupInformation? pickup = null,
        DropoffInformation? dropoff = null,
        DriverOption? driver = null,
        DeliveryOption? delivery = null,
        string? notes = null)
    {
        AppGuard.NotEmpty(bookingNumber, nameof(bookingNumber));
        AppGuard.NotEmpty(assetId, nameof(assetId));
        AppGuard.NotEmpty(renterId, nameof(renterId));
        AppGuard.NotEmpty(hostId, nameof(hostId));

        if (renterId == hostId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.BookingOwnAsset));
        }

        BookingAvailability.EnsureStartNotInPast(period, nowUtc);
        BookingAvailability.EnsureRentalDaysAllowed(period, terms);

        var driverOpt = driver ?? DriverOption.None();
        var deliveryOpt = delivery ?? DeliveryOption.None();
        var total = CalculateTotal(unitPrice, period, driverOpt, deliveryOpt);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingNumber = bookingNumber.Trim(),
            AssetId = assetId,
            RenterId = renterId,
            HostId = hostId,
            Period = period,
            BufferDays = terms.BufferDays,
            UnitPrice = unitPrice,
            TotalPrice = total,
            AssetSnapshot = assetSnapshot,
            Terms = terms,
            Pickup = pickup ?? PickupInformation.UseAsset(),
            Dropoff = dropoff ?? DropoffInformation.UseAsset(),
            Driver = driverOpt,
            Delivery = deliveryOpt,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            AggregateVersion = 1
        };

        booking.SetCreatedAudit(nowUtc, renterId);

        if (instantBook)
        {
            booking.TransitionTo(
                BookingStatusCode.PendingPayment,
                nowUtc,
                renterId,
                "Created",
                "Booking created (Instant Book) — Hard Hold, awaiting payment.");
            booking.ExpiresAtUtc = nowUtc.Add(BookingDefaults.PaymentTtl);
        }
        else
        {
            booking.TransitionTo(
                BookingStatusCode.PendingOwnerApproval,
                nowUtc,
                renterId,
                "Created",
                "Booking created — Soft Hold, awaiting owner approval.");
            booking.ExpiresAtUtc = nowUtc.Add(BookingDefaults.OwnerApprovalTtl);
        }

        booking.Raise(new BookingCreated(
            booking.Id,
            booking.BookingNumber,
            booking.AssetId,
            booking.RenterId,
            booking.HostId,
            booking.Period.StartDate,
            booking.Period.EndDate,
            booking.Status.Value,
            nowUtc));

        return booking;
    }

    public void Approve(Guid hostId, DateTime nowUtc)
    {
        EnsureHost(hostId);
        EnsureStatus(BookingStatusCode.PendingOwnerApproval);
        EnsureHoldActive(nowUtc);

        TransitionTo(
            BookingStatusCode.PendingPayment,
            nowUtc,
            hostId,
            "Approved",
            "Owner approved — payment window started.");
        ExpiresAtUtc = nowUtc.Add(BookingDefaults.PaymentTtl);
        AggregateVersion++;

        Raise(new BookingApproved(Id, BookingNumber, nowUtc));
    }

    public void Reject(Guid hostId, string reason, DateTime nowUtc)
    {
        EnsureHost(hostId);
        EnsureStatus(BookingStatusCode.PendingOwnerApproval);
        RejectionReason = AppGuard.NotEmpty(reason, nameof(reason)).Trim();
        if (RejectionReason.Length > 1000)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingReasonTooLong));
        }

        TransitionTo(
            BookingStatusCode.Rejected,
            nowUtc,
            hostId,
            "Rejected",
            $"Owner rejected: {RejectionReason}");
        ExpiresAtUtc = null;
        AggregateVersion++;

        Raise(new BookingRejected(Id, BookingNumber, RejectionReason, nowUtc));
    }

    public void Expire(DateTime nowUtc)
    {
        if (!Status.IsOneOf(BookingStatusCode.PendingOwnerApproval, BookingStatusCode.PendingPayment))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingInvalidStatusTransition));
        }

        if (ExpiresAtUtc is null || nowUtc < ExpiresAtUtc)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingNotExpiredYet));
        }

        TransitionTo(
            BookingStatusCode.Expired,
            nowUtc,
            null,
            "Expired",
            "Booking expired — hold released.");
        ExpiresAtUtc = null;
        AggregateVersion++;

        Raise(new BookingExpired(Id, BookingNumber, nowUtc));
    }

    public void Confirm(Guid paymentId, DateTime nowUtc)
    {
        EnsureStatus(BookingStatusCode.PendingPayment);
        EnsureHoldActive(nowUtc);
        PaymentId = AppGuard.NotEmpty(paymentId, nameof(paymentId));
        ConfirmedAtUtc = nowUtc;
        ExpiresAtUtc = null;

        TransitionTo(
            BookingStatusCode.Confirmed,
            nowUtc,
            null,
            "Paid",
            "Payment succeeded — booking confirmed.");
        AggregateVersion++;

        Raise(new BookingConfirmed(Id, BookingNumber, paymentId, nowUtc));
    }

    public bool ConflictsWith(BookingPeriod otherPeriod, int otherBufferDays)
        => BookingAvailability.ConflictsWithBlockingBooking(
            Period,
            BufferDays,
            otherPeriod,
            otherBufferDays);

    private static Money CalculateTotal(
        Money unitPrice,
        BookingPeriod period,
        DriverOption driver,
        DeliveryOption delivery)
    {
        var total = unitPrice.Multiply(period.Days);
        if (driver.Requested && driver.Fee is not null)
        {
            total = total.Add(driver.Fee);
        }

        if (delivery.Requested && delivery.Fee is not null)
        {
            total = total.Add(delivery.Fee);
        }

        return total;
    }

    private void TransitionTo(
        BookingStatusCode to,
        DateTime nowUtc,
        Guid? actorId,
        string timelineCode,
        string timelineMessage)
    {
        BookingStatusCode? from = _statusHistory.Count == 0 ? null : Status;
        _statusHistory.Add(BookingStatusHistoryEntry.Create(from, to, nowUtc, actorId));
        _timeline.Add(BookingTimelineEntry.Create(timelineCode, timelineMessage, nowUtc, actorId));
        Status = to;
        SetUpdatedAudit(nowUtc, actorId);
    }

    private void EnsureHoldActive(DateTime nowUtc)
    {
        if (ExpiresAtUtc is not null && nowUtc >= ExpiresAtUtc)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingHoldExpired));
        }
    }

    private void EnsureHost(Guid hostId)
    {
        if (hostId != HostId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.BookingAccessDenied));
        }
    }

    private void EnsureStatus(BookingStatusCode expected)
    {
        if (Status != expected)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingInvalidStatusTransition));
        }
    }
}
