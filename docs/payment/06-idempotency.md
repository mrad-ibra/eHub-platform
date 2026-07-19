# EHUB-606 — Idempotency

**Status:** READY FOR ARCHITECT REVIEW

## Principle

> Every payment effect happens **at most once**, even under retries, duplicate webhooks, network timeouts, and crashes between steps. The **same webhook is processed once** (L2).

## Three idempotency surfaces

| # | Surface | Key | Store | Guarantee |
|---|---------|-----|-------|-----------|
| I1 | Inbound API (initiate payment) | `Idempotency-Key` header + `BookingId` | `IdempotencyKeys` | Same key → same Payment, no duplicate row |
| I2 | Outbound provider call | `Payment.IdempotencyKey` | Passed to provider | Provider create/refund not double-charged |
| I3 | Inbound webhook | Provider `EventId` | `PaymentInbox` (unique `Provider,EventId`) | Same webhook processed once (L2) |

## I1 — Inbound API idempotency

- `POST /payments` requires `Idempotency-Key` (mirrors Booking `POST /bookings`).
- Same key + same `BookingId` → return the **existing** Payment (200), never create a second one (BR-PAY-002).
- Different key but Booking already has a non-terminal Payment → still return existing (one active payment rule wins).
- Processing lock TTL for in-flight requests (reuse Booking pattern: `IdempotencyProcessingTtl = 5 min`).

## I2 — Outbound provider idempotency

- `Payment.IdempotencyKey` is generated once at Payment creation and **reused** on retries of the provider create call.
- Refund calls carry a per-refund idempotency key (`RefundId`-derived) so retried refunds don't double-refund.
- If the provider create call times out, retry with the **same** key → provider returns the original session, not a new charge.

## I3 — Inbound webhook idempotency (L2)

```text
BEGIN TX
  INSERT INTO PaymentInbox (Provider, EventId, ...)   -- UNIQUE(Provider, EventId)
  -- if duplicate key violation → ROLLBACK → ack 200, no effect
  apply Payment transition
  append PaymentStatusHistory
  enqueue Outbox event
COMMIT
```

- The **unique constraint is the dedupe** — not an in-memory check (survives concurrency + crashes).
- Insert + effect + outbox share **one** transaction, so either all happen or none (no half-applied webhook).

## Exactly-once *effect* (not just delivery)

True exactly-once delivery is impossible; we achieve **exactly-once effect**:

```text
at-least-once delivery (provider retries)
        + idempotent processing (inbox unique key)
        + transactional outbox (single TX with state change)
        = each business effect applied once
```

## Consumer idempotency (Booking side, L9)

- `PaymentSucceeded` consumer (`Booking.Confirm`) must be idempotent: confirming an already-`Confirmed` booking with the same `paymentId` is a no-op success.
- Confirming an `Expired`/terminal booking is rejected → routes to reconcile (L4), never throws the message into a poison loop without handling.

## Crash-safety matrix

| Crash point | Recovery |
|-------------|----------|
| After provider create, before `MarkPending` | Reconcile by `IdempotencyKey`; provider returns same session |
| After webhook verified, before commit | No inbox row → provider retry re-delivers → processed once |
| After commit, before outbox dispatch | Outbox dispatcher retries; consumer idempotent |
| Duplicate webhook delivery | Inbox unique key rejects second → `200` no-op |

## Sign-off

- [ ] Three idempotency surfaces approved
- [ ] Inbox unique-key = dedupe approved
- [ ] Single-TX (inbox + state + outbox) approved
