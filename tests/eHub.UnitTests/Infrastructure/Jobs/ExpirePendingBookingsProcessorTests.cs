using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Domain.Bookings;
using eHub.Domain.Common;
using eHub.Infrastructure.Jobs;
using eHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace eHub.UnitTests.Infrastructure.Jobs;

public sealed class ExpirePendingBookingsProcessorTests
{
    private static readonly DateTime Now = new(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CurrencyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid HostId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task RunOnce_ExpiresHold_WritesOutbox_AndSaves()
    {
        var bookings = new InMemoryBookingRepository();
        var outbox = Substitute.For<IOutboxWriter>();
        var notifier = Substitute.For<IBookingExpiryNotifier>();
        var metrics = Substitute.For<IExpireBookingsMetrics>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = new FixedTestClock(Now);

        var createAt = Now.AddHours(-13);
        var hold = Booking.CreateRequest(
            "BK-2026-000000099",
            Guid.NewGuid(),
            Guid.NewGuid(),
            HostId,
            BookingPeriod.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3)),
            Money.Create(100m, CurrencyId),
            BookingAssetSnapshot.Create("Asset", HostId, createAt),
            BookingTerms.Create(1),
            createAt);

        await bookings.AddAsync(hold, createAt);

        var processor = new ExpirePendingBookingsProcessor(
            bookings,
            outbox,
            notifier,
            metrics,
            uow,
            clock,
            Options.Create(new JobsOptions
            {
                ExpirePendingBookings = new ExpirePendingBookingsOptions { BatchSize = 10 }
            }),
            NullLogger<ExpirePendingBookingsProcessor>.Instance);

        var count = await processor.RunOnceAsync();

        count.Should().Be(1);
        hold.Status.Should().Be(BookingStatusCode.Expired);
        await outbox.Received(1).EnqueueAsync(
            Arg.Any<IDomainEvent>(),
            Now,
            Arg.Any<CancellationToken>());
        await notifier.Received(1).NotifyExpiredAsync(hold, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        metrics.Received(1).RecordBatch(1, 0, Arg.Any<TimeSpan>());
    }

    private sealed class FixedTestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
