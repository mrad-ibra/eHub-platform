# Companion — Acceptance, Edge Cases, Test Scenarios

**Status:** READY FOR ARCHITECT REVIEW  
**Sources:** [00](00-business-rules.md), [02](02-state-machine.md), [10](10-failure-scenarios.md)

---

## Mandatory acceptance scenarios (Architect-requested)

These are the **must-pass** Sprint 6.1 scenarios. Each maps to AC / unit / integration / API below.

| ID | Scenario | Expected |
|----|----------|----------|
| S01 | **Create payment for pending booking** | Booking `PendingPayment` + active hold → Payment `Pending`; amount = Booking `TotalPrice` |
| S02 | **Reject payment for expired booking** | Booking `Expired` / hold dead → `409 booking_not_payable`; no Payment created |
| S03 | **Reject client-supplied amount mismatch** | Body contains amount ≠ snapshot → reject / ignore; stored Amount always = snapshot (L1) |
| S04 | **Process duplicate webhook once** | Same `EventId` twice → one effect; second `200` no-op (L2) |
| S05 | **Handle successful payment** | Verified Succeeded → Payment `Succeeded` → Outbox → Booking `Confirmed` (L3) |
| S06 | **Handle failed payment** | Verified Failed → Payment `Failed`; Booking stays `PendingPayment` until TTL; retry allowed (L10) |
| S07 | **Handle late success webhook** | Booking already `Expired` → **no Confirm**; auto full refund → Payment `Refunded` (L4) |
| S08 | **Handle partial refund** | Refund `< remaining` → `PartiallyRefunded`; `RefundedAmount` updated; audited |
| S09 | **Handle full refund** | Refund brings total to `Amount` → `Refunded`; Outbox → Booking refunded path |
| S10 | **Reject refund above paid amount** | `amount > Amount − RefundedAmount` → `400 refund_exceeds_remaining` |
| S11 | **Retry provider timeout safely** | Create/refund timeout → retry **same** idempotency key → no double charge (I2) |

---

## Acceptance Criteria (L1–L10)

| ID | Criterion |
|----|-----------|
| AC-01 | Amount = Booking `TotalPrice` snapshot; client amount not authoritative (L1) |
| AC-02 | Confirm only after `Succeeded` + active hold (L3) |
| AC-03 | Late success never confirms terminal Booking (L4) |
| AC-04 | Duplicate webhook → single effect (L2) |
| AC-05 | Invalid signature → `401`, no effect (L6) |
| AC-06 | Separate aggregates; Outbox only (L7, L9) |
| AC-07 | Provider models stay behind adapter (L8) |
| AC-08 | Refund separate; partial/full rules; never over-refund (L5) |
| AC-09 | ≤1 active Payment per Booking |
| AC-10 | 15m window; Expired releases hold |
| AC-11 | Failed does not stick Booking forever; TTL + retry (L10) |
| AC-12 | Audit history on every transition / provider call |
| AC-13 | Outbound create/refund idempotent under timeout |

---

## Edge Cases

| ID | Case | Expected |
|----|------|----------|
| E01 | Client amount ≠ snapshot | Snapshot wins / request rejected |
| E02 | Provider captured ≠ Payment.Amount | Mismatch fail/reconcile |
| E03 | Duplicate webhook | Once |
| E04 | Failed then Succeeded out of order | Illegal flip rejected |
| E05 | Webhook before create persisted | Park/retry once |
| E06 | Succeeded 1s after Booking Expired | No confirm; auto-refund |
| E07 | Two partials → full | `PartiallyRefunded` → `Refunded` |
| E08 | Over-refund 0.01 | Reject |
| E09 | Refund while `Pending` | Reject |
| E10 | Concurrent initiate | One Payment |
| E11 | Bad signature | `401` |
| E12 | Provider create timeout | Same key, one session |
| E13 | New provider | Adapter only |
| E14 | Failed then retry while hold live | Second Payment allowed after terminal Failed |

---

## Test Scenarios (Sprint 6.1)

### Unit

| T | Maps to | Scenario |
|---|---------|----------|
| U01 | S01,S03 | Create copies snapshot; rejects bad amount |
| U02 | S02 | Create rejected when Booking not payable |
| U03 | — | Illegal transitions (`Expired`→`Succeeded`) |
| U04 | S08,S09,S10 | Refund guards + status derivation |
| U05 | S06 | Failed leaves Booking TTL path open |
| U06 | — | Version + history on mutation |

### Integration

| T | Maps to | Scenario |
|---|---------|----------|
| I01 | S01,S05 | Approve → pay → webhook → Confirm |
| I02 | S04 | Duplicate webhook → one Confirm |
| I03 | S02 | Expired booking → cannot initiate |
| I04 | S07 | Late Succeeded → no Confirm → auto-refund |
| I05 | S06 | Failed webhook → Failed Payment; Booking still PendingPayment |
| I06 | S08,S09 | Partial then full refund |
| I07 | S10 | Over-refund rejected |
| I08 | S11 | Provider timeout retry same key |
| I09 | — | Invalid signature → no state |
| I10 | — | Concurrent initiate → one row |

### API

| T | Maps to | Scenario |
|---|---------|----------|
| A01 | S01 | `POST /payments` happy path |
| A02 | S03 | Amount in body rejected/ignored |
| A03 | S02 | Expired booking → 409 |
| A04 | S04 | Webhook duplicate → 200 no-op |
| A05 | S05/S06 | Webhook success/fail |
| A06 | S08–S10 | Refund endpoints |
| A07 | — | Missing Idempotency-Key → 400 |

---

## Sign-off

- [ ] S01–S11 accepted as 6.1 gate
- [ ] AC covers L1–L10
- [ ] Partial vs full + late auto-refund covered
