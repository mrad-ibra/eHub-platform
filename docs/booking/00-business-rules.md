# EHUB-500 — Booking Business Rules

**Status:** APPROVED WITH MINOR CHANGES (Architect sign-off 2026-07-19).  
Change only via explicit decision note.

## Core principle

> Two renters must never hold overlapping **blocking** bookings on the same Asset.

---

## BR-BKG-001 — Overlap

Booking periods on the same `AssetId` **must not overlap** if both bookings are in a **blocking** status — after applying **Booking Buffer** (BR-BKG-015).

**Blocking statuses:**  
`PendingOwnerApproval` (Soft Hold), `PendingPayment` (Hard Hold), `Confirmed`, `InProgress`

**Non-blocking (do not reserve calendar):**  
`Draft`, `Rejected`, `Cancelled`, `Expired`, `Completed`, `Refunded`

---

## BR-BKG-002 — Period model (inclusive overlap)

- Granularity (v1): **calendar days** (`DateOnly` start/end inclusive).  
- Example: 1–5 Jul and 5–10 Jul → **overlap on 5 Jul** → reject.  
- Rationale: return, inspection, cleaning, preparation share the boundary day.  
- Hourly rentals: out of scope for v1.

---

## BR-BKG-003 — Soft Hold (PendingOwnerApproval)

**Decision: Soft Hold — yes, pending blocks the calendar.**

```text
PendingOwnerApproval
        ↓
    Soft Hold  (dates temporarily reserved)
        ↓
    TTL 12h
        ↓
    Expired → Release hold
```

Rules:

1. While in `PendingOwnerApproval`, the period (+ buffer) is **reserved**.  
2. Other users **cannot** create a booking that conflicts with that hold.  
3. Owner receives **one** competing request per slot — not a pile of overlapping Pendings.  
4. On `Expired` / `Rejected` / `Cancelled` → hold **automatically released**.  
5. Soft Hold ≠ payment; no charge yet. Soft = temporary reservation only.

`PendingPayment` is also blocking (**Hard Hold** until paid or expired) — same calendar effect, stronger commercial intent.

---

## BR-BKG-004 — Pending TTL (auto-expire)

| Status | TTL (v1 locked) | When timer starts | On expire |
|--------|-----------------|-------------------|-----------|
| `PendingOwnerApproval` | **12 hours** | At **create** | → `Expired`; Soft Hold released; `BookingExpired` |
| `PendingPayment` | **15 minutes** | At **Approve** (or Instant Book create) — **never before approval** | → `Expired`; hold released; `BookingExpired` |

```text
PendingOwnerApproval (12h Soft Hold)
        ↓ Approve
PendingPayment (15m — timer starts HERE)
        ↓ Pay
Confirmed
```

Background job: `ExpirePendingBookingsJob`.

### Future (Company / Asset policy)

Companies may configure:

- Auto Approve vs Manual Approve  
- Approval TTL  
- Payment TTL  

v1 uses platform defaults above.

---

## BR-BKG-005 — Owner reject

Owner (Host) **may reject** only from `PendingOwnerApproval` → `Rejected`.  
Reason required (non-empty, max 1000). Soft Hold released.

---

## BR-BKG-006 — Auto-approve (Instant Book)

- `InstantBook = true` → skip POA; enter `PendingPayment` with **15m** timer from create.  
- `InstantBook = false` (**v1 default**) → `PendingOwnerApproval` with Soft Hold.

---

## BR-BKG-007 — Auto-cancel / expire

- TTL expiry → `Expired` (system), not `Cancelled`.  
- Renter/Host voluntary stop → `Cancelled`.  
- Payment failure after approve → EHUB-510 (prefer `Expired`).

---

## BR-BKG-008 — Extension

Allowed from `Confirmed` or `InProgress` if:

1. New end date > current end  
2. No conflict with other blocking bookings / asset blocks / **buffers**  
3. Additional days priced at **original locked unit price**

Creates timeline entry + `BookingExtended`.

---

## BR-BKG-009 — Price lock

At create, **unit and total price are frozen** on the Booking.  
Owner raising Asset to 150 AZN later does **not** change an existing 100 AZN booking.

---

## BR-BKG-010 — Asset status during rental

Asset remains `Published` (or admin `Suspended`).  
No “Rented” Asset status. Occupancy = Bookings + Soft/Hard Holds + Buffers + Availability blocks.

---

## BR-BKG-011 — Who can book

- Renter ≠ Host.  
- Asset must be `Published`.  
- Renter active + email confirmed.

---

## BR-BKG-012 — Soft delete

Bookings are not soft-deleted. Terminal statuses only.  
Asset archived/deleted: no **new** bookings; existing Confirmed/InProgress per EHUB-510.

---

## BR-BKG-013 — Idempotency

`POST /bookings` requires `Idempotency-Key`. Same key + renter → same booking (no duplicate Soft Hold).

---

## BR-BKG-014 — Cancellation window (refund stub)

| Actor | Condition | Booking status | Refund (Payment) |
|-------|-----------|----------------|------------------|
| Renter | ≥ 48h before start | → Cancelled | Full (v1 stub) |
| Renter | < 48h before start | → Cancelled | Partial / none (TBD) |
| Host | Before start | → Cancelled | Full to renter |
| System | Payment timeout / expire | → Expired | No capture |

---

## BR-BKG-015 — Booking Buffer (preparation)

After each booking’s `EndDate`, apply **Preparation Buffer** days before the next booking may start.

```text
Booking:     1–5 Jul
Buffer:      1 day  →  6 Jul reserved for return / inspection / cleaning / prep
Next book:   may start 7 Jul
```

- Default buffer: **1 day** (platform).  
- Owner/Company **may override** per Asset (0..N days) — especially Boat, Luxury Car, Construction Equipment.  
- Buffer applies to **all blocking** bookings (Soft Hold, Hard Hold, Confirmed, InProgress).  
- Effective occupied range for conflict checks:

```text
OccupiedStart = StartDate
OccupiedEnd   = EndDate + BufferDays
```

Conflict if requested period overlaps occupied range (inclusive).

---

## BR-BKG-016 — Booking Number

Human-readable business id, separate from GUID `BookingId`:

```text
BK-2026-000000123
```

Format: `BK-{yyyy}-{sequential}` (unique). Operators use this in support/UI. API may accept either id where noted.

---

## BR-BKG-017 — Booking Snapshot

At create, freeze a **read-only Asset snapshot** on the Booking:

- Name, Brand, Model  
- Primary image URL(s)  
- Unit price + currency (also in Money)  
- Owner (Host) display identity  

So history remains correct years later if Asset changes.

---

## BR-BKG-018 — Booking Terms (Rental Rules snapshot)

At create, freeze **Rental Rules** applicable at that moment (min/max days, deposit policy text/ids, cancel hints, etc.).  
Later Asset rule edits do not rewrite historical bookings.

---

## BR-BKG-019 — Booking Version

`Booking.Version` (int) for optimistic concurrency **and** future schema/evolution of snapshots. Increment on mutating transitions.

---

## Decision log (2026-07-19)

| Topic | Decision |
|-------|----------|
| Soft Hold | Yes — Pending blocks calendar |
| Approval TTL | **12h** |
| Payment TTL | **15m**, starts only after Approve |
| Inclusive overlap | Confirmed |
| Buffer | Yes — default 1 day, owner-configurable |
| Price freeze | Confirmed |
| Asset stays Published | Confirmed |
| Optimistic concurrency first | Confirmed |
| Booking Number / Snapshot / Terms / Version | Added |
