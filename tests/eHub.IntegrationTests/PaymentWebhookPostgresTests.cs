using System.Text;
using System.Text.Json;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Application.Payments.Commands.ProcessWebhook;
using eHub.Domain.Bookings;
using eHub.Domain.Common;
using eHub.Domain.Payments;
using eHub.Domain.Payments.Events;
using eHub.Infrastructure.Jobs;
using eHub.Infrastructure.Payments;
using eHub.Persistence;
using eHub.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eHub.IntegrationTests;

[Collection("PostgresBooking")]
public sealed class PaymentWebhookPostgresTests
{
    private const string WebhookSecret = "pg-test-webhook-secret";
    private static readonly Guid CurrencyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly PostgresBookingFixture _fixture;

    public PaymentWebhookPostgresTests(PostgresBookingFixture fixture)
    {
        _fixture = fixture;
    }

    private void RequirePostgres()
        => Skip.If(!_fixture.IsAvailable, "PostgreSQL Testcontainer unavailable (Docker required).");

    [SkippableFact]
    public async Task Migrate_CreatesPaymentWebhookInboxTable()
    {
        RequirePostgres();

        await using var db = _fixture.CreateDbContext();
        var table = await db.Database.SqlQueryRaw<string>(
                """
                SELECT tablename AS "Value"
                FROM pg_tables
                WHERE schemaname = 'public' AND tablename = 'payment_webhook_inbox'
                """)
            .SingleOrDefaultAsync();
        table.Should().Be("payment_webhook_inbox");

        var index = await db.Database.SqlQueryRaw<string>(
                """
                SELECT indexname AS "Value"
                FROM pg_indexes
                WHERE indexname = 'IX_payment_webhook_inbox_Provider_ProviderEventId'
                """)
            .SingleOrDefaultAsync();
        index.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task SuccessWebhook_UpdatesPayment_WritesOutbox_AndConfirmsBooking()
    {
        RequirePostgres();

        await using var provider = _fixture.CreatePaymentServices(new FixedClock(Now));
        await using var scope = provider.CreateAsyncScope();
        var bookings = scope.ServiceProvider.GetRequiredService<EfBookingRepository>();
        var payments = scope.ServiceProvider.GetRequiredService<EfPaymentRepository>();
        var outbox = scope.ServiceProvider.GetRequiredService<EfOutboxWriter>();
        var webhook = scope.ServiceProvider.GetRequiredService<ProcessWebhookCommandHandler>();
        var processor = scope.ServiceProvider.GetRequiredService<PaymentOutboxProcessor>();
        var uow = scope.ServiceProvider.GetRequiredService<EfUnitOfWork>();

        var booking = InstantBook();
        await bookings.AddAsync(booking, Now);
        await uow.SaveChangesAsync();

        var payment = Payment.Create(
            booking.Id,
            booking.TotalPrice,
            PaymentProviderCode.Test,
            $"idem-{Guid.NewGuid():N}",
            Now);
        payment.MarkPending($"fake_{payment.Id:N}", Now);
        await payments.AddAsync(payment);
        foreach (var domainEvent in payment.DomainEvents)
        {
            await outbox.EnqueueAsync(domainEvent, Now);
        }

        payment.ClearDomainEvents();
        await uow.SaveChangesAsync();

        var body = WebhookBody(
            $"pg-evt-ok-{Guid.NewGuid():N}",
            "SUCCEEDED",
            payment.Id,
            payment.ProviderPaymentId,
            amount: payment.Amount.Amount);
        var first = await webhook.Handle(
            new ProcessWebhookCommand(PaymentProviderCodes.Test, SignedHeaders(body), Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);
        var second = await webhook.Handle(
            new ProcessWebhookCommand(PaymentProviderCodes.Test, SignedHeaders(body), Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);

        first.Code.Should().Be("processed");
        second.Code.Should().Be("duplicate");

        await using var readDb = _fixture.CreateDbContext();
        var loadedPayment = await readDb.Payments.SingleAsync(p => p.Id == payment.Id);
        loadedPayment.Status.Should().Be(PaymentStatusCode.Succeeded);

        var succeededOutbox = await readDb.OutboxMessages
            .CountAsync(m => m.Type == nameof(PaymentSucceeded) && m.ProcessedAtUtc == null);
        succeededOutbox.Should().Be(1);

        var inboxRows = await readDb.PaymentWebhookInbox.CountAsync(x => x.ProviderEventId == "pg-evt-1");
        inboxRows.Should().Be(1);

        await processor.RunOnceAsync();

        var loadedBooking = await readDb.Bookings.SingleAsync(b => b.Id == booking.Id);
        loadedBooking.Status.Should().Be(BookingStatusCode.Confirmed);
    }

    [SkippableFact]
    public async Task LateSuccess_DoesNotConfirmExpiredBooking()
    {
        RequirePostgres();

        await using var provider = _fixture.CreatePaymentServices(new FixedClock(Now));
        await using var scope = provider.CreateAsyncScope();
        var bookings = scope.ServiceProvider.GetRequiredService<EfBookingRepository>();
        var payments = scope.ServiceProvider.GetRequiredService<EfPaymentRepository>();
        var outbox = scope.ServiceProvider.GetRequiredService<EfOutboxWriter>();
        var webhook = scope.ServiceProvider.GetRequiredService<ProcessWebhookCommandHandler>();
        var processor = scope.ServiceProvider.GetRequiredService<PaymentOutboxProcessor>();
        var uow = scope.ServiceProvider.GetRequiredService<EfUnitOfWork>();

        var booking = InstantBook(createdAtUtc: Now.AddHours(-1));
        booking.Expire(Now);
        await bookings.AddAsync(booking, Now);

        var payment = Payment.Create(
            booking.Id,
            booking.TotalPrice,
            PaymentProviderCode.Test,
            $"idem-{Guid.NewGuid():N}",
            Now);
        payment.MarkPending($"fake_{payment.Id:N}", Now);
        await payments.AddAsync(payment);
        payment.ClearDomainEvents();
        await uow.SaveChangesAsync();

        var body = WebhookBody(
            $"pg-evt-late-{Guid.NewGuid():N}",
            "SUCCEEDED",
            payment.Id,
            payment.ProviderPaymentId,
            amount: payment.Amount.Amount);
        var result = await webhook.Handle(
            new ProcessWebhookCommand(PaymentProviderCodes.Test, SignedHeaders(body), Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);

        result.Code.Should().Be("processed");
        await processor.RunOnceAsync();

        await using var readDb = _fixture.CreateDbContext();
        var loadedBooking = await readDb.Bookings.SingleAsync(b => b.Id == booking.Id);
        loadedBooking.Status.Should().Be(BookingStatusCode.Expired);
    }

    [SkippableFact]
    public async Task FailedWebhook_MarksPaymentFailed_WithoutConfirmingBooking()
    {
        RequirePostgres();

        await using var provider = _fixture.CreatePaymentServices(new FixedClock(Now));
        await using var scope = provider.CreateAsyncScope();
        var bookings = scope.ServiceProvider.GetRequiredService<EfBookingRepository>();
        var payments = scope.ServiceProvider.GetRequiredService<EfPaymentRepository>();
        var webhook = scope.ServiceProvider.GetRequiredService<ProcessWebhookCommandHandler>();
        var uow = scope.ServiceProvider.GetRequiredService<EfUnitOfWork>();

        var booking = InstantBook();
        await bookings.AddAsync(booking, Now);

        var payment = Payment.Create(
            booking.Id,
            booking.TotalPrice,
            PaymentProviderCode.Test,
            $"idem-{Guid.NewGuid():N}",
            Now);
        payment.MarkPending($"fake_{payment.Id:N}", Now);
        await payments.AddAsync(payment);
        payment.ClearDomainEvents();
        await uow.SaveChangesAsync();

        var body = WebhookBody(
            $"pg-evt-fail-{Guid.NewGuid():N}",
            "FAILED",
            payment.Id,
            payment.ProviderPaymentId,
            failureReason: "declined",
            amount: payment.Amount.Amount);
        var result = await webhook.Handle(
            new ProcessWebhookCommand(PaymentProviderCodes.Test, SignedHeaders(body), Encoding.UTF8.GetBytes(body)),
            CancellationToken.None);

        result.Code.Should().Be("processed");

        await using var readDb = _fixture.CreateDbContext();
        var loadedPayment = await readDb.Payments.SingleAsync(p => p.Id == payment.Id);
        loadedPayment.Status.Should().Be(PaymentStatusCode.Failed);
        var loadedBooking = await readDb.Bookings.SingleAsync(b => b.Id == booking.Id);
        loadedBooking.Status.Should().Be(BookingStatusCode.PendingPayment);
    }

    private static Booking InstantBook(DateTime? createdAtUtc = null)
    {
        var at = createdAtUtc ?? Now;
        return Booking.CreateRequest(
            $"BK-IT-{Guid.NewGuid():N}"[..32],
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BookingPeriod.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5)),
            Money.Create(100m, CurrencyId),
            BookingAssetSnapshot.Create("Car", Guid.NewGuid(), at),
            BookingTerms.Create(1),
            at,
            instantBook: true);
    }

    private static string WebhookBody(
        string eventId,
        string outcome,
        Guid paymentId,
        string? providerPaymentId,
        string? failureReason = null,
        decimal amount = 500m)
    {
        var payload = new
        {
            eventId,
            outcome,
            paymentId,
            providerPaymentId,
            amount,
            currencyId = CurrencyId,
            failureReason,
            occurredAtUtc = Now
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static Dictionary<string, string> SignedHeaders(string body)
    {
        var unix = new DateTimeOffset(Now).ToUnixTimeSeconds();
        var sig = FakePaymentProvider.Sign(WebhookSecret, unix, body);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [FakePaymentProvider.TimestampHeader] = unix.ToString(),
            [FakePaymentProvider.SignatureHeader] = sig
        };
    }
}
