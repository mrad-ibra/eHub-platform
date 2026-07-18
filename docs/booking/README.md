# EPIC 5 — Booking Engine

**Sprint 5.0:** CLOSED — Architecture **APPROVED** (2026-07-19)  
**Sprint 5.1:** **CHANGES REQUIRED** → P0 remediation applied (see [12-sprint-51-review.md](12-sprint-51-review.md)). Re-review pending.  
Domain model remains strong; production readiness still gated on EF + expire worker + deeper integration tests.

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
