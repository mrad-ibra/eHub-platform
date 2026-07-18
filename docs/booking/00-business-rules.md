# EHUB-500 — Booking Business Rules

**Status:** Draft (proposed defaults). Change only via explicit decision note.

## Core principle

> Two renters must never hold overlapping **blocking** bookings on the same Asset.

---

## BR-BKG-001 — Overlap

Booking periods on the same `AssetId` **must not overlap** if both bookings are in a **blocking** status.

**Blocking statuses:**  
`PendingOwnerApproval`, `PendingPayment`, `Confirmed`, `InProgress`

**Non-blocking (do not reserve calendar):**  
`Draft`, `Rejected`, `Cancelled`, `Expired`, `Completed`, `Refunded`

Overlap includes: full contain, partial intersection, identical range, adjacent-with-touch if policy is exclusive end (see BR-BKG-002).

---

## BR-BKG-002 — Period model

- Granularity (v1): **calendar days** (`DateOnly` start/end inclusive).  
- Example: 1–5 Jul and 5–6 Jul → **overlap on 5 Jul** → reject.  
- Hourly rentals: out of scope for v1 (follow-up epic).

---

## BR-BKG-003 — Pending holds availability

**Yes.** Unconfirmed bookings in `PendingOwnerApproval` or `PendingPayment` **do** block availability until they leave blocking set (approve path, reject, expire, cancel).

Rationale: otherwise double-sell during approval/payment window.

---

## BR-BKG-004 — Pending TTL (auto-expire)

| Status | TTL (proposed) | On expire |
|--------|----------------|-----------|
| `PendingOwnerApproval` | **24 hours** | → `Expired`; calendar freed; event `BookingExpired` |
| `PendingPayment` | **30 minutes** | → `Expired`; calendar freed; event `BookingExpired` |

Background job: `ExpirePendingBookingsJob` (Hangfire/similar).

---

## BR-BKG-005 — Owner reject

Owner (Host) **may reject** only from `PendingOwnerApproval` → `Rejected`.  
Reason required (non-empty, max 1000).

---

## BR-BKG-006 — Auto-approve (Instant Book)

- If Asset has `InstantBook = true` (future field on Asset commercial/support): skip `PendingOwnerApproval`, go `PendingPayment` (or `Confirmed` if offline pay — not v1).  
- If `InstantBook = false` (v1 default): `Draft/Create` → `PendingOwnerApproval`.

**v1 default:** Instant Book **off** (host approval required).

---

## BR-BKG-007 — Auto-cancel / expire

- TTL expiry → `Expired` (system), not `Cancelled`.  
- Renter/Host voluntary stop → `Cancelled`.  
- Payment failure after approve → see EHUB-510 (back to cancellable / expired per flow).

---

## BR-BKG-008 — Extension

Allowed from `Confirmed` or `InProgress` if:

1. New end date > current end  
2. No overlap with other blocking bookings / asset blocks  
3. Same price policy: **prorated at original locked unit price** (no silent price hike)

Creates timeline entry + `BookingExtended` event.

---

## BR-BKG-009 — Price lock

At booking creation, **total and unit price are frozen** on the Booking (`Money` snapshot).  
Owner changing Asset pricing **does not** change existing bookings.

---

## BR-BKG-010 — Asset status during rental

Asset remains `Published` (or `Suspended` by admin).  
We **do not** move Asset to a “Rented” status. Occupancy is derived from Bookings + Availability blocks.

---

## BR-BKG-011 — Who can book

- Renter ≠ Host (cannot book own asset).  
- Asset must be `Published`.  
- Renter account active + email confirmed (reuse Identity rules).

---

## BR-BKG-012 — Soft delete

Bookings are **not** soft-deleted for history. Terminal statuses only.  
If Asset is soft-deleted/archived: no **new** bookings; existing Confirmed/InProgress handled per EHUB-510.

---

## BR-BKG-013 — Idempotency

`POST /bookings` requires `Idempotency-Key`. Same key + same renter → same booking result (no duplicate hold).

---

## BR-BKG-014 — Cancellation window (refund policy stub)

| Actor | Condition | Booking status | Refund (Payment module) |
|-------|-----------|----------------|-------------------------|
| Renter | ≥ 48h before start | → Cancelled | Full (v1 stub) |
| Renter | < 48h before start | → Cancelled | Partial / none (TBD finance) |
| Host | Before start | → Cancelled | Full to renter |
| System | Payment timeout / expire | → Expired | No capture |

Exact money movements live in Payment BRS later; Booking only emits events.

---

## Acceptance (design)

- [ ] Product agrees TTL values  
- [ ] Product agrees Instant Book default off  
- [ ] Product agrees inclusive day overlap rule  
- [ ] Finance agrees cancel refund stub for v1
