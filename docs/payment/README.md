# EPIC 6 — Payment

**Sprint 6.0:** Payment Architecture Pack — **APPROVED** (2026-07-20)  
**Sprint 6.1 Phase 1:** Domain — **APPROVED**  
**Sprint 6.1 Phase 2:** Application + Persistence — **IN PROGRESS** ([13](13-sprint-61-phase2.md))

See [12-sprint-60-approved-61-phase1.md](12-sprint-60-approved-61-phase1.md).

## Documents

| # | Document | Purpose |
|---|----------|---------|
| — | [README.md](README.md) | Index + locked principles |
| 00–11 | Architecture pack | Rules, aggregate, SM, webhook, refund, API, AC |
| 12 | [12-sprint-60-approved-61-phase1.md](12-sprint-60-approved-61-phase1.md) | 6.0 approval + Phase 1 |
| 13 | [13-sprint-61-phase2.md](13-sprint-61-phase2.md) | Phase 2 Application/Persistence |

## Locked principles (non-negotiable for 6.1)

| # | Principle |
|---|-----------|
| L1 | Amount from **Booking `TotalPrice` snapshot** — client price never trusted |
| L2 | Same webhook processed **once** (inbox unique key) |
| L3 | Booking `Confirmed` **only after** Payment `Succeeded` |
| L4 | Late webhook **must not** confirm an `Expired`/terminal Booking |
| L5 | Refund = **separate audited operation** (partial and full have distinct rules) |
| L6 | Webhook **signature validation mandatory** before any effect |
| L7 | Payment and Booking = **separate aggregates** (id-only) |
| L8 | Provider-specific models **never** enter Domain (adapter / ACL) |
| L9 | Payment → Booking effects only via **Outbox / events** |
| L10 | Failed payment must **not** leave Booking stuck forever — TTL + optional retry while hold active |

## Architect recommendations (apply during 6.1)

- Small provider interface (`Create` / `Cancel` / `Refund` / `VerifyWebhook`)
- `Money` value object everywhere
- `PaymentTimeline` (Booking-style audit trail)
- Outbox between Payment and Booking confirm

## Relationship to Booking (EPIC 5)

| Booking | Payment |
|---------|---------|
| Owns calendar, Soft/Hard Hold, price freeze | Owns money movement |
| `PendingPayment` + 15m TTL | Must succeed inside that window |
| Confirms only on `PaymentSucceeded` (hold active) | Emits events; never calls Booking directly |
| Expire worker releases hold | Failed/Expired payments; retry only while Booking still payable |

## Related

- [../booking/README.md](../booking/README.md) — Booking Core COMPLETED  
- [../observability.md](../observability.md)  
- [ADR 0006](../adr/0006-domain-primitives.md)
