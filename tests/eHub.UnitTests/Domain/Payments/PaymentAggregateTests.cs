using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Domain.Payments.Events;

namespace eHub.UnitTests.Domain.Payments;

public sealed class PaymentAggregateTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid BookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CurrencyId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void Create_SetsCreatedStatus_Timeline_AndWindow()
    {
        var payment = CreatePayment();

        payment.Status.Should().Be(PaymentStatusCode.Created);
        payment.BookingId.Should().Be(BookingId);
        payment.Amount.Amount.Should().Be(250m);
        payment.RefundedAmount.Amount.Should().Be(0m);
        payment.ExpiresAtUtc.Should().Be(Now.Add(PaymentDefaults.PaymentWindow));
        payment.Timeline.Should().ContainSingle(t => t.Code == "Created");
        payment.DomainEvents.OfType<PaymentCreated>().Should().ContainSingle();
        payment.AggregateVersion.Should().Be(1);
    }

    [Fact]
    public void Create_RejectsZeroAmount()
    {
        var act = () => Payment.Create(
            BookingId,
            Money.Create(0m, CurrencyId),
            PaymentProviderCode.Test,
            "idem-1",
            Now);

        act.Should().Throw<ValidationFailedException>();
    }

    [Fact]
    public void MarkPending_ThenSucceeded_RaisesPaymentSucceeded()
    {
        var payment = CreatePayment();
        payment.MarkPending("prov_123", Now.AddMinutes(1));
        payment.ClearDomainEvents();

        payment.MarkSucceeded(Now.AddMinutes(2), "prov_123");

        payment.Status.Should().Be(PaymentStatusCode.Succeeded);
        payment.PaidAtUtc.Should().Be(Now.AddMinutes(2));
        payment.ExpiresAtUtc.Should().BeNull();
        payment.Timeline.Should().Contain(t => t.Code == "Paid");
        payment.DomainEvents.OfType<PaymentSucceeded>().Should().ContainSingle();
        payment.Attempts.Should().Contain(a => a.Kind == PaymentAttemptKind.Webhook && a.Result == PaymentAttemptResult.Succeeded);
    }

    [Fact]
    public void MarkSucceeded_FromExpired_IsRejected()
    {
        var payment = CreatePayment();
        payment.MarkPending("prov_1", Now);
        payment.MarkExpired(Now.Add(PaymentDefaults.PaymentWindow));

        var act = () => payment.MarkSucceeded(Now.AddHours(1));

        act.Should().Throw<ConflictException>();
        payment.Status.Should().Be(PaymentStatusCode.Expired);
    }

    [Fact]
    public void MarkSucceeded_IdempotentWhenAlreadySucceeded()
    {
        var payment = CreatePayment();
        payment.MarkPending("prov_1", Now);
        payment.MarkSucceeded(Now.AddMinutes(1));
        payment.ClearDomainEvents();

        payment.MarkSucceeded(Now.AddMinutes(2));

        payment.DomainEvents.Should().BeEmpty();
        payment.Status.Should().Be(PaymentStatusCode.Succeeded);
    }

    [Fact]
    public void MarkFailed_FromPending()
    {
        var payment = CreatePayment();
        payment.MarkPending("prov_1", Now);
        payment.ClearDomainEvents();

        payment.MarkFailed("card_declined", Now.AddMinutes(1));

        payment.Status.Should().Be(PaymentStatusCode.Failed);
        payment.FailureReason.Should().Be("card_declined");
        payment.DomainEvents.OfType<PaymentFailed>().Should().ContainSingle();
    }

    [Fact]
    public void AddRefund_PartialThenFull()
    {
        var payment = CreatePayment();
        payment.MarkPending("prov_1", Now);
        payment.MarkSucceeded(Now.AddMinutes(1));
        payment.ClearDomainEvents();

        payment.AddRefund(Money.Create(100m, CurrencyId), "partial", Now.AddMinutes(2));
        payment.Status.Should().Be(PaymentStatusCode.PartiallyRefunded);
        payment.RefundedAmount.Amount.Should().Be(100m);
        payment.RemainingRefundable.Amount.Should().Be(150m);

        payment.AddRefund(Money.Create(150m, CurrencyId), "rest", Now.AddMinutes(3));
        payment.Status.Should().Be(PaymentStatusCode.Refunded);
        payment.RefundedAmount.Amount.Should().Be(250m);
        payment.DomainEvents.OfType<PaymentRefunded>().Should().HaveCount(2);
        payment.DomainEvents.OfType<PaymentRefunded>().Last().FullyRefunded.Should().BeTrue();
    }

    [Fact]
    public void AddRefund_RejectsWhenNotSucceeded()
    {
        var payment = CreatePayment();
        payment.MarkPending("prov_1", Now);

        var act = () => payment.AddRefund(Money.Create(10m, CurrencyId), "too early", Now);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void MarkExpired_BeforeWindow_Rejected()
    {
        var payment = CreatePayment();

        var act = () => payment.MarkExpired(Now.AddMinutes(1));

        act.Should().Throw<ValidationFailedException>();
    }

    private static Payment CreatePayment()
        => Payment.Create(
            BookingId,
            Money.Create(250m, CurrencyId),
            PaymentProviderCode.Test,
            "idem-booking-111",
            Now);
}
