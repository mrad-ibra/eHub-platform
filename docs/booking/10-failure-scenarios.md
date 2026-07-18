# EHUB-510 — Failure Scenarios

**Status:** APPROVED WITH MINOR CHANGES (Architect 2026-07-19).  
Each row must map to a test in Sprint 5.1.

## Payment & approval

| # | Scenario | Expected |
|---|----------|----------|
| F01 | Host approved → Payment fails / 15m timeout | `PendingPayment` → `Expired`; Soft/Hard Hold released; `BookingExpired`; no charge |
| F02 | Payment webhook duplicated | Inbox dedupe; Confirmed once |
| F03 | Payment OK but Confirm crashes | Outbox/inbox retry; idempotent Confirm |
| F04 | Customer pays after Expired | Reject or auto-refund; Booking stays Expired |
| F05 | Payment attempt during Soft Hold (before Approve) | Denied — payment timer/session not started |

## Soft Hold & TTL

| # | Scenario | Expected |
|---|----------|----------|
| F06 | POA exceeds 12h | `Expired`; calendar + buffer released |
| F07 | Second customer tries same dates during Soft Hold | 409 conflict |
| F08 | Soft Hold expires → third party books same dates | Allowed |

## Asset lifecycle vs booking

| # | Scenario | Expected |
|---|----------|----------|
| F10 | Asset archived during Soft Hold | Cannot approve; expire/reject; no new bookings |
| F11 | Asset soft-deleted while Confirmed | Existing booking remains; no new bookings |
| F12 | Asset Suspended mid-hold | Pending expire/reject; Confirmed honored unless force-majeure |

## Availability, buffer, concurrency

| # | Scenario | Expected |
|---|----------|----------|
| F20 | Two creates same dates concurrent | One 201, one 409 |
| F21 | Request starts on buffer day (booking 1–5, buffer 1, request 6) | 409 |
| F22 | Idempotent replay of create | Same BookingId/Number; no second Soft Hold |
| F23 | Extend into neighbor buffer | 409; period unchanged |

## Cancel & notification

| # | Scenario | Expected |
|---|----------|----------|
| F30 | Cancel ≥48h before start | Cancelled; refund event |
| F40 | Email send fails | Booking committed; outbox retries |
| F41 | Notification never sent | Booking valid; ops replay |

## Snapshot integrity

| # | Scenario | Expected |
|---|----------|----------|
| F60 | Asset renamed 2 years later | GET booking still shows snapshot name/brand/model |
| F61 | Asset price raised after create | Booking total unchanged |

## Design principles

1. Calendar truth = Soft Hold + Hard Hold + Confirmed + InProgress + buffers.  
2. Money only in Payment aggregate.  
3. No external IO inside DB transaction.  
4. Prefer `Expired` over silent delete.  
5. Payment TTL never starts before Approve (unless Instant Book).

## Sign-off

- [x] Soft Hold + 12h/15m failure paths approved  
- [x] Buffer + snapshot failures approved
