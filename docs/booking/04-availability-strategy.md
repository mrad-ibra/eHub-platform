# EHUB-504 — Availability Strategy

**Status:** APPROVED WITH MINOR CHANGES (Architect 2026-07-19).  
**Hardest part of the engine.** Goal: no double booking + Soft Hold UX + preparation buffer.

## Inputs

1. **Asset availability blocks** — owner blackouts  
2. **Blocking bookings** (Soft Hold / Hard Hold / Confirmed / InProgress)  
3. **Requested period** `BookingPeriod`  
4. **BufferDays** per booking (snapshot) / Asset default

## Overlap definition (inclusive)

```text
A.StartDate <= B.EndDate AND A.EndDate >= B.StartDate
```

### Occupied range (with buffer)

```text
OccupiedStart = Booking.StartDate
OccupiedEnd   = Booking.EndDate + Booking.BufferDays
```

Conflict if request overlaps `[OccupiedStart, OccupiedEnd]` inclusive.

### Examples

| Existing | Buffer | Request | Result |
|----------|--------|---------|--------|
| 1–5 Jul | 0 | 1–5 Jul | **Reject** (full) |
| 1–5 Jul | 0 | 3–6 Jul | **Reject** (partial) |
| 1–5 Jul | 0 | 5–10 Jul | **Reject** (touch day 5) |
| 1–5 Jul | 0 | 6–8 Jul | **Accept** |
| 1–5 Jul | **1** | 6–8 Jul | **Reject** (day 6 = buffer) |
| 1–5 Jul | **1** | 7–9 Jul | **Accept** |
| 1–5 Soft Hold (POA) | 1 | 3–4 Jul | **Reject** |
| 1–5 Cancelled | 1 | 3–4 Jul | **Accept** |

## Soft Hold (pending)

`PendingOwnerApproval` and `PendingPayment` **block** like committed bookings for conflict checks.  
Prevents Owner receiving three overlapping requests for the same slot.

On Expired/Rejected/Cancelled → occupied range released.

## Partial vs full overlap

No distinction for v1. Any overlap with occupied range → reject.

## Check algorithm (logical)

```text
function CanBook(assetId, period):
  asset = load Asset
  if asset.Status != Published: deny
  if asset.IsDeleted: deny
  if period invalid: deny
  if period violates Min/Max rental days: deny

  for block in asset.Availability.Blocks:
    if block.Overlaps(period): deny(AssetBlocked)

  bufferDefault = asset.PreparationBufferDays ?? platformDefault(1)

  for booking in Bookings.FindBlocking(assetId):
    occupied = [booking.Start, booking.End + booking.BufferDays]
    if period.Overlaps(occupied): deny(BookingConflict)

  // Also: new booking's own buffer must not collide with neighbors
  // (symmetric check when comparing against existing Start)

  allow
```

Must run inside concurrency boundary (EHUB-505).

## Sign-off

- [x] Inclusive overlap accepted  
- [x] Soft Hold accepted  
- [x] Buffer (default 1 day) accepted
