using eHub.Application.Common.Context;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Authorization;
using eHub.Application.Payments.Commands.CreateRefund;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Persistence;
using eHub.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace eHub.IntegrationTests;

[Collection("PostgresBooking")]
public sealed class PaymentRefundPostgresTests
{
    private readonly PostgresBookingFixture _fixture;

    public PaymentRefundPostgresTests(PostgresBookingFixture fixture)
    {
        _fixture = fixture;
    }

    private void RequirePostgres()
        => Skip.If(!_fixture.IsAvailable, "PostgreSQL Testcontainer unavailable (Docker required).");

    [SkippableFact]
    public async Task CreateRefund_PersistsRefund_IdempotencyKey_AndOutbox()
    {
        RequirePostgres();

        var paymentId = await SeedSucceededPaymentAsync(250m);
        await using var provider = _fixture.CreateRefundServices();
        await using var scope = provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateRefundCommandHandler>();

        var result = await handler.Handle(
            new CreateRefundCommand(paymentId, 100m, "customer request", "ref-pg-1"),
            CancellationToken.None);

        result.Status.Should().Be(RefundStatusCode.Succeeded.Value);
        result.PaymentRefundedAmount.Should().Be(100m);

        await using var readDb = _fixture.CreateDbContext();
        var refund = readDb.Set<Refund>().Single(r => r.Id == result.RefundId);
        refund.IdempotencyKey.Should().Be("ref-pg-1");

        var paymentIdText = paymentId.ToString();
        var outboxTypes = readDb.OutboxMessages
            .AsEnumerable()
            .Where(o => o.PayloadJson.Contains(paymentIdText, StringComparison.Ordinal))
            .Select(o => o.Type)
            .ToList();
        outboxTypes.Should().Contain("RefundRequested");
        outboxTypes.Should().Contain("RefundSucceeded");
        outboxTypes.Should().Contain("PaymentPartiallyRefunded");
    }

    [SkippableFact]
    public async Task CreateRefund_DuplicateIdempotencyKey_ReturnsSameResult()
    {
        RequirePostgres();

        var paymentId = await SeedSucceededPaymentAsync(250m);
        await using var provider = _fixture.CreateRefundServices();

        CreateRefundResult first;
        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<CreateRefundCommandHandler>();
            first = await handler.Handle(
                new CreateRefundCommand(paymentId, 40m, "dup test", "ref-dup"),
                CancellationToken.None);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<CreateRefundCommandHandler>();
            var second = await handler.Handle(
                new CreateRefundCommand(paymentId, 40m, "dup test", "ref-dup"),
                CancellationToken.None);

            second.RefundId.Should().Be(first.RefundId);
            second.PaymentRefundedAmount.Should().Be(40m);
        }

        await using var readDb = _fixture.CreateDbContext();
        readDb.Set<Refund>().Count(r => r.PaymentId == paymentId).Should().Be(1);
    }

    [SkippableFact]
    public async Task ParallelPartialRefunds_CannotExceedPaymentTotal()
    {
        RequirePostgres();

        var paymentId = await SeedSucceededPaymentAsync(250m);
        await using var provider = _fixture.CreateRefundServices();
        using var gate = new ManualResetEventSlim(false);

        async Task<CreateRefundResult?> TryRefundAsync(string key)
        {
            await using var scope = provider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<CreateRefundCommandHandler>();
            gate.Wait();
            try
            {
                return await handler.Handle(
                    new CreateRefundCommand(paymentId, 150m, "parallel", key),
                    CancellationToken.None);
            }
            catch (ConflictException)
            {
                return null;
            }
            catch (ValidationFailedException)
            {
                return null;
            }
        }

        var taskA = Task.Run(() => TryRefundAsync("ref-par-a"));
        var taskB = Task.Run(() => TryRefundAsync("ref-par-b"));
        gate.Set();

        var results = await Task.WhenAll(taskA, taskB);
        var successes = results.Where(r => r is not null).ToList();

        successes.Should().HaveCount(1);
        successes[0]!.PaymentRefundedAmount.Should().Be(150m);

        await using var readDb = _fixture.CreateDbContext();
        var payment = await readDb.Payments.AsNoTracking().FirstAsync(p => p.Id == paymentId);
        payment.RefundedAmount.Amount.Should().Be(150m);
        readDb.Set<Refund>().Count(r => r.PaymentId == paymentId && r.Status == RefundStatusCode.Succeeded).Should().Be(1);
    }

    private async Task<Guid> SeedSucceededPaymentAsync(decimal amount)
    {
        await using var provider = _fixture.CreateServices();
        await using var scope = provider.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<EfPaymentRepository>();
        var outbox = scope.ServiceProvider.GetRequiredService<EfOutboxWriter>();
        var uow = scope.ServiceProvider.GetRequiredService<EfUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var currency = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var payment = Payment.Create(
            Guid.NewGuid(),
            Money.Create(amount, currency),
            PaymentProviderCode.Test,
            $"idem-{Guid.NewGuid():N}",
            clock.UtcNow);
        payment.MarkPending($"fake_{Guid.NewGuid():N}", clock.UtcNow);
        payment.MarkSucceeded(clock.UtcNow);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await outbox.EnqueueAsync(domainEvent, clock.UtcNow);
        }

        payment.ClearDomainEvents();
        await payments.AddAsync(payment);
        await uow.SaveChangesAsync();
        return payment.Id;
    }
}
