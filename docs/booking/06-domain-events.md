# EHUB-506 — Booking Domain Events

**Status:** APPROVED — naming **LOCKED** (Architect 2026-07-19).  
**Transport:** Outbox (same TX as Booking write). Consumers must be idempotent.

## Naming convention (locked)

PascalCase past tense: `{Aggregate}{PastTenseVerb}`

Do **not** rename after Sprint 5.1. No prefixes like `I`, no `On`, no `Event` suffix on type names used in outbox payloads (CLR types may end with `DomainEvent` if needed for clarity in code).

### Locked catalog (v1)

| Event name | When |
|------------|------|
| `BookingCreated` | Request created (Soft Hold POA or Instant Book PP) |
| `BookingApproved` | Host approve → PendingPayment |
| `BookingRejected` | Host reject |
| `BookingCancelled` | Actor cancel |
| `BookingExpired` | TTL job |
| `BookingConfirmed` | Payment succeeded |
| `BookingStarted` | → InProgress |
| `BookingCompleted` | → Completed |
| `BookingExtended` | Period extended |
| `BookingRefunded` | Refund settled |

Optional later (not required day one): `BookingPaymentPending` — Instant Book may rely on `BookingCreated` with status PP.

## Payload guidelines

- `BookingId`, `BookingNumber`, `AssetId`, `RenterId`, `HostId`, `Period`, `Status`, `OccurredAtUtc`  
- Money as snapshot  
- No SMTP / payment provider calls inside event handlers’ domain layer

## Non-goals

- Events must not embed email send  
- Payment provider calls happen in Payment handlers after outbox/inbox

## Sign-off

- [x] Event list locked for v1  
- [x] Outbox mandatory approved
