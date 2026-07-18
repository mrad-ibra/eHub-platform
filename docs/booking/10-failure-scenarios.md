# EHUB-510 — Failure Scenarios

**Status:** Draft for sign-off. Each row must map to a test in Sprint 5.1.

## Payment & approval

| # | Scenario | Expected |
|---|----------|----------|
| F01 | Host approved → Payment fails / timeout | Booking `PendingPayment` → `Expired`; no Confirmed; calendar freed; `BookingExpired`; no charge |
| F02 | Payment webhook duplicated | Inbox dedupe; Booking stays Confirmed once; no double events side effects |
| F03 | Payment succeeds but Confirm handler crashes | Outbox/inbox retry; eventually Confirmed; idempotent Confirm |
| F04 | Customer pays after Expired | Payment module rejects or auto-refund; Booking stays Expired |

## Asset lifecycle vs booking

| # | Scenario | Expected |
|---|----------|----------|
| F10 | Asset archived while `PendingOwnerApproval` | Host cannot approve; system expire or auto-reject; no new bookings |
| F11 | Asset soft-deleted while `Confirmed` | Existing booking remains; ops flag; no new bookings; start/complete still possible by policy |
| F12 | Asset unpublished (Suspended) mid-hold | Pending expire/reject; Confirmed: honor booking (customer protection) unless force-majeure policy |

## Availability & concurrency

| # | Scenario | Expected |
|---|----------|----------|
| F20 | Two creates same dates concurrent | One 201, one 409 conflict |
| F21 | Create vs owner blackout same second | 409 / validation conflict |
| F22 | Idempotent replay of create | Same BookingId, no second hold |

## Expiry & cancel

| # | Scenario | Expected |
|---|----------|----------|
| F30 | POA expires | `Expired`; availability open; notify both |
| F31 | PP expires | `Expired`; availability open |
| F32 | Cancel after Confirmed (≥48h) | `Cancelled`; refund event to Payment |
| F33 | Cancel after start | Denied or exceptional path only |

## Notification failures

| # | Scenario | Expected |
|---|----------|----------|
| F40 | Email send fails | Outbox message remains / retries; Booking state already committed |
| F41 | Notification never sent | Booking still valid; ops replay outbox |

## Extension

| # | Scenario | Expected |
|---|----------|----------|
| F50 | Extend into other booking | 409; period unchanged |
| F51 | Extend with price change on Asset | Additional days priced from **frozen** unit price |

## Design principles under failure

1. **Calendar truth** = blocking statuses only.  
2. **Money movement** only in Payment aggregate.  
3. **Never** call external IO inside DB transaction.  
4. Prefer **Expired** over silent delete.

## Sign-off

- [ ] F01–F04 payment path approved  
- [ ] F20 concurrency approved  
- [ ] F40 notification decoupling approved
