# EHUB-610 — Failure Scenarios

**Status:** READY FOR ARCHITECT REVIEW  
Each row must map to a test in Sprint 6.1 (see [11-acceptance-edge-tests.md](11-acceptance-edge-tests.md)).

## Payment core

| # | Scenario | Expected |
|---|----------|----------|
| F-PAY-01 | Client sends amount ≠ Booking total | `400`; Payment uses / only ever stores snapshot (L1) |
| F-PAY-02 | Payment succeeds while Booking still `PendingPayment` + hold active | `BookingConfirmed` via Outbox (L3) |
| F-PAY-03 | Payment succeeds **after** Booking `Expired` (late callback) | Booking NOT confirmed; auto full refund (L4) |
| F-PAY-04 | 15m window elapses, no payment | Payment → `Expired`; Booking → `Expired`; hold released |
| F-PAY-05 | Provider declines | Payment → `Failed`; Booking stays payable until TTL; retry allowed (L10) |
| F-PAY-05b | Initiate payment for Expired booking | `409 booking_not_payable` |

## Webhook

| # | Scenario | Expected |
|---|----------|----------|
| F-PAY-06 | Same webhook delivered twice | Processed once (inbox unique key); `200` no-op on replay (L2) |
| F-PAY-07 | Webhook with invalid signature | `401`; no state change; no inbox row (L6) |
| F-PAY-08 | Webhook arrives before create-response persisted | Park/short-retry; not a hard failure; eventually processed once |
| F-PAY-09 | Webhook out of order (Failed then Succeeded) | Terminal-state guards; illegal flip rejected; reconcile |
| F-PAY-10 | Provider retries after our `200` | No second effect (deduped) |
| F-PAY-11 | Webhook amount/currency ≠ Payment.Amount | Mismatch → treat as failure/reconcile; never silently accept (BR-PAY-001/013) |

## Confirm / crash / outbox

| # | Scenario | Expected |
|---|----------|----------|
| F-PAY-12 | `PaymentSucceeded` consumer runs twice | Idempotent `Confirm`; single `Confirmed` |
| F-PAY-13 | Crash after webhook verify, before commit | No inbox row → provider re-delivers → processed once |
| F-PAY-14 | Crash after commit, before outbox dispatch | Outbox retries; consumer idempotent |
| F-PAY-15 | Provider create call times out | Retry with same `IdempotencyKey`; no double charge (I2) |
| F-PAY-16 | Confirm attempted on already-`Confirmed` booking | No-op success |

## Refund

| # | Scenario | Expected |
|---|----------|----------|
| F-PAY-20 | Refund > remaining amount | Rejected `refund_exceeds_remaining` (R2) |
| F-PAY-21 | Refund from non-`Succeeded` payment | Rejected `payment_not_refundable` (R1) |
| F-PAY-22 | Refund provider call fails | Refund row `Failed`; `RefundedAmount` unchanged; original charge intact (R6) |
| F-PAY-23 | Retried refund (same `RefundId`) | Idempotent; single refund at provider (R3) |
| F-PAY-24 | Partial then remaining refund | `PartiallyRefunded` → `Refunded`; audited (L5) |
| F-PAY-25 | Auto-refund on late callback | Full auto-refund; Booking stays terminal (L4) |

## Aggregate / anti-corruption

| # | Scenario | Expected |
|---|----------|----------|
| F-PAY-30 | Add second provider (Stripe → Payriff) | Only a new adapter; Payment aggregate + Booking untouched (L8) |
| F-PAY-31 | Provider returns unknown error code | Normalized to `provider_error_unknown`; raw stored audit-only |
| F-PAY-32 | Attempt to read Booking state synchronously from Payment | Disallowed by design; use events (L7/L9) |
| F-PAY-33 | Two initiate calls for same booking (concurrent) | One Payment; second returns existing (BR-PAY-002) |

## Design principles

1. Amount truth = Booking snapshot, not client, not provider (L1).
2. Effect truth = verified + deduped webhook (L2, L6).
3. Booking confirms only on `Succeeded`, only if hold active (L3, L4).
4. Provider is behind an adapter; domain sees normalized results only (L8).
5. Cross-aggregate effects flow through the Outbox; consumers are idempotent (L7, L9).
6. Refund is additive and audited; never an in-place edit (L5).
7. Failed payment does not freeze Booking forever (L10).

## Sign-off

- [ ] Late-callback + duplicate-webhook rows approved
- [ ] Failed→TTL/retry (L10) approved
- [ ] Crash/outbox recovery rows approved
- [ ] Refund + anti-corruption rows approved
