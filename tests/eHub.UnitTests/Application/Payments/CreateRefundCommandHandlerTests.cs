using eHub.Application.Bookings.Abstractions;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Authorization;
using eHub.Application.Payments.Abstractions;
using eHub.Application.Payments.Commands.CreateRefund;
using eHub.Application.Payments;
using eHub.Domain.Catalog;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Domain.Payments.Events;

namespace eHub.UnitTests.Application.Payments;

public sealed class CreateRefundCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 14, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CurrencyId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly ICurrencyRepository _currencies = Substitute.For<ICurrencyRepository>();
    private readonly IMinorUnitConverter _minorUnits = new MinorUnitConverter();
    private readonly IPaymentProviderResolver _providerResolver = Substitute.For<IPaymentProviderResolver>();
    private readonly IPaymentProvider _provider = Substitute.For<IPaymentProvider>();
    private readonly IOutboxWriter _outbox = Substitute.For<IOutboxWriter>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CreateRefundCommandHandler _handler;

    public CreateRefundCommandHandlerTests()
    {
        _currentUser.RequireUserId().Returns(AdminId);
        _currentUser.HasPermission(AuthPolicies.PaymentsRefund).Returns(true);
        _currentUser.IsInRole("Admin").Returns(true);
        _clock.UtcNow.Returns(Now);
        _provider.RefundAsync(Arg.Any<ProviderRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(ProviderRefundResult.Success("re_test_1"));
        _providerResolver.GetRequired(Arg.Any<string>()).Returns(_provider);
        _currencies.GetByIdAsync(CurrencyId, Arg.Any<CancellationToken>())
            .Returns(Currency.Create("AZN", "Manat", "₼", Now));
        _handler = new CreateRefundCommandHandler(
            _currentUser,
            _payments,
            _currencies,
            _minorUnits,
            _providerResolver,
            _outbox,
            _clock,
            _uow);
    }

    [Fact]
    public async Task Handle_PartialRefund_Succeeds_AndEnqueuesOutbox()
    {
        var payment = SucceededPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(
            new CreateRefundCommand(payment.Id, 100m, "partial", "ref-1"),
            CancellationToken.None);

        result.Status.Should().Be(RefundStatusCode.Succeeded.Value);
        result.Amount.Should().Be(100m);
        result.PaymentRefundedAmount.Should().Be(100m);
        result.PaymentStatus.Should().Be(PaymentStatusCode.PartiallyRefunded.Value);
        payment.Refunds.Should().ContainSingle(r => r.IdempotencyKey == "ref-1");
        await _outbox.Received(4).EnqueueAsync(Arg.Any<IDomainEvent>(), Now, Arg.Any<CancellationToken>());
        await _outbox.Received(1).EnqueueAsync(Arg.Is<IDomainEvent>(e => e is RefundRequested), Now, Arg.Any<CancellationToken>());
        await _outbox.Received(1).EnqueueAsync(Arg.Is<IDomainEvent>(e => e is RefundSucceeded), Now, Arg.Any<CancellationToken>());
        await _provider.Received(1).RefundAsync(Arg.Any<ProviderRefundRequest>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FullRefund_SetsPaymentRefunded()
    {
        var payment = SucceededPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(
            new CreateRefundCommand(payment.Id, 250m, "full", "ref-full"),
            CancellationToken.None);

        result.PaymentStatus.Should().Be(PaymentStatusCode.Refunded.Value);
        result.PaymentRefundedAmount.Should().Be(250m);
    }

    [Fact]
    public async Task Handle_IdempotentReplay_ReturnsExistingWithoutProviderCall()
    {
        var payment = SucceededPayment();
        payment.AddRefund(Money.Create(50m, CurrencyId), "earlier", Now, AdminId, "re_old", "ref-1");
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(
            new CreateRefundCommand(payment.Id, 50m, "earlier", "ref-1"),
            CancellationToken.None);

        result.RefundId.Should().Be(payment.Refunds.Single().Id);
        await _provider.DidNotReceive().RefundAsync(Arg.Any<ProviderRefundRequest>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdempotentReplay_DifferentAmount_Conflict()
    {
        var payment = SucceededPayment();
        payment.AddRefund(Money.Create(50m, CurrencyId), "earlier", Now, AdminId, "re_old", "ref-1");
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var act = () => _handler.Handle(
            new CreateRefundCommand(payment.Id, 60m, "earlier", "ref-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_NonAdmin_Forbidden()
    {
        _currentUser.IsInRole("Admin").Returns(false);
        var payment = SucceededPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var act = () => _handler.Handle(
            new CreateRefundCommand(payment.Id, 10m, "ops", "ref-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_WithoutPermission_Forbidden()
    {
        _currentUser.HasPermission(AuthPolicies.PaymentsRefund).Returns(false);
        var payment = SucceededPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var act = () => _handler.Handle(
            new CreateRefundCommand(payment.Id, 10m, "ops", "ref-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_FailedPayment_Rejected()
    {
        var payment = Payment.Create(
            Guid.NewGuid(),
            Money.Create(250m, CurrencyId),
            PaymentProviderCode.Test,
            $"pay-{Guid.NewGuid():N}",
            Now);
        payment.MarkPending("fake_prov_1", Now);
        payment.MarkFailed("declined", Now.AddMinutes(1));
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var act = () => _handler.Handle(
            new CreateRefundCommand(payment.Id, 10m, "too late", "ref-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_AmountExceedsRemaining_Rejected()
    {
        var payment = SucceededPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var act = () => _handler.Handle(
            new CreateRefundCommand(payment.Id, 300m, "too much", "ref-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationFailedException>();
    }

    [Fact]
    public async Task Handle_ProviderFailure_MarksRefundFailed()
    {
        _provider.RefundAsync(Arg.Any<ProviderRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(ProviderRefundResult.Failed(new ProviderFailure(
                PaymentFailureReason.ProviderUnavailable,
                ProviderCode: "provider_down",
                SafeMessage: "Provider unavailable.",
                IsRetryable: true)));
        var payment = SucceededPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(
            new CreateRefundCommand(payment.Id, 25m, "retry later", "ref-fail"),
            CancellationToken.None);

        result.Status.Should().Be(RefundStatusCode.Failed.Value);
        payment.RefundedAmount.Amount.Should().Be(0m);
        await _outbox.Received(1).EnqueueAsync(Arg.Is<IDomainEvent>(e => e is RefundFailed), Now, Arg.Any<CancellationToken>());
    }

    private static Payment SucceededPayment()
    {
        var payment = Payment.Create(
            Guid.NewGuid(),
            Money.Create(250m, CurrencyId),
            PaymentProviderCode.Test,
            $"pay-{Guid.NewGuid():N}",
            Now);
        payment.MarkPending("fake_prov_1", Now);
        payment.MarkSucceeded(Now);
        payment.ClearDomainEvents();
        return payment;
    }
}
