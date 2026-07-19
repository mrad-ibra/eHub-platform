# Sprint 5.2A — EF Persistence Foundation

**Status:** APPROVED WITH COMMENTS (Architect) → **5.2A.1 correctness pack in progress/done**  
**Depends on:** Sprint 5.1 in-memory APPROVED  

## Ordering decision

1. **5.2A** EF Persistence foundation ✅  
2. **5.2A.1** EXCLUDE visibility + Npgsql mapping + PG integration tests ✅  
3. **5.2B** Expire Worker — after 5.2A.1  

## Idempotency TTL

| TTL | Value | Purpose |
|-----|-------|---------|
| `IdempotencyProcessingTtl` | **5 minutes** | Begin → Complete/Abandon lease |
| `OwnerApprovalTtl` | 12 hours | Soft Hold |
| `PaymentTtl` | 15 minutes | Hard Hold after Approve |

## EF transaction rule (mandatory)

Within one DB transaction / one `SaveChangesAsync`:

1. Booking insert (tracked)  
2. Idempotency complete (tracked)  
3. Outbox messages (when added)  
4. `SaveChangesAsync` → commit  

`CompleteAsync` must **not** flush separately.

## DB correctness line (in Initial migration)

`InitialBookingPersistence` applies:

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;
CREATE SEQUENCE IF NOT EXISTS booking_number_seq START 1;

ALTER TABLE bookings
ADD CONSTRAINT bookings_no_overlap
EXCLUDE USING gist (
    "AssetId" WITH =,
    daterange(start_date, end_date + "BufferDays", '[]') WITH &&
)
WHERE ("Status" IN (
    'PENDING_OWNER_APPROVAL',
    'PENDING_PAYMENT',
    'CONFIRMED',
    'IN_PROGRESS'
));
```

Also: unique `BookingNumber`, PK `(RenterId, IdempotencyKey)`, required indexes.

Application `ListBlockingByAssetAsync` = **UX early rejection only**.  
`PostgresExceptionMapper` maps `23505` / `23P01` → `ConflictException` on `EfUnitOfWork.SaveChangesAsync`.

## Integration tests (Testcontainers)

`BookingPostgresPersistenceTests` (skip if Docker/Postgres unavailable):

- Migration applies exclusion + sequence  
- Insert + owned read-back  
- Transaction rollback  
- Idempotency unique / mismatch  
- Parallel overlapping inserts → one wins via EXCLUDE  
- AggregateVersion concurrency token  

## Production readiness

Still **NOT APPROVED** until expire worker + broader ops (5.2B+).
