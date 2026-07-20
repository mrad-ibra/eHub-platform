using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Application.Payments.Abstractions;
using eHub.Application.Payments.Commands.CreatePayment;
using eHub.Domain.Bookings;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Domain.Payments.Events;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Application.Payments;

public sealed class CreatePaymentCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid RenterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HostId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CurrencyId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private const string ProviderPaymentId = "fake_provider_1";

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IPaymentProviderResolver _providerResolver = Substitute.For<IPaymentProviderResolver>();
    private readonly IPaymentProvider _provider = Substitute.For<IPaymentProvider>();
    private readonly IOutboxWriter _outbox = Substitute.For<IOutboxWriter>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CreatePaymentCommandHandler _handler;

    public CreatePaymentCommandHandlerTests()
    {
        _currentUser.RequireUserId().Returns(RenterId);
        _clock.UtcNow.Returns(Now);
        _provider.CreatePaymentAsync(Arg.Any<ProviderCreatePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(ProviderCreatePaymentResult.Success(
                ProviderPaymentId,
                "https://payments.ehub.local/fake/checkout"));
        _providerResolver.GetRequired(Arg.Any<string>()).Returns(_provider);
        _handler = new CreatePaymentCommandHandler(
            _currentUser,
            _bookings,
            _payments,
            _providerResolver,
            _outbox,
            _clock,
            _uow,
            Options.Create(new PaymentsOptions { AllowTestProvider = true }));
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesPayment_AndEnqueuesOutbox()
    {
        var booking = PayableBooking();
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _payments.GetByIdempotencyKeyAsync("pay-1", Arg.Any<CancellationToken>()).Returns((Payment?)null);
        _payments.GetActiveByBookingIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var result = await _handler.Handle(
            new CreatePaymentCommand(booking.Id, "pay-1", "TEST"),
            CancellationToken.None);

        result.BookingId.Should().Be(booking.Id);
        result.Status.Should().Be(PaymentStatusCode.Pending.Value);
        result.Amount.Should().Be(booking.TotalPrice.Amount);
        result.CurrencyId.Should().Be(CurrencyId);
        result.ExpiresAtUtc.Should().Be(Now.Add(PaymentDefaults.PaymentWindow));
        result.ProviderPaymentId.Should().Be(ProviderPaymentId);
        result.RedirectUrl.Should().Be("https://payments.ehub.local/fake/checkout");
        await _payments.Received(1).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _provider.Received(1).CreatePaymentAsync(Arg.Any<ProviderCreatePaymentRequest>(), Arg.Any<CancellationToken>());
        await _outbox.Received(2).EnqueueAsync(
            Arg.Any<IDomainEvent>(),
            Now,
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdempotentReplay_ReturnsExisting()
    {
        var booking = PayableBooking();
        var existing = Payment.Create(booking.Id, booking.TotalPrice, PaymentProviderCode.Test, "pay-1", Now);
        _payments.GetByIdempotencyKeyAsync("pay-1", Arg.Any<CancellationToken>()).Returns(existing);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        var result = await _handler.Handle(
            new CreatePaymentCommand(booking.Id, "pay-1", "TEST"),
            CancellationToken.None);

        result.Id.Should().Be(existing.Id);
        await _payments.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdempotentReplay_DifferentProvider_Conflict()
    {
        var booking = PayableBooking();
        var existing = Payment.Create(booking.Id, booking.TotalPrice, PaymentProviderCode.Test, "pay-1", Now);
        _payments.GetByIdempotencyKeyAsync("pay-1", Arg.Any<CancellationToken>()).Returns(existing);

        var act = () => _handler.Handle(
            new CreatePaymentCommand(booking.Id, "pay-1", "STRIPE"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_IdempotentReplay_DifferentUser_Forbidden()
    {
        var booking = PayableBooking(renterId: Guid.NewGuid());
        var existing = Payment.Create(booking.Id, booking.TotalPrice, PaymentProviderCode.Test, "pay-1", Now);
        _payments.GetByIdempotencyKeyAsync("pay-1", Arg.Any<CancellationToken>()).Returns(existing);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        var act = () => _handler.Handle(
            new CreatePaymentCommand(booking.Id, "pay-1", "TEST"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_TestProvider_NotAllowedWhenConfigured()
    {
        var handler = new CreatePaymentCommandHandler(
            _currentUser,
            _bookings,
            _payments,
            _providerResolver,
            _outbox,
            _clock,
            _uow,
            Options.Create(new PaymentsOptions { AllowTestProvider = false }));

        var act = () => handler.Handle(
            new CreatePaymentCommand(Guid.NewGuid(), "pay-1", "TEST"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationFailedException>();
    }

    [Fact]
    public async Task Handle_WrongRenter_Forbidden()
    {
        var booking = PayableBooking(renterId: Guid.NewGuid());
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _payments.GetByIdempotencyKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var act = () => _handler.Handle(new CreatePaymentCommand(booking.Id, "pay-1", "TEST"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_NotPendingPayment_Conflict()
    {
        var booking = Booking.CreateRequest(
            "BK-2026-000000001",
            Guid.NewGuid(),
            RenterId,
            HostId,
            BookingPeriod.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5)),
            Money.Create(100m, CurrencyId),
            BookingAssetSnapshot.Create("Car", HostId, Now),
            BookingTerms.Create(1),
            Now);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _payments.GetByIdempotencyKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var act = () => _handler.Handle(new CreatePaymentCommand(booking.Id, "pay-1", "TEST"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    private static Booking PayableBooking(Guid? renterId = null)
    {
        var booking = Booking.CreateRequest(
            "BK-2026-000000099",
            Guid.NewGuid(),
            renterId ?? RenterId,
            HostId,
            BookingPeriod.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5)),
            Money.Create(100m, CurrencyId),
            BookingAssetSnapshot.Create("Car", HostId, Now),
            BookingTerms.Create(1),
            Now,
            instantBook: true);
        return booking;
    }
}
