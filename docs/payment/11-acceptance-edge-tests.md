# Companion — Acceptance, Edge Cases, Test Scenarios

**Status:** DRAFT — awaiting Architect review.  
**Sources:** [00-business-rules.md](00-business-rules.md), [10-failure-scenarios.md](10-failure-scenarios.md).

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-01 | Payment `Amount` equals the Booking `TotalPrice` snapshot; client-supplied amount is ignored (L1). |
| AC-02 | Booking becomes `Confirmed` only after Payment reaches `Succeeded` (L3). |
| AC-03 | A late `Succeeded` for an `Expired`/terminal Booking does NOT confirm it (L4). |
| AC-04 | The same webhook is processed exactly once; replays are no-ops (L2). |
| AC-05 | Webhook signature is verified before any effect; invalid → `401`, no change (L6). |
| AC-06 | Payment and Booking are separate aggregates linked by id; coordination via Outbox (L7, L9). |
| AC-07 | Provider payloads never bind into the domain model; adapter normalizes first (L8). |
| AC-08 | Refund is a separate, audited operation; `RefundedAmount` ≤ `Amount` (L5). |
| AC-09 | Only one non-terminal Payment per Booking; re-initiate returns existing. |
| AC-10 | Payment window = Booking Payment TTL (15m); expiry → `Expired`, hold released. |
| AC-11 | Every status change writes status history; every provider call writes an attempt (audit). |
| AC-12 | Outbound provider call + refund carry stable idempotency keys (no double charge). |

---

## Edge Cases

| ID | Case | Expected |
|----|------|----------|
| E01 | Client amount ≠ Booking total | Booking total used |
| E02 | Provider captured amount ≠ Payment.Amount | Mismatch → fail/reconcile |
| E03 | Duplicate webhook | Processed once |
| E04 | Out-of-order webhooks (Failed→Succeeded) | Guards reject illegal flip |
| E05 | Webhook before create persisted | Parked/retried, processed once |
| E06 | Succeeded lands 1s after Expired | No confirm; auto-refund |
| E07 | Full refund in two partials | `PartiallyRefunded` → `Refunded` |
| E08 | Over-refund by 0.01 | Rejected |
| E09 | Refund on `Pending` payment | Rejected |
| E10 | Second initiate for same booking | Returns existing Payment |
| E11 | Invalid signature | `401`, no effect |
| E12 | Provider timeout on create, retried | Same key, one session |
| E13 | Add new provider | New adapter only; no domain change |
| E14 | Booking cancelled → refund instruction | Refund executed + `BookingRefunded` |

---

## Test Scenarios (Sprint 6.1)

### Unit

| T | Scenario |
|---|----------|
| U01 | `Payment.Create` copies amount from Booking snapshot; ignores input amount |
| U02 | Illegal transitions rejected (`Expired`→`Succeeded`, refund from `Pending`) |
| U03 | `MarkSucceeded` with amount ≠ `Amount` → mismatch failure |
| U04 | `AddRefund` guards: `> 0`, `≤ remaining`, correct status derivation |
| U05 | Status history + attempt appended on every transition |
| U06 | `Version` increments on mutation |

### Integration

| T | Scenario |
|---|----------|
| I01 | Approve → create Payment → webhook Succeeded → BookingConfirmed |
| I02 | Duplicate webhook → single confirm (inbox unique key) |
| I03 | 15m expiry → Payment Expired + Booking Expired |
| I04 | Late Succeeded after Expired → no confirm → auto-refund |
| I05 | Signature invalid → `401`, no state change |
| I06 | Crash between verify and commit → reprocessed once |
| I07 | Refund partial then full → `Refunded` + `BookingRefunded` |
| I08 | Concurrent initiate → one Payment |
| I09 | F-PAY-06…11, F-PAY-12…16, F-PAY-20…25 |

### API

| T | Scenario |
|---|----------|
| A01 | `POST /payments` missing `Idempotency-Key` → `400` |
| A02 | `POST /payments` with amount in body → amount ignored, booking total used |
| A03 | `POST /payments/webhook/{provider}` bad signature → `401` |
| A04 | `POST /payments/{id}/refund` exceeding remaining → `400` |
| A05 | `GET /payments/{id}` returns normalized `failureReason` + status history |

---

## Sign-off

- [ ] AC covers all 9 locked principles (L1–L9)
- [ ] Edges + failure matrix mapped to tests
- [ ] Test list approved for Sprint 6.1
