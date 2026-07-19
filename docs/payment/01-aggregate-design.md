# EHUB-601 — Payment Aggregate Design

**Status:** READY FOR ARCHITECT REVIEW  
**Boundary:** Payment is its **own** aggregate. It is **not** inside Booking. Booking and Payment reference each other by **id only** (L7).

## Aggregate diagram

```text
Payment (Aggregate Root)
├── PaymentId               (strongly typed Guid Id)
├── BookingId               (Id-only reference — no navigation into Booking)
├── Amount                  (Money: Amount + CurrencyId — copied from Booking TotalPrice) (L1)
├── Currency                (CurrencyId — mirrors Booking money)
├── Provider                (enum/smart enum: e.g. Stripe, Payriff, Manual, Test)
├── ProviderPaymentId?      (opaque id returned by provider adapter)
├── Status                  (smart enum — see 02-state-machine.md)
├── IdempotencyKey          (stable key for the outbound create-payment call)
├── FailureReason?          (normalized reason — never raw provider payload) (L8)
├── PaidAtUtc?              (set when Status → Succeeded)
├── RefundedAmount          (Money — running total; 0 until first refund)
├── Version                 (int — optimistic concurrency + evolution)
├── PaymentStatusHistory    (append-only audit of transitions)
├── PaymentAttempts         (audit of provider interactions: create/webhook/capture)
└── Refunds                 (child collection — each refund is its own audited row) (L5)
```

## What Payment owns

| Concern | Owner |
|---------|-------|
| Charge amount + currency | Payment (copied from Booking snapshot) |
| Provider id + status | Payment |
| Idempotency key for provider call | Payment |
| Refund records + running total | Payment |
| Money audit / attempts | Payment |

## What Payment must NOT own

- Calendar / hold / availability (Booking).
- The decision to `Confirm` a Booking (Booking aggregate reacts to `PaymentSucceeded`).
- Raw provider payloads as domain fields (adapter normalizes first — L8).
- Sending email/SMS (Notification via Outbox).

## Value objects / children

### Money (`Amount`, `RefundedAmount`)

```text
Amount: decimal
CurrencyId: uuid
Invariant: Amount > 0 (for a Payment); RefundedAmount >= 0 and <= Amount
```

### Refund (child)

```text
RefundId: uuid
Amount: Money            (> 0, <= Amount - RefundedAmount at time of issue)
Reason: string
ProviderRefundId?: string
Status: Requested | Succeeded | Failed
RequestedByActorId / RequestedAtUtc
SettledAtUtc?
```

### PaymentStatusHistoryEntry

```text
Id, PaymentId, FromStatus?, ToStatus, AtUtc, ActorId?/System, Reason?
```

### PaymentAttempt (provider interaction audit)

```text
Id, PaymentId, Kind (Create|Webhook|Capture|Refund|Reconcile),
NormalizedResult (Succeeded|Failed|Pending|...),
ProviderReference?, RawPayloadJson? (opaque, audit only), AtUtc
```

## Cross-aggregate references (id-only)

```text
Booking.PaymentId?  ──id──▶  Payment
Payment.BookingId   ──id──▶  Booking
```

No object navigation, no shared domain transaction. Coordination is event-driven (L9):

```text
BookingApproved ──Outbox──▶ create Payment (Pending)
PaymentSucceeded ──Outbox──▶ Booking.Confirm(paymentId)
PaymentFailed/Expired ──Outbox──▶ (Booking expire path / notify)
PaymentRefunded ──Outbox──▶ BookingRefunded
```

## Factory & behaviors (names only — no code this sprint)

- `Payment.Create(bookingId, amountFromBooking, currency, provider, idempotencyKey)` → `Created`/`Pending`
- `MarkPending(providerPaymentId)` — provider session created
- `MarkAuthorized()` — auth-only (reserved for future)
- `MarkSucceeded(paidAtUtc, providerResult)` — capture confirmed
- `MarkFailed(failureReason)` / `MarkCancelled()` / `MarkExpired()`
- `AddRefund(amount, reason, actorId)` → recompute `RefundedAmount` → `PartiallyRefunded` / `Refunded`

All mutations: append status history + attempt audit, bump `Version` (BR-PAY-012).

## Guard rules on the aggregate

- `MarkSucceeded` rejected unless current status ∈ `{Pending, Authorized}`.
- Amount on success must equal `Amount` (else route to mismatch failure — BR-PAY-001).
- `AddRefund` rejected unless status ∈ `{Succeeded, PartiallyRefunded}` and amount ≤ remaining.
- Terminal states (`Failed`, `Cancelled`, `Expired`, `Refunded`) reject further transitions except audit no-ops.

## Sign-off

- [ ] Aggregate boundary (separate, id-only) approved
- [ ] Refund as child collection approved
- [ ] Normalized `FailureReason` (no raw payload in domain) approved
