using eHub.Application.Bookings.Abstractions;
using eHub.Domain.Bookings;
using eHub.Domain.Exceptions;
using eHub.Persistence;
using eHub.Persistence.Entities;
using eHub.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace eHub.IntegrationTests;

[Collection("PostgresBooking")]
public sealed class BookingPostgresPersistenceTests
{
    private readonly PostgresBookingFixture _fixture;

    public BookingPostgresPersistenceTests(PostgresBookingFixture fixture)
    {
        _fixture = fixture;
    }

    private void RequirePostgres()
        => Skip.If(!_fixture.IsAvailable, "PostgreSQL Testcontainer unavailable (Docker required).");

    [SkippableFact]
    public async Task Migrate_AppliesExclusionConstraintAndSequence()
    {
        RequirePostgres();

        await using var db = _fixture.CreateDbContext();
        var constraint = await db.Database.SqlQueryRaw<string>(
                """
                SELECT conname AS "Value"
                FROM pg_constraint
                WHERE conname = 'bookings_no_overlap'
                """)
            .SingleOrDefaultAsync();

        constraint.Should().Be("bookings_no_overlap");

        var seq = await db.Database.SqlQueryRaw<string>(
                """
                SELECT sequencename AS "Value"
                FROM pg_sequences
                WHERE sequencename = 'booking_number_seq'
                """)
            .SingleOrDefaultAsync();

        seq.Should().Be("booking_number_seq");
    }

    [SkippableFact]
    public async Task Insert_PersistsOwnedObjects_AndReadBack()
    {
        RequirePostgres();

        await using var provider = _fixture.CreateServices();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EHubDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<EfBookingRepository>();
        var numbers = scope.ServiceProvider.GetRequiredService<EfBookingNumberGenerator>();
        var uow = scope.ServiceProvider.GetRequiredService<EfUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<eHub.Application.Common.Time.IClock>();

        var assetId = Guid.NewGuid();
        var number = await numbers.NextAsync();
        var booking = PostgresBookingFixture.CreateSoftHold(
            assetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 5),
            number,
            clock.UtcNow);

        await repo.AddAsync(booking, clock.UtcNow);
        await uow.SaveChangesAsync();

        await using var readDb = _fixture.CreateDbContext();
        var loaded = await readDb.Bookings
            .Include(b => b.Timeline)
            .Include(b => b.StatusHistory)
            .SingleAsync(b => b.Id == booking.Id);

        loaded.BookingNumber.Should().Be(number);
        loaded.AssetSnapshot.Name.Should().Be("Test Asset");
        loaded.AssetSnapshot.Brand.Should().Be("Brand");
        loaded.Terms.BufferDays.Should().Be(1);
        loaded.UnitPrice.Amount.Should().Be(100m);
        loaded.TotalPrice.Amount.Should().Be(500m);
        loaded.Period.StartDate.Should().Be(new DateOnly(2026, 8, 1));
        loaded.Timeline.Should().NotBeEmpty();
        loaded.StatusHistory.Should().NotBeEmpty();
        loaded.AggregateVersion.Should().Be(1);
    }

    [SkippableFact]
    public async Task TransactionRollback_DoesNotPersistBookingOrIdempotencyComplete()
    {
        RequirePostgres();

        await using var db = _fixture.CreateDbContext();
        var clock = new FixedClock(new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc));
        var repo = new EfBookingRepository(db, clock);
        var idem = new EfBookingIdempotencyStore(db);
        var numbers = new EfBookingNumberGenerator(db, clock);

        var renterId = Guid.NewGuid();
        var key = $"rollback-{Guid.NewGuid():N}";
        var begin = await idem.BeginAsync(
            renterId,
            key,
            "hash-a",
            clock.UtcNow,
            BookingDefaults.IdempotencyProcessingTtl);
        begin.Should().BeOfType<IdempotencyBeginResult.Began>();

        await using var tx = await db.Database.BeginTransactionAsync();
        var number = await numbers.NextAsync();
        var booking = PostgresBookingFixture.CreateSoftHold(
            Guid.NewGuid(),
            renterId,
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 3),
            number,
            clock.UtcNow);

        await repo.AddAsync(booking, clock.UtcNow);
        await idem.CompleteAsync(renterId, key, booking.Id);
        await db.SaveChangesAsync();
        await tx.RollbackAsync();

        await using var verify = _fixture.CreateDbContext();
        (await verify.Bookings.AnyAsync(b => b.Id == booking.Id)).Should().BeFalse();

        var lease = await verify.BookingIdempotencyEntries
            .SingleAsync(e => e.RenterId == renterId && e.IdempotencyKey == key);
        lease.Status.Should().Be(BookingIdempotencyStatus.Started);
        lease.BookingId.Should().BeNull();
    }

    [SkippableFact]
    public async Task UniqueIdempotencyKey_SamePayload_Replays_DifferentPayload_Conflicts()
    {
        RequirePostgres();

        await using var db = _fixture.CreateDbContext();
        var clock = new FixedClock(new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc));
        var idem = new EfBookingIdempotencyStore(db);
        var renterId = Guid.NewGuid();
        var key = $"idem-{Guid.NewGuid():N}";

        var first = await idem.BeginAsync(renterId, key, "hash-1", clock.UtcNow, TimeSpan.FromMinutes(5));
        first.Should().BeOfType<IdempotencyBeginResult.Began>();

        var bookingId = Guid.NewGuid();
        await idem.CompleteAsync(renterId, key, bookingId);
        await db.SaveChangesAsync();

        var replay = await idem.BeginAsync(renterId, key, "hash-1", clock.UtcNow, TimeSpan.FromMinutes(5));
        replay.Should().BeOfType<IdempotencyBeginResult.CompletedReplay>()
            .Which.BookingId.Should().Be(bookingId);

        var mismatch = await idem.BeginAsync(renterId, key, "hash-2", clock.UtcNow, TimeSpan.FromMinutes(5));
        mismatch.Should().BeOfType<IdempotencyBeginResult.PayloadMismatch>();
    }

    [SkippableFact]
    public async Task ParallelOverlappingInserts_OneSucceeds_OneConflictsViaExclusion()
    {
        RequirePostgres();

        var assetId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var clock = new FixedClock(new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc));
        var start = new DateOnly(2026, 10, 1);
        var end = new DateOnly(2026, 10, 5);

        async Task<(bool Ok, Exception? Error)> TryInsertAsync(Guid renterId, string suffix)
        {
            try
            {
                await using var db = _fixture.CreateDbContext();
                var repo = new EfBookingRepository(db, clock);
                var numbers = new EfBookingNumberGenerator(db, clock);
                var uow = new EfUnitOfWork(db);

                // Bypass app-level check path under race: insert directly then SaveChanges
                // so exclusion constraint is the deciding line.
                var number = await numbers.NextAsync();
                var booking = PostgresBookingFixture.CreateSoftHold(
                    assetId, renterId, hostId, start, end, number, clock.UtcNow);

                await db.Bookings.AddAsync(booking);
                await uow.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }

        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();
        var results = await Task.WhenAll(TryInsertAsync(r1, "a"), TryInsertAsync(r2, "b"));

        results.Count(r => r.Ok).Should().Be(1);
        results.Count(r => !r.Ok).Should().Be(1);
        results.Single(r => !r.Ok).Error.Should().BeOfType<ConflictException>();

        await using var verify = _fixture.CreateDbContext();
        var count = await verify.Bookings.CountAsync(b => b.AssetId == assetId);
        count.Should().Be(1);
    }

    [SkippableFact]
    public async Task AggregateVersion_IsConcurrencyToken()
    {
        RequirePostgres();

        await using var db1 = _fixture.CreateDbContext();
        var clock = new FixedClock(new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc));
        var repo = new EfBookingRepository(db1, clock);
        var numbers = new EfBookingNumberGenerator(db1, clock);
        var number = await numbers.NextAsync();
        var booking = PostgresBookingFixture.CreateSoftHold(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 2),
            number,
            clock.UtcNow);

        await repo.AddAsync(booking, clock.UtcNow);
        await db1.SaveChangesAsync();

        await using var dbA = _fixture.CreateDbContext();
        await using var dbB = _fixture.CreateDbContext();
        var a = await dbA.Bookings.SingleAsync(b => b.Id == booking.Id);
        var b = await dbB.Bookings.SingleAsync(b => b.Id == booking.Id);

        a.Approve(a.HostId, clock.UtcNow.AddMinutes(1));
        await dbA.SaveChangesAsync();

        b.Approve(b.HostId, clock.UtcNow.AddMinutes(2));
        var act = () => dbB.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [SkippableFact]
    public async Task ExpiredHold_StillBlocksViaExclusion_UntilWorkerExpires()
    {
        RequirePostgres();

        var assetId = Guid.NewGuid();
        var createClock = new FixedClock(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        var expireClock = new FixedClock(new DateTime(2026, 7, 2, 1, 0, 0, DateTimeKind.Utc));

        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new EfBookingRepository(db, createClock);
            var numbers = new EfBookingNumberGenerator(db, createClock);
            var number = await numbers.NextAsync();
            var hold = PostgresBookingFixture.CreateSoftHold(
                assetId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 9, 10),
                new DateOnly(2026, 9, 12),
                number,
                createClock.UtcNow);

            await repo.AddAsync(hold, createClock.UtcNow);
            await db.SaveChangesAsync();

            // Simulate elapsed Soft Hold TTL without status change (lazy expire gap).
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE bookings SET "ExpiresAtUtc" = {createClock.UtcNow.AddHours(-1)} WHERE "Id" = {hold.Id}""");
        }

        await using (var db = _fixture.CreateDbContext())
        {
            var uow = new EfUnitOfWork(db);
            var numbers = new EfBookingNumberGenerator(db, expireClock);
            var number = await numbers.NextAsync();
            var competing = PostgresBookingFixture.CreateSoftHold(
                assetId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 9, 10),
                new DateOnly(2026, 9, 12),
                number,
                expireClock.UtcNow);

            // App-level list may not block (BlocksCalendar), but EXCLUDE still rejects.
            await db.Bookings.AddAsync(competing);
            var act = () => uow.SaveChangesAsync();
            await act.Should().ThrowAsync<ConflictException>();
        }

        await using (var db = _fixture.CreateDbContext())
        {
            var holds = await new EfBookingRepository(db, expireClock)
                .ListExpiredHoldsAsync(expireClock.UtcNow, 10);
            holds.Should().HaveCount(1);
            holds[0].Expire(expireClock.UtcNow);
            await new EfOutboxWriter(db).EnqueueAsync(holds[0].DomainEvents.First(), expireClock.UtcNow);
            holds[0].ClearDomainEvents();
            await db.SaveChangesAsync();

            var outboxCount = await db.OutboxMessages.CountAsync();
            outboxCount.Should().Be(1);
        }

        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new EfBookingRepository(db, expireClock);
            var numbers = new EfBookingNumberGenerator(db, expireClock);
            var number = await numbers.NextAsync();
            var afterExpire = PostgresBookingFixture.CreateSoftHold(
                assetId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 9, 10),
                new DateOnly(2026, 9, 12),
                number,
                expireClock.UtcNow);

            await repo.AddAsync(afterExpire, expireClock.UtcNow);
            await db.SaveChangesAsync();

            (await db.Bookings.CountAsync(b => b.AssetId == assetId)).Should().Be(2);
        }
    }

    [SkippableFact]
    public async Task ExpiredIdempotencyLease_Takeover_IsAtomic_OnlyOneBegan()
    {
        RequirePostgres();

        var renterId = Guid.NewGuid();
        const string key = "lease-race";
        const string hash = "same-hash";
        var past = new DateTime(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);

        await using (var seed = _fixture.CreateDbContext())
        {
            seed.BookingIdempotencyEntries.Add(new BookingIdempotencyEntry
            {
                RenterId = renterId,
                IdempotencyKey = key,
                RequestHash = hash,
                Status = BookingIdempotencyStatus.Started,
                BookingId = null,
                CreatedAtUtc = past,
                ExpiresAtUtc = past.AddMinutes(5)
            });
            await seed.SaveChangesAsync();
        }

        await using var dbA = _fixture.CreateDbContext();
        await using var dbB = _fixture.CreateDbContext();
        var storeA = new EfBookingIdempotencyStore(dbA);
        var storeB = new EfBookingIdempotencyStore(dbB);

        var tA = storeA.BeginAsync(renterId, key, hash, now, TimeSpan.FromMinutes(5));
        var tB = storeB.BeginAsync(renterId, key, hash, now, TimeSpan.FromMinutes(5));
        await Task.WhenAll(tA, tB);

        var results = new[] { tA.Result, tB.Result };
        results.Count(r => r is IdempotencyBeginResult.Began).Should().Be(1);
        results.Count(r => r is IdempotencyBeginResult.InProgress).Should().Be(1);
    }
}
