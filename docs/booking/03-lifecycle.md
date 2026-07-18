# EHUB-503 — Booking Lifecycle

**Status:** APPROVED WITH MINOR CHANGES (Architect 2026-07-19).

## Happy path (host approval)

```text
Customer creates booking
        ↓
Availability check (blocks + Soft/Hard Holds + buffers + published)
        ↓
Booking = PendingOwnerApproval
  Soft Hold on dates (+ buffer)
  ExpiresAt = now + 12h
  BookingNumber assigned
  AssetSnapshot + BookingTerms captured
        ↓
Owner Approval
        ↓
Booking = PendingPayment
  Hard Hold continues
  ExpiresAt = now + 15m   ← payment timer starts HERE
        ↓
Payment succeeds
        ↓
Booking = Confirmed
        ↓
Rental starts → InProgress
        ↓
Rental ends → Completed
        ↓
Review (future — event only)
```

## Soft Hold release paths

```text
POA Soft Hold → Rejected | Expired (12h) | Cancelled  →  calendar released
PP Hard Hold  → Expired (15m) | Cancelled             →  calendar released
```

## Instant Book path

```text
Create → PendingPayment (15m from create) → Confirmed → InProgress → Completed
```

## Side paths

| Path | Flow |
|------|------|
| Host rejects | POA → Rejected (release Soft Hold) |
| Renter cancels early | POA/PP/Confirmed → Cancelled |
| TTL | POA 12h / PP 15m → Expired |
| Payment fails | PP → Expired |
| Extend | Confirmed/InProgress + new EndDate (re-check buffer) |
| Refund | Cancelled → Refunded after Payment |

## Responsibilities by step

| Step | Owner component |
|------|-----------------|
| Availability + buffer | Booking domain service + Asset read + booking repo |
| Soft/Hard Hold | Booking aggregate status |
| Approve/Reject | Booking aggregate |
| Charge | Payment aggregate |
| Notify | Outbox → Notification |
| Review | Future module on `BookingCompleted` |

## Sign-off

- [x] Happy path + Soft Hold agreed  
- [x] Payment timer after Approve agreed
