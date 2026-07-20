using System.Text;
using System.Text.Json;
using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Application.Payments.Commands.ProcessWebhook;
using eHub.Domain.Common;
using eHub.Domain.Payments;
using eHub.Domain.Payments.Events;
using eHub.Infrastructure.Payments;
using eHub.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Application.Payments;

public sealed class ProcessWebhookCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
    private const string Secret = "test-webhook-secret";
    private static readonly Guid CurrencyId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly IPaymentProviderResolver _providers = new PaymentProviderResolver([
        new FakePaymentProvider(Options.Create(new PaymentProviderOptions
        {
            Fake = new FakeProviderOptions { WebhookSecret = Secret, TimestampToleranceSeconds = 300 }
        }))
    ]);
    private readonly InMemoryPaymentWebhookInboxStore _inbox = new();
    private readonly InMemoryPaymentRepository _payments = new();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ProcessWebhookCommandHandler _handler;
    private readonly IOutboxWriter _outbox;

    public ProcessWebhookCommandHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _outbox = Substitute.For<IOutboxWriter>();
        _handler = new ProcessWebhookCommandHandler(
            _providers,
            _inbox,
            _payments,
            _outbox,
            _clock,
            _uow);
    }

    [Fact]
    public async Task Handle_InvalidSignature_ReturnsUnauthorizedCode()
    {
        var body = WebhookBody("evt-1", "SUCCEEDED");
        var result = await _handler.Handle(
            new ProcessWebhookCommand(
                PaymentProviderCodes.Test,
                SignedHeaders(body, secret: "wrong"),
                Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);

        result.Accepted.Should().BeFalse();
        result.Code.Should().Be("invalid_signature");
    }

    [Fact]
    public async Task Handle_ExpiredTimestamp_ReturnsInvalidSignature()
    {
        var body = WebhookBody("evt-2", "SUCCEEDED");
        var oldUnix = new DateTimeOffset(Now.AddHours(-2)).ToUnixTimeSeconds();
        var result = await _handler.Handle(
            new ProcessWebhookCommand(
                PaymentProviderCodes.Test,
                SignedHeaders(body, unix: oldUnix),
                Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);

        result.Code.Should().Be("invalid_signature");
    }

    [Fact]
    public async Task Handle_UnknownProvider_ReturnsNotFoundCode()
    {
        var body = WebhookBody("evt-3", "SUCCEEDED");
        var result = await _handler.Handle(
            new ProcessWebhookCommand(
                "UNKNOWN",
                SignedHeaders(body),
                Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);

        result.Accepted.Should().BeFalse();
        result.Code.Should().Be("unknown_provider");
    }

    [Fact]
    public async Task Handle_SuccessWebhook_UpdatesPayment_AndEnqueuesOutbox()
    {
        var payment = PendingPayment();
        await _payments.AddAsync(payment);

        var body = WebhookBody("evt-4", "SUCCEEDED", payment.Id, payment.ProviderPaymentId);
        var result = await Send(body);

        result.Code.Should().Be("processed");
        payment.Status.Should().Be(PaymentStatusCode.Succeeded);
        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<IDomainEvent>(e => e.GetType().Name == nameof(PaymentSucceeded)),
            Now,
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateWebhook_IsProcessedOnce()
    {
        var payment = PendingPayment();
        await _payments.AddAsync(payment);
        var body = WebhookBody("evt-5", "SUCCEEDED", payment.Id, payment.ProviderPaymentId);

        var first = await Send(body);
        var second = await Send(body);

        first.Code.Should().Be("processed");
        second.Code.Should().Be("duplicate");
        payment.Status.Should().Be(PaymentStatusCode.Succeeded);
        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<IDomainEvent>(e => e.GetType().Name == nameof(PaymentSucceeded)),
            Now,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailedWebhook_MarksPaymentFailed()
    {
        var payment = PendingPayment();
        await _payments.AddAsync(payment);
        var body = WebhookBody("evt-6", "FAILED", payment.Id, payment.ProviderPaymentId, failureReason: "declined");

        var result = await Send(body);

        result.Code.Should().Be("processed");
        payment.Status.Should().Be(PaymentStatusCode.Failed);
        payment.FailureReason.Should().Be("declined");
    }

    [Fact]
    public async Task Handle_ParseWebhookThrows_ReturnsUnparseableNot500()
    {
        var resolver = new PaymentProviderResolver([new ThrowingParsePaymentProvider()]);
        var handler = new ProcessWebhookCommandHandler(
            resolver,
            _inbox,
            _payments,
            _outbox,
            _clock,
            _uow);

        var body = WebhookBody("evt-throw", "SUCCEEDED");
        var result = await handler.Handle(
            new ProcessWebhookCommand(
                PaymentProviderCodes.Test,
                SignedHeaders(body),
                Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);

        result.Code.Should().Be("unparseable");
        result.Accepted.Should().BeTrue();
    }

    private sealed class ThrowingParsePaymentProvider : IPaymentProvider
    {
        public string ProviderKey => PaymentProviderCodes.Test;

        public Task<ProviderCreatePaymentResult> CreatePaymentAsync(
            ProviderCreatePaymentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ProviderCreatePaymentResult.Success("x"));

        public Task<ProviderCancelResult> CancelPaymentAsync(
            string providerPaymentId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ProviderCancelResult.Success());

        public Task<ProviderRefundResult> RefundAsync(
            ProviderRefundRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ProviderRefundResult.Success("re_x"));

        public bool VerifyWebhook(
            IReadOnlyDictionary<string, string> headers,
            ReadOnlySpan<byte> rawBody,
            DateTime nowUtc)
            => true;

        public ProviderWebhookEvent? ParseWebhook(ReadOnlySpan<byte> rawBody)
            => throw new InvalidOperationException("bad payload");
    }

    [Fact]
    public async Task Handle_UnknownEventType_IsAcknowledgedSafely()
    {
        var body = WebhookBody("evt-7", "WEIRD_EVENT");
        var result = await Send(body);

        result.Code.Should().Be("ignored");
    }

    [Fact]
    public async Task Handle_IllegalTransition_IsIgnoredWithoutThrowing()
    {
        var payment = Payment.Create(
            Guid.NewGuid(),
            Money.Create(100m, CurrencyId),
            PaymentProviderCode.Test,
            "idem-illegal",
            Now);
        payment.MarkPending("fake_1", Now);
        payment.MarkFailed("already_failed", Now);
        await _payments.AddAsync(payment);

        var body = WebhookBody("evt-8", "SUCCEEDED", payment.Id, payment.ProviderPaymentId);
        var result = await Send(body);

        result.Code.Should().Be("ignored_transition");
        payment.Status.Should().Be(PaymentStatusCode.Failed);
    }

    private Task<ProcessWebhookResult> Send(string body)
        => _handler.Handle(
            new ProcessWebhookCommand(
                PaymentProviderCodes.Test,
                SignedHeaders(body),
                Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);

    private static Payment PendingPayment()
    {
        var payment = Payment.Create(
            Guid.NewGuid(),
            Money.Create(100m, CurrencyId),
            PaymentProviderCode.Test,
            $"idem-{Guid.NewGuid():N}",
            Now);
        payment.MarkPending($"fake_{payment.Id:N}", Now);
        return payment;
    }

    private static string WebhookBody(
        string eventId,
        string outcome,
        Guid? paymentId = null,
        string? providerPaymentId = null,
        string? failureReason = null)
    {
        var payload = new
        {
            eventId,
            outcome,
            paymentId,
            providerPaymentId,
            amount = 100m,
            currencyId = CurrencyId,
            failureReason,
            occurredAtUtc = Now
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static Dictionary<string, string> SignedHeaders(string body, string? secret = null, long? unix = null)
    {
        var ts = unix ?? new DateTimeOffset(Now).ToUnixTimeSeconds();
        var sig = FakePaymentProvider.Sign(secret ?? Secret, ts, body);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [FakePaymentProvider.TimestampHeader] = ts.ToString(),
            [FakePaymentProvider.SignatureHeader] = sig
        };
    }
}
