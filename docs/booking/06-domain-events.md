# EHUB-506 — Booking Domain Events

**Status:** Draft for sign-off.  
**Transport:** Outbox (same TX as Booking write). Consumers must be idempotent.

## Event catalog

| Event | When | Typical consumers |
|-------|------|-------------------|
| `BookingCreated` | Request created (POA or PP) | Notify host/renter, analytics |
| `BookingApproved` | Host approve → PP | Notify renter, start payment |
| `BookingRejected` | Host reject | Notify renter |
| `BookingCancelled` | Actor cancel | Payment refund saga, notify |
| `BookingExpired` | TTL job | Notify, free calendar (implicit) |
| `BookingPaymentPending` | Enter PP (optional if Created covers Instant Book) | Payment module |
| `BookingConfirmed` | Payment OK | Notify both, calendar solid |
| `BookingStarted` | → InProgress | Notify, ops |
| `BookingCompleted` | → Completed | Review module, notify |
| `BookingExtended` | Period extended | Notify, pricing snapshot update |
| `BookingRefunded` | Refund settled | Notify |

Minimum required for v1:  
`BookingCreated`, `BookingApproved`, `BookingRejected`, `BookingCancelled`, `BookingExpired`, `BookingConfirmed`, `BookingStarted`, `BookingCompleted`, `BookingExtended`, `BookingRefunded`.

## Payload guidelines

- Include: `BookingId`, `AssetId`, `RenterId`, `HostId`, `Period`, `Status`, `OccurredAtUtc`  
- Money as snapshot  
- No PII beyond what Notification needs (email resolved by Identity consumer)

## Non-goals

- Events must not embed SMTP send  
- Payment provider calls happen in Payment handlers after reading outbox/inbox

## Sign-off

- [ ] Event list locked for v1  
- [ ] Outbox mandatory approved
