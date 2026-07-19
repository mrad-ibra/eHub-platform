using eHub.Application.Assets.Abstractions;
using eHub.Application.Bookings.Abstractions;
using eHub.Application.Bookings.Commands.CreateBooking;
using eHub.Application.Bookings.Services;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Assets;
using eHub.Domain.Bookings;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using NSubstitute;

namespace eHub.UnitTests.Application.Bookings;

public sealed class CreateBookingCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid RenterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HostId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CategoryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CurrencyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PeriodTypeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid CountryId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid CityId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IAssetRepository _assets = Substitute.For<IAssetRepository>();
    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IBookingNumberGenerator _numbers = Substitute.For<IBookingNumberGenerator>();
    private readonly IBookingIdempotencyStore _idempotency = Substitute.For<IBookingIdempotencyStore>();
    private readonly IBrandRepository _brands = Substitute.For<IBrandRepository>();
    private readonly IModelRepository _models = Substitute.For<IModelRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IBookingMetrics _metrics = Substitute.For<IBookingMetrics>();
    private readonly CreateBookingCommandHandler _handler;

    public CreateBookingCommandHandlerTests()
    {
        _currentUser.RequireUserId().Returns(RenterId);
        _clock.UtcNow.Returns(Now);
        _numbers.NextAsync(Arg.Any<CancellationToken>()).Returns("BK-2026-000000042");
        _idempotency.BeginAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => new IdempotencyBeginResult.Began(
                new BookingIdempotencyRecord(
                    ci.ArgAt<Guid>(0),
                    ci.ArgAt<string>(1),
                    ci.ArgAt<string>(2),
                    BookingIdempotencyStatus.Started,
                    null,
                    Now,
                    Now.AddHours(12))));
        _bookings.ListBlockingByAssetAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Booking>());

        _handler = new CreateBookingCommandHandler(
            _currentUser,
            _assets,
            _bookings,
            _numbers,
            _idempotency,
            _brands,
            _models,
            new BookingAvailabilityService(),
            _clock,
            _uow,
            _metrics);
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesSoftHold()
    {
        var asset = PublishedAsset();
        _assets.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);

        var result = await _handler.Handle(
            new CreateBookingCommand(
                asset.Id,
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 5),
                "key-1"),
            CancellationToken.None);

        result.BookingNumber.Should().Be("BK-2026-000000042");
        result.Status.Should().Be(BookingStatusCode.PendingOwnerApproval.Value);
        result.BufferDays.Should().Be(1);
        result.ExpiresAtUtc.Should().Be(Now.AddHours(12));
        result.SnapshotName.Should().Be("Toyota Corolla");
        result.AggregateVersion.Should().Be(1);
        await _bookings.Received(1).AddAsync(Arg.Any<Booking>(), Now, Arg.Any<CancellationToken>());
        await _idempotency.Received(1).CompleteAsync(RenterId, "key-1", result.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdempotentReplay_ReturnsSameBooking()
    {
        var asset = PublishedAsset();
        var existing = Booking.CreateRequest(
            "BK-2026-000000001",
            asset.Id,
            RenterId,
            HostId,
            BookingPeriod.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3)),
            Money.Create(100m, CurrencyId),
            BookingAssetSnapshot.Create(asset.Title, HostId, Now),
            BookingTerms.Create(1),
            Now);

        _idempotency.BeginAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new IdempotencyBeginResult.CompletedReplay(existing.Id));
        _bookings.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(
            new CreateBookingCommand(asset.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), "key-1"),
            CancellationToken.None);

        result.Id.Should().Be(existing.Id);
        await _bookings.DidNotReceive().AddAsync(
            Arg.Any<Booking>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdempotencyPayloadMismatch_Throws()
    {
        _idempotency.BeginAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new IdempotencyBeginResult.PayloadMismatch());

        var act = () => _handler.Handle(
            new CreateBookingCommand(Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), "key-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_SoftHoldConflict_Throws()
    {
        var asset = PublishedAsset();
        _assets.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);

        var hold = Booking.CreateRequest(
            "BK-2026-000000002",
            asset.Id,
            Guid.NewGuid(),
            HostId,
            BookingPeriod.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5)),
            Money.Create(100m, CurrencyId),
            BookingAssetSnapshot.Create(asset.Title, HostId, Now),
            BookingTerms.Create(1),
            Now);

        _bookings.ListBlockingByAssetAsync(asset.Id, Now, Arg.Any<CancellationToken>())
            .Returns(new[] { hold });

        var act = () => _handler.Handle(
            new CreateBookingCommand(asset.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4), "key-2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _idempotency.Received(1).AbandonAsync(RenterId, "key-2", Arg.Any<CancellationToken>());
    }

    private Asset PublishedAsset()
    {
        var asset = Asset.Create(HostId, CategoryId, "Toyota Corolla", Now);
        asset.SetPricing(AssetPricing.Create(CurrencyId, PeriodTypeId, 100m), Now);
        asset.SetLocation(AssetLocation.Create(CountryId, CityId), Now);
        asset.AddMedia(AssetMediaKind.Image, "https://cdn/car.jpg", Now, isPrimary: true);
        asset.SubmitForApproval(Now.AddMinutes(1));
        asset.Approve(Now.AddMinutes(2));
        return asset;
    }
}
