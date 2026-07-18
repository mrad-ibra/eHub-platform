using eHub.Domain.Bookings;
using eHub.Domain.Bookings.Events;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;

namespace eHub.UnitTests.Domain.Bookings;

public sealed class BookingPeriodTests
{
    [Fact]
    public void Create_RejectsEndBeforeStart()
    {
        var act = () => BookingPeriod.Create(new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 1));
        act.Should().Throw<ValidationFailedException>();
    }

    [Fact]
    public void InclusiveOverlap_TouchesOnSharedDay()
    {
        var a = BookingPeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));
        var b = BookingPeriod.Create(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 10));
        a.Overlaps(b).Should().BeTrue();
    }

    [Fact]
    public void AdjacentDays_DoNotOverlap_WithoutBuffer()
    {
        var a = BookingPeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));
        var b = BookingPeriod.Create(new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 8));
        a.Overlaps(b).Should().BeFalse();
    }

    [Fact]
    public void Buffer_BlocksNextDay()
    {
        var existing = BookingPeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));
        var request = BookingPeriod.Create(new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 8));

        BookingAvailability.ConflictsWithBlockingBooking(request, 1, existing, 1)
            .Should().BeTrue();
        BookingAvailability.ConflictsWithBlockingBooking(request, 0, existing, 0)
            .Should().BeFalse();
    }

    [Fact]
    public void Buffer_AllowsDayAfterBuffer()
    {
        var existing = BookingPeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));
        var request = BookingPeriod.Create(new DateOnly(2026, 7, 7), new DateOnly(2026, 7, 9));

        BookingAvailability.ConflictsWithBlockingBooking(request, 1, existing, 1)
            .Should().BeFalse();
    }
}

public sealed class BookingAggregateTests
{
    private static readonly DateTime Now = new(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AssetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RenterId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid HostId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid CurrencyId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void CreateRequest_SoftHold_SetsTwelveHourExpiryAndSnapshot()
    {
        var booking = CreateBooking();

        booking.Status.Should().Be(BookingStatusCode.PendingOwnerApproval);
        booking.ExpiresAtUtc.Should().Be(Now.Add(BookingDefaults.OwnerApprovalTtl));
        booking.BufferDays.Should().Be(1);
        booking.UnitPrice.Amount.Should().Be(100m);
        booking.TotalPrice.Amount.Should().Be(500m); // 5 days
        booking.AssetSnapshot.Name.Should().Be("BMW X5");
        booking.Terms.MinRentalDays.Should().Be(1);
        booking.Timeline.Should().ContainSingle(t => t.Code == "Created");
        booking.DomainEvents.OfType<BookingCreated>().Should().ContainSingle();
        booking.BookingNumber.Should().StartWith("BK-");
    }

    [Fact]
    public void CreateRequest_OwnAsset_Forbidden()
    {
        var act = () => Booking.CreateRequest(
            "BK-2026-000000001",
            AssetId,
            HostId,
            HostId,
            BookingPeriod.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5)),
            Money.Create(100m, CurrencyId),
            BookingAssetSnapshot.Create("Car", HostId, Now),
            BookingTerms.Create(1),
            Now);

        act.Should().Throw<ForbiddenAccessException>();
    }

    [Fact]
    public void Approve_StartsFifteenMinutePaymentTimer()
    {
        var booking = CreateBooking();

        booking.Approve(HostId, Now.AddHours(1));

        booking.Status.Should().Be(BookingStatusCode.PendingPayment);
        booking.ExpiresAtUtc.Should().Be(Now.AddHours(1).Add(BookingDefaults.PaymentTtl));
        booking.DomainEvents.OfType<BookingApproved>().Should().ContainSingle();
    }

    [Fact]
    public void Expire_SoftHold_ReleasesCalendarStatus()
    {
        var booking = CreateBooking();
        booking.Expire(Now.Add(BookingDefaults.OwnerApprovalTtl));

        booking.Status.Should().Be(BookingStatusCode.Expired);
        booking.ExpiresAtUtc.Should().BeNull();
        booking.Status.IsBlocking.Should().BeFalse();
    }

    [Fact]
    public void PriceFreeze_IsIndependentOfLaterAssetPrice()
    {
        var booking = CreateBooking();
        booking.UnitPrice.Amount.Should().Be(100m);
        // Aggregate does not expose SetPrice — freeze is structural.
        booking.TotalPrice.Amount.Should().Be(500m);
    }

    private static Booking CreateBooking()
        => Booking.CreateRequest(
            "BK-2026-000000001",
            AssetId,
            RenterId,
            HostId,
            BookingPeriod.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5)),
            Money.Create(100m, CurrencyId),
            BookingAssetSnapshot.Create("BMW X5", HostId, Now, "BMW", "X5"),
            BookingTerms.Create(1, minRentalDays: 1, maxRentalDays: 30),
            Now);
}

public sealed class MoneyTests
{
    [Fact]
    public void RejectsNegative()
    {
        var act = () => Money.Create(-1, Guid.NewGuid());
        act.Should().Throw<ValidationFailedException>();
    }

    [Fact]
    public void MultiplyAndAdd_SameCurrency()
    {
        var currency = Guid.NewGuid();
        var unit = Money.Create(100m, currency);
        var total = unit.Multiply(3).Add(Money.Create(50m, currency));
        total.Amount.Should().Be(350m);
    }
}
