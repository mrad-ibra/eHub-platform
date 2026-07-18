# EHUB-505 — Concurrency Strategy

**Status:** Draft for sign-off.

## Chosen approach (v1)

**Optimistic concurrency first.** Add distributed lock only if metrics demand it.

### Stack

| Layer | Mechanism |
|-------|-----------|
| Application | `Idempotency-Key` on create |
| Domain/Persistence | `Asset.VersionNumber` (or RowVersion) checked when placing hold |
| Database | Filtered unique / exclusion strategy for active booking ranges (PostgreSQL) |
| Optional later | Redis lock `lock:asset:{assetId}:booking` TTL 5–10s around create |

## Create booking concurrency flow

```text
1. Begin transaction
2. Load Asset WITH concurrency token
3. Run availability checks
4. Insert Booking (blocking status)
5. Write Outbox (BookingCreated)
6. Commit
7. If concurrency conflict → retry once or return 409 Conflict
```

## PostgreSQL range idea (implementation sprint)

Prefer:

- `tstzrange` / `daterange` column on Booking  
- Exclusion constraint with `gist` on `(AssetId WITH =, Period WITH &&)` where status in blocking set  

Or application-level: `UPDLOCK`-style serializable transaction on asset row.

**Decision for design pack:** document requirement; exact SQL in Sprint 5.1 persistence task.

## Idempotency store

```text
IdempotencyKey + UserId → BookingId (or hash of request body)
TTL >= PendingPayment TTL
```

## Distributed lock (phase 2 criteria)

Add when:

- Conflict/retry rate > threshold under load test  
- Hot assets (popular cars) show double-booking incidents  

## Sign-off

- [ ] Optimistic-first approved  
- [ ] 409 on conflict approved  
- [ ] Exclusion constraint planned for EF sprint
