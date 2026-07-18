# EHUB-501 — Booking Aggregate Design

**Status:** APPROVED WITH MINOR CHANGES (Architect 2026-07-19).  
**Boundary:** Booking is its **own** aggregate. Not inside Asset. Payment is a **separate** aggregate referenced by id.

## Aggregate diagram

```text
Booking (Aggregate Root)
├── BookingId                 (strongly typed Guid Id)
├── BookingNumber             (BK-2026-000000123 — business id)
├── Version                   (int — concurrency + evolution)
├── AssetId                   (Id-only reference)
├── RenterId                  (Id-only reference)
├── HostId                    (denormalized from Asset.OwnerId at create)
├── BookingPeriod             (VO: StartDate, EndDate — DateOnly inclusive)
├── BufferDays                (int — snapshot of prep buffer at create)
├── Money Snapshot
│   ├── UnitPrice             (Money: Amount + CurrencyId)
│   ├── TotalPrice            (Money)
│   └── optional fees         (Money: DriverFee, DeliveryFee)
├── AssetSnapshot             (VO — name, brand, model, images, owner display)
├── BookingTerms              (VO — rental rules snapshot at create)
├── PickupInformation         (VO)
├── DropoffInformation        (VO)
├── DriverOption              (VO / entity child)
├── DeliveryOption            (VO / entity child)
├── BookingStatus             (smart enum)
├── BookingTimeline           (product-facing lifecycle log)
├── StatusHistory             (append-only audit: Created/Approved/Rejected/Paid/…)
├── RejectionReason?
├── CancellationReason?
├── ExpiresAtUtc?             (POA: +12h from create; PP: +15m from approve)
├── ConfirmedAtUtc?
├── StartedAtUtc?
├── CompletedAtUtc?
└── PaymentId?                (Id-only when known)
```

## Soft Hold vs Hard Hold

| Status | Hold type | Calendar |
|--------|-----------|----------|
| `PendingOwnerApproval` | Soft Hold | Blocks (+ buffer) |
| `PendingPayment` | Hard Hold | Blocks (+ buffer) |
| `Confirmed` / `InProgress` | Committed | Blocks (+ buffer) |

## Value objects

### BookingPeriod

```text
StartDate: DateOnly
EndDate: DateOnly
Invariant: EndDate >= StartDate
Days: EndDate - StartDate + 1 (inclusive)
Overlaps(other): StartDate <= other.EndDate && EndDate >= other.StartDate
OccupiedEnd(bufferDays): EndDate.AddDays(bufferDays)
```

### AssetSnapshot

```text
Name, Brand, Model
PrimaryImageUrls[]
UnitPrice / CurrencyId (mirror of money freeze)
HostDisplayName / HostId
CapturedAtUtc
```

### BookingTerms

```text
MinRentalDays, MaxRentalDays
Deposit / policy notes (as applicable at create)
Cancel window summary
Raw rule version or serialized snapshot JSON
```

### Money / Pickup / Dropoff / Driver / Delivery

Unchanged from prior draft (Money Amount + CurrencyId; location snapshots; fee snapshots).

## Child collections

| Child | Purpose |
|-------|---------|
| `BookingTimelineEntry` | Product timeline (Created, Approved, Rejected, Paid, Started, Completed, Cancelled, …) |
| `BookingStatusHistoryEntry` | Strict status transition audit |

No `BookingPayment` inside Booking — Payment aggregate owns payment rows.

## Factory & behaviors (names only)

- `Booking.CreateRequest(...)` → Soft Hold POA (or PP if Instant Book); assign `BookingNumber`; capture snapshots  
- `Approve(hostId)` → Hard Hold PP; **start 15m payment timer**  
- `Reject` / `Cancel` / `Expire` → release hold  
- `Confirm(paymentId)` / `Start` / `Complete` / `Extend`  

## What must NOT be in Booking

- Payment provider charge logic  
- Email sending  
- GPS / Chat / Asset media mutations  

## Sign-off

- [x] Aggregate boundary approved  
- [x] Soft Hold + Snapshot + Number + Terms + Version approved  
- [x] Period inclusive days approved
