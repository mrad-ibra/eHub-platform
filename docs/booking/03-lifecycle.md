# EHUB-503 — Booking Lifecycle

**Status:** Draft for sign-off.

## Happy path (host approval)

```text
Customer creates booking
        ↓
Availability check (blocks + blocking bookings + asset published)
        ↓
Booking = PendingOwnerApproval (hold calendar, ExpiresAt = now+24h)
        ↓
Owner Approval
        ↓
Booking = PendingPayment (ExpiresAt = now+30m)
        ↓
Payment succeeds (Payment aggregate + inbox/outbox)
        ↓
Booking = Confirmed
        ↓
Rental starts (StartDate reached / check-in)
        ↓
Booking = InProgress
        ↓
Rental ends
        ↓
Booking = Completed
        ↓
Review (future module — event only)
```

## Instant Book path

```text
Create → PendingPayment → Confirmed → InProgress → Completed
```

## Side paths

| Path | Flow |
|------|------|
| Host rejects | POA → Rejected |
| Renter cancels early | POA/PP/Confirmed → Cancelled |
| TTL | POA/PP → Expired |
| Payment fails | PP → Expired (or Cancelled — prefer Expired) |
| Extend | Confirmed/InProgress + new EndDate |
| Refund | Cancelled → Refunded after Payment completes refund |

## Responsibilities by step

| Step | Owner component |
|------|-----------------|
| Availability | Booking domain service + Asset read (Id) + booking repo query |
| Approve/Reject | Booking aggregate |
| Charge | Payment aggregate |
| Notify | Outbox → Notification handlers |
| Review | Future Reviews module on `BookingCompleted` |

## Sign-off

- [ ] Happy path agreed  
- [ ] Instant Book path agreed
