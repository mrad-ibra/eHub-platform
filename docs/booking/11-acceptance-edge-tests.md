# EHUB-500…510 companion — Acceptance, Edge Cases, Test Scenarios

**Status:** Draft for sign-off. Used as Sprint 5.1 QA gate.  
**Source of truth for rules:** [00-business-rules.md](00-business-rules.md), [10-failure-scenarios.md](10-failure-scenarios.md).

---

## Acceptance Criteria (pack-level)

| ID | Criterion |
|----|-----------|
| AC-01 | Creating two overlapping blocking bookings on the same Asset is impossible (API returns 409). |
| AC-02 | PendingOwnerApproval and PendingPayment block calendar until terminal non-blocking status. |
| AC-03 | POA older than 24h and PP older than 30m become Expired via job; calendar frees. |
| AC-04 | Host can reject only from PendingOwnerApproval with a reason. |
| AC-05 | Unit/total price on Booking is frozen at create; Asset price change does not mutate Booking. |
| AC-06 | Asset status stays Published during rental; occupancy is booking-derived. |
| AC-07 | POST /bookings with same Idempotency-Key returns the same BookingId. |
| AC-08 | Payment failures never leave a Confirmed booking without a successful Payment. |
| AC-09 | Domain events for status changes are written in the same transaction (outbox). |
| AC-10 | Design pack signed before any Booking C# code lands. |

---

## Edge Cases

| ID | Case | Expected |
|----|------|----------|
| E01 | StartDate = EndDate (1-day rental) | Allowed if MinRentalDays ≤ 1 |
| E02 | Request ends on day existing booking starts | Reject (inclusive overlap) |
| E03 | Adjacent: existing ends 5 Jul, request starts 6 Jul | Accept |
| E04 | Owner blackout + free booking days | Accept only if no intersection with blackout |
| E05 | Renter books own asset | 403 |
| E06 | Instant Book on (future) | Skip POA → PendingPayment |
| E07 | Extend by 0 days | Validation fail |
| E08 | Extend into blackout | 409 |
| E09 | Cancel in Draft (if used) | Allowed → Cancelled |
| E10 | Approve after TTL already Expired | 409 / invalid transition |
| E11 | Concurrent approve + expire | One wins; other gets invalid transition |
| E12 | Multi-currency Asset change after create | Booking keeps original CurrencyId |

---

## Test Scenarios (Sprint 5.1 mapping)

### Unit (Domain)

| T | Scenario |
|---|----------|
| U01 | BookingPeriod rejects End < Start |
| U02 | Overlap helper: partial / full / adjacent |
| U03 | Illegal transition throws (e.g. Rejected → Confirmed) |
| U04 | Approve from POA → PendingPayment + expiresAt set to +30m |
| U05 | Expire from POA/PP → Expired |
| U06 | Price snapshot immutable after create |

### Application / Integration

| T | Scenario |
|---|----------|
| I01 | Create → POA; second create overlapping → 409 |
| I02 | Idempotent create replay |
| I03 | Host approve → PP; payment confirm → Confirmed |
| I04 | Host reject → Rejected; third party can book same dates |
| I05 | Expire job frees dates |
| I06 | Concurrent creates under load → exactly one success |
| I07 | Failure F01–F04 from EHUB-510 |

### API contract tests

| T | Scenario |
|---|----------|
| A01 | POST without Idempotency-Key → 400 |
| A02 | PATCH approve as non-host → 403 |
| A03 | GET mine returns only caller’s bookings |

---

## Sign-off

- [ ] AC-01…10 accepted  
- [ ] Edge list accepted  
- [ ] Test matrix sufficient to start Sprint 5.1
