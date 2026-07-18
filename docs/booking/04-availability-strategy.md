# EHUB-504 — Availability Strategy

**Status:** Draft for sign-off.  
**Hardest part of the engine.** Goal: no double booking.

## Inputs

1. **Asset availability blocks** (`AssetAvailability`) — owner blackouts  
2. **Blocking bookings** on same AssetId  
3. **Requested period** `BookingPeriod`

## Overlap definition

Periods A and B overlap iff:

```text
A.StartDate <= B.EndDate AND A.EndDate >= B.StartDate
```

(inclusive calendar days — BR-BKG-002)

### Examples

| Existing | Request | Result |
|----------|---------|--------|
| 1–5 Jul | 1–5 Jul | **Reject** (full) |
| 1–5 Jul | 3–6 Jul | **Reject** (partial) |
| 1–5 Jul | 6–8 Jul | **Accept** (no shared day) |
| 1–5 Jul | 5–5 Jul | **Reject** (touch on end day) |
| 1–5 Jul PendingPayment | 3–4 Jul | **Reject** (pending holds) |
| 1–5 Jul Cancelled | 3–4 Jul | **Accept** |
| 1–5 Jul + owner block 4–4 | 1–3 Jul | **Accept** |
| 1–5 Jul + owner block 4–4 | 3–5 Jul | **Reject** (hits block) |

## Partial vs full overlap

**No distinction for v1.** Any overlap with a blocking source → reject.  
No “partial accept” / split bookings.

## Pending overlap

Pending (`PendingOwnerApproval`, `PendingPayment`) **counts as blocking**.  
Customer B cannot steal days while Customer A waits for host/payment.

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

  for booking in Bookings.FindBlocking(assetId, period):
    deny(BookingConflict)

  allow
```

`FindBlocking` filters statuses in blocking set and applies overlap predicate.  
Must run inside the same concurrency boundary as insert (EHUB-505).

## Race window

Two parallel `CanBook` both allow → both insert.  
Mitigations: optimistic version on Asset **or** DB exclusion constraint / transaction serialization — see EHUB-505.

## Sign-off

- [ ] Inclusive overlap examples accepted  
- [ ] Pending holds calendar accepted  
- [ ] No partial booking accepted
