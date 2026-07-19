# EPIC 5 — Booking Engine

**Sprint 5.0:** CLOSED — Architecture **APPROVED**  
**Sprint 5.1:** In-memory + P0 — **APPROVED**  
**Sprint 5.2A:** EF Persistence Foundation — **APPROVED WITH COMMENTS**  
**Sprint 5.2A.1:** APPROVED WITH REQUIRED FOLLOW-UPS — see [16](16-sprint-52a1-rereview-disposition.md)  
**Sprint 5.2B:** Expire Worker + atomic idempotency + CI PG gate — **APPROVED WITH MINOR COMMENTS** ([15](15-sprint-52b-expire-worker.md), [17](17-sprint-52b-approval.md))  
**Production readiness:** improving (8.8/10) — platform work next (Notification / Payment / OTel)  
**Next focus (Architect):** Notification/Outbox processing → Payment → Observability → Redis → Search  

### Applied after re-review

- `BookingDefaults.IdempotencyProcessingTtl` = **5 minutes** (separate from Soft Hold / Payment TTL)  
- EF: `CompleteAsync` does not flush; booking + idempotency complete share `SaveChangesAsync`  
- `EHubDbContext`, Booking mappings, idempotency table, sequence, exclusion constraint migration  
- Connection string present → EF Booking adapters; empty → InMemory (Sprint 5.1 path)
- **5.2B:** `ExpirePendingBookings` hosted job, outbox stub, atomic idempotency reclaim, `EHUB_REQUIRE_POSTGRES_TESTS`

### Architect review focus (going forward)

Not method style — **domain growth decisions**, e.g.:

- Which aggregate owns a concern?  
- When does Review spawn?  
- Refund after which event?  
- Damage Report / Inspection boundaries?  
- How does Payment Failure affect availability?

Keep as-is for 5.1: Aggregate · CQRS · Handler · Validation · Snapshot · Idempotency · IClock.

When Booking grows (Refund / Insurance / Inspection / …): extract internal components (`BookingLifecycle`, `BookingPricing`, `BookingFulfillment`, `BookingAudit`) — Asset pattern.

| Task | Document | Status |
|------|----------|--------|
| EHUB-500…510 + companion | [pack docs](.) | **APPROVED** |

## Locked decisions

Soft Hold · Approval TTL 12h · Payment TTL 15m (after Approve) · Inclusive overlap · Buffer (default 1d) · Booking Number · Asset/Terms Snapshot · Version · Optimistic concurrency first

## Sprint 5.1 review bar (Architect)

Not “does it run?” — **does it model the business rules correctly?**

- Business rules live in Domain / Aggregate — **not** in handlers  
- Handler = orchestration only  
- Availability = Domain Service / domain component  
- Clear transaction boundaries; `CancellationToken` everywhere  
- Audit + timeline from day one  
- Domain events: locked names (see [06-domain-events.md](06-domain-events.md))  
- Time via `IClock` — never raw `DateTime.UtcNow` in domain/app logic  

## Backlog order

1. Domain (Aggregate + VOs)  
2. Persistence (EF, migration, indexes, concurrency)  
3. Application (`CreateBookingCommand` + validator + handler)  
4. API `POST /bookings`  
5. Tests (happy path, overlap, Soft Hold, TTL, snapshot, idempotency)

## Design-first checklist (later stories)

Business Rules → Domain Model → State Machine → Sequence → ER → API → AC → Edges → Failures → Tests → code

## Related

- [BRS](../brs.md) · [ADR 0006](../adr/0006-domain-primitives.md)
