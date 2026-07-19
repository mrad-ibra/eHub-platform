# EPIC 5 — Booking Engine

**Sprint 5.0–5.2:** **APPROVED** — Booking Core **COMPLETED**  
See [14-sprint-52-final-disposition.md](14-sprint-52-final-disposition.md)

**Next epic:** [Payment Architecture Pack](../payment/README.md) (Sprint 6.0 DRAFT)

### Locked decisions (Booking)

Soft Hold · Approval TTL 12h · Payment TTL 15m (after Approve) · Inclusive overlap · Buffer (default 1d) · Booking Number · Asset/Terms Snapshot · Version · Optimistic concurrency · PostgreSQL EXCLUDE · Expire worker · Idempotency processing TTL 5m

### Applied

- EF Persistence, exclusion constraint, sequence, concurrency token  
- Atomic idempotency lease takeover  
- ExpirePendingBookings hosted job + outbox table  
- CI mandatory PostgreSQL tests (`EHUB_REQUIRE_POSTGRES_TESTS`)  
- Observability: OTel, correlation id, booking metrics, health  

| Task | Document | Status |
|------|----------|--------|
| EHUB-500…510 + companion | [pack docs](.) | **APPROVED** |
| Sprint 5.2 closeout | [14](14-sprint-52-final-disposition.md) | **APPROVED** |

## Design-first checklist (later stories)

Business Rules → Domain Model → State Machine → Sequence → ER → API → AC → Edges → Failures → Tests → code

## Related

- [BRS](../brs.md) · [ADR 0006](../adr/0006-domain-primitives.md) · [Payment](../payment/README.md)
