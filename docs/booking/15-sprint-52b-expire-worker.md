# Sprint 5.2B — Expire Worker + correctness follow-ups

**Status:** APPROVED (Architect)  
See [18](18-sprint-52b-final-approval.md).  

## Scope (architect-required trio)

1. **ExpirePendingBookings** worker — Soft/Hard holds → `EXPIRED` so EXCLUDE releases the slot  
2. **Atomic idempotency lease takeover** — conditional `UPDATE … WHERE ExpiresAtUtc <= now AND Status = Started`  
3. **CI mandatory PostgreSQL tests** — `EHUB_REQUIRE_POSTGRES_TESTS=true` fails fixture init if Docker/Testcontainers cannot start  

## Why Expire Worker is a blocker

Application `BlocksCalendar(now)` filters expired Soft/Hard holds for UX.  
PostgreSQL `EXCLUDE USING gist` only looks at **Status**, not wall-clock.  

A row with `PENDING_OWNER_APPROVAL` + past `ExpiresAtUtc` still blocks inserts until status becomes `EXPIRED`.

## Worker design

| Piece | Implementation |
|-------|----------------|
| Hosted service | `ExpirePendingBookingsHostedService` |
| Processor | `ExpirePendingBookingsProcessor` |
| Query | `IBookingRepository.ListExpiredHoldsAsync` (`ExpiresAtUtc <= now`, POA/PP) |
| Domain | `Booking.Expire(now)` |
| Outbox | `IOutboxWriter` / `outbox_messages` (same UoW as expire) |
| Notify | `LoggingBookingExpiryNotifier` stub |
| Metrics | `LoggingExpireBookingsMetrics` |
| Retry | Hosted loop backoff via `Jobs:ExpirePendingBookings:RetryDelaySeconds` |
| Multi-instance | Optimistic concurrency on `AggregateVersion` + domain guards |

Config (`appsettings.json`):

```json
"Jobs": {
  "ExpirePendingBookings": {
    "Enabled": true,
    "IntervalSeconds": 60,
    "BatchSize": 100,
    "RetryDelaySeconds": 15
  }
}
```

## Idempotency lease (documented)

- **Begin** claims the lease **outside** the booking transaction (`SaveChanges` immediately).  
- On API crash after Begin: row stays `Started` until **processing TTL** (`BookingDefaults.IdempotencyProcessingTtl` = **5 minutes**).  
- **Complete** is tracked only; flushed with booking insert via `IUnitOfWork`.  
- Expired lease reclaim: **atomic conditional UPDATE** (one winner → `Began`, loser → `InProgress`).

## CI gate

```bash
EHUB_REQUIRE_POSTGRES_TESTS=true
```

Local without Docker: tests **Skip**.  
CI (`.github/workflows/ci.yml`): env set → fixture **throws** if container/migrate fails.

## Integration coverage added

- Expired pending still blocked by EXCLUDE → expire → insert succeeds + outbox row  
- Parallel expired idempotency takeover → exactly one `Began`

## Ops notes (medium, not blockers for this sprint)

- API is **not** in docker-compose; Postgres/Redis/pgAdmin are. Run API with `dotnet run`.  
- Migrations: apply via `dotnet ef database update` (or a deploy job) — **not** auto on API startup.  
