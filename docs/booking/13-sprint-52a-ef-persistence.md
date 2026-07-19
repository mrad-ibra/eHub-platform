# Sprint 5.2A — EF Persistence Foundation

**Status:** IN PROGRESS  
**Authorized:** Architect green light after Sprint 5.1 P0 re-review  
**Depends on:** Sprint 5.1 in-memory APPROVED  

## Ordering decision

1. **5.2A EF Persistence** (this sprint)  
2. **5.2B Expire Worker** — after persistent, queryable storage  

## Idempotency TTL (applied)

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

`CompleteAsync` must **not** flush separately. On rollback, booking + idempotency + outbox all revert.  
`AbandonAsync` only clears **Started** leases after failed orchestration (before successful commit).

## Required constraints

- `UNIQUE (booking_number)`  
- `UNIQUE (renter_id, idempotency_key)`  
- Indexes: `(asset_id, status)`, `(expires_at_utc, status)`, `(renter_id, created_at_utc)`, `(host_id, created_at_utc)`  
- PostgreSQL `EXCLUDE USING gist` on occupied range for blocking statuses  

Application conflict checks remain for early UX errors; **DB exclusion is the correctness line**.
