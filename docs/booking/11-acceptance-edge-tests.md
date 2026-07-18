# Companion — Acceptance, Edge Cases, Test Scenarios

**Status:** APPROVED WITH MINOR CHANGES (Architect 2026-07-19).  
**Sources:** [00-business-rules.md](00-business-rules.md), [10-failure-scenarios.md](10-failure-scenarios.md).

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-01 | Overlapping Soft/Hard Hold or committed bookings on same Asset → 409. |
| AC-02 | Soft Hold (`PendingOwnerApproval`) and Hard Hold (`PendingPayment`) block calendar (+ buffer). |
| AC-03 | POA > **12h** or PP > **15m** → `Expired`; hold released. |
| AC-04 | Payment timer starts only on Approve (or Instant Book create). |
| AC-05 | Host reject only from POA with reason; Soft Hold released. |
| AC-06 | Price frozen at create; Asset price change does not mutate Booking. |
| AC-07 | Asset stays Published; occupancy is booking/hold/buffer-derived. |
| AC-08 | Buffer default 1 day; request on buffer day → 409. |
| AC-09 | `BookingNumber` assigned (`BK-yyyy-…`); unique. |
| AC-10 | AssetSnapshot + BookingTerms captured at create; immutable. |
| AC-11 | Idempotent POST → same BookingId/Number. |
| AC-12 | Domain events + outbox in same TX as status change. |

---

## Edge Cases

| ID | Case | Expected |
|----|------|----------|
| E01 | 1-day rental | OK if MinRentalDays ≤ 1 |
| E02 | 1–5 vs 5–10 | Reject (inclusive) |
| E03 | 1–5 buffer 0, request 6–8 | Accept |
| E04 | 1–5 buffer 1, request 6–8 | Reject |
| E05 | 1–5 buffer 1, request 7–9 | Accept |
| E06 | Soft Hold active, second create same dates | 409 |
| E07 | Soft Hold expired, second create | Accept |
| E08 | Own asset | 403 |
| E09 | Instant Book | Skip Soft Hold → PP 15m |
| E10 | Approve after 12h Expired | Invalid transition |
| E11 | Snapshot after Asset rename | Old name on booking |
| E12 | BufferDays = 0 on Asset | Adjacent day 6 allowed after 1–5 |

---

## Test Scenarios (Sprint 5.1)

### Unit

| T | Scenario |
|---|----------|
| U01 | Period End < Start rejected |
| U02 | Inclusive overlap + buffer occupied end |
| U03 | Illegal transitions |
| U04 | Approve → PP + ExpiresAt = now+**15m** |
| U05 | Create → ExpiresAt = now+**12h** |
| U06 | Price + Snapshot + Terms immutable |
| U07 | BookingNumber format |

### Integration

| T | Scenario |
|---|----------|
| I01 | Soft Hold blocks second booking |
| I02 | Expire 12h frees dates |
| I03 | Approve then expire 15m frees dates |
| I04 | Buffer day conflict |
| I05 | Concurrent creates → one success |
| I06 | F01–F08, F20–F23, F60–F61 |

### API

| T | Scenario |
|---|----------|
| A01 | Missing Idempotency-Key → 400 |
| A02 | GET by BookingNumber |
| A03 | Response includes snapshot + bookingNumber + bufferDays |

---

## Sign-off

- [x] AC / edges / tests updated for Soft Hold, TTL, Buffer, Snapshot, Number
