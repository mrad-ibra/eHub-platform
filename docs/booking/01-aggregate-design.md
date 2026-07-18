# EHUB-501 — Booking Aggregate Design

**Status:** Draft for architect sign-off.  
**Boundary:** Booking is its **own** aggregate. Not inside Asset. Payment is a **separate** aggregate referenced by id.

## Aggregate diagram

```text
Booking (Aggregate Root)
├── BookingId                 (strongly typed Id)
├── AssetId                   (Id-only reference)
├── RenterId                  (Id-only reference)
├── HostId                    (denormalized from Asset.OwnerId at create)
├── BookingPeriod             (VO: StartDate, EndDate — DateOnly inclusive)
├── Money Snapshot
│   ├── UnitPrice             (Money: Amount + CurrencyId)
│   ├── TotalPrice            (Money)
│   └── optional fees         (Money: DriverFee, DeliveryFee)
├── PickupInformation         (VO)
├── DropoffInformation        (VO)
├── DriverOption              (VO / entity child)
├── DeliveryOption            (VO / entity child)
├── BookingStatus             (smart enum — same pattern as AssetStatusCode)
├── BookingTimeline           (ordered entries inside aggregate)
├── StatusHistory             (append-only inside aggregate)
├── RejectionReason?          (string)
├── CancellationReason?       (string)
├── ExpiresAtUtc?             (for pending TTLs)
├── ConfirmedAtUtc?
├── StartedAtUtc?
├── CompletedAtUtc?
├── RowVersion / Version      (concurrency)
└── Audit                     (Created/Updated)
```

## Value objects

### BookingPeriod

```text
StartDate: DateOnly
EndDate: DateOnly
Invariant: EndDate >= StartDate
Days: EndDate - StartDate + 1 (inclusive)
Overlaps(other): StartDate <= other.EndDate && EndDate >= other.StartDate
```

### Money

```text
Amount: decimal (>= 0)
CurrencyId: Guid  // Catalog Currency — Id-only
```

Same-currency arithmetic only.

### PickupInformation / DropoffInformation

```text
Location snapshot (optional CountryId/CityId/DistrictId/AddressLine/Lat/Lng)
or "UseAssetLocation" flag
ScheduledTimeUtc? (optional for v1)
Notes?
```

### DriverOption

```text
Requested: bool
Fee: Money?   // snapshot at booking time
```

### DeliveryOption

```text
Requested: bool
Fee: Money?
Address snapshot?
```

## Child collections (inside aggregate, not separate roots)

| Child | Purpose |
|-------|---------|
| `BookingTimelineEntry` | Human-readable lifecycle log (who/when/what) |
| `BookingStatusHistoryEntry` | Status transition audit |

No `BookingPayment` entity inside Booking — Payment aggregate owns payment rows; Booking stores optional `PaymentId?` when known.

## Factory & behaviors (names only — no code this sprint)

- `Booking.CreateRequest(...)` → PendingOwnerApproval or PendingPayment  
- `Approve(hostId)`  
- `Reject(hostId, reason)`  
- `MarkPaymentPending(expiresAt)`  
- `Confirm(paymentId)`  
- `Cancel(actor, reason)`  
- `Expire()`  
- `Start()` / `Complete()`  
- `Extend(newEndDate, additionalMoney)`  

All transitions validated by state machine (EHUB-502).

## What must NOT be in Booking

- Payment provider charge logic  
- Email sending  
- GPS tracking  
- Chat messages  
- Asset media mutations  

## Sign-off

- [ ] Aggregate boundary approved  
- [ ] Money VO approved  
- [ ] Period inclusive days approved
