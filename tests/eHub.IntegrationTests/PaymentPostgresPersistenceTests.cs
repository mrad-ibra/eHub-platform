using eHub.Domain.Bookings;
using eHub.Domain.Common;
using eHub.Domain.Payments;
using eHub.Domain.Exceptions;
using eHub.Localization;
using eHub.Persistence;
using eHub.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eHub.IntegrationTests;

[Collection("PostgresBooking")]
public sealed class PaymentPostgresPersistenceTests
{
    private readonly PostgresBookingFixture _fixture;

    public PaymentPostgresPersistenceTests(PostgresBookingFixture fixture)
    {
        _fixture = fixture;
    }

    private void RequirePostgres()
        => Skip.If(!_fixture.IsAvailable, "PostgreSQL Testcontainer unavailable (Docker required).");

    [SkippableFact]
    public async Task Migrate_CreatesPaymentTablesAndActiveUniqueIndex()
    {
        RequirePostgres();

        await using var db = _fixture.CreateDbContext();

        var table = await db.Database.SqlQueryRaw<string>(
                """
                SELECT tablename AS "Value"
                FROM pg_tables
                WHERE schemaname = 'public' AND tablename = 'payments'
                """)
            .SingleOrDefaultAsync();
        table.Should().Be("payments");

        var index = await db.Database.SqlQueryRaw<string>(
                """
                SELECT indexname AS "Value"
                FROM pg_indexes
                WHERE indexname = 'ux_payments_one_active_per_booking'
                """)
            .SingleOrDefaultAsync();
        index.Should().Be("ux_payments_one_active_per_booking");
    }

    [SkippableFact]
    public async Task Insert_PersistsMoney_Timeline_AndOutbox()
    {
        RequirePostgres();

        await using var provider = _fixture.CreateServices();
        await using var scope = provider.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<EfPaymentRepository>();
        var outbox = scope.ServiceProvider.GetRequiredService<EfOutboxWriter>();
        var uow = scope.ServiceProvider.GetRequiredService<EfUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<eHub.Application.Common.Time.IClock>();

        var bookingId = Guid.NewGuid();
        var currency = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var payment = Payment.Create(
            bookingId,
            Money.Create(250m, currency),
            PaymentProviderCode.Test,
            $"idem-{Guid.NewGuid():N}",
            clock.UtcNow);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await outbox.EnqueueAsync(domainEvent, clock.UtcNow);
        }

        payment.ClearDomainEvents();
        await payments.AddAsync(payment);
        await uow.SaveChangesAsync();

        await using var readDb = _fixture.CreateDbContext();
        var loaded = await readDb.Payments
            .Include(p => p.Timeline)
            .Include(p => p.StatusHistory)
            .SingleAsync(p => p.Id == payment.Id);

        loaded.Amount.Amount.Should().Be(250m);
        loaded.RefundedAmount.Amount.Should().Be(0m);
        loaded.Status.Should().Be(PaymentStatusCode.Created);
        loaded.Timeline.Should().ContainSingle(t => t.Code == "Created");
        loaded.AggregateVersion.Should().Be(1);

        var outboxCount = await readDb.OutboxMessages.CountAsync(o => o.Type == "PaymentCreated");
        outboxCount.Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task ActiveUnique_RejectsSecondActivePaymentForSameBooking()
    {
        RequirePostgres();

        await using var provider = _fixture.CreateServices();
        await using var scope = provider.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<EfPaymentRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<EfUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<eHub.Application.Common.Time.IClock>();

        var bookingId = Guid.NewGuid();
        var currency = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var first = Payment.Create(
            bookingId,
            Money.Create(100m, currency),
            PaymentProviderCode.Test,
            $"idem-a-{Guid.NewGuid():N}",
            clock.UtcNow);
        await payments.AddAsync(first);
        await uow.SaveChangesAsync();

        var second = Payment.Create(
            bookingId,
            Money.Create(100m, currency),
            PaymentProviderCode.Test,
            $"idem-b-{Guid.NewGuid():N}",
            clock.UtcNow);
        await payments.AddAsync(second);

        var act = () => uow.SaveChangesAsync();
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(ErrorResources.Get(ErrorCodes.PaymentActiveAlreadyExists));
    }
}
