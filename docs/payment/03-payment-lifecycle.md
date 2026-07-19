# EHUB-603 — Payment Lifecycle

**Status:** DRAFT — awaiting Architect review.

## Happy path (approve → pay → confirm)

```text
Booking approved (PendingPayment, 15m Hard Hold timer starts)
        ↓  BookingApproved (Outbox)
Payment created
  Amount = Booking.TotalPrice snapshot   ← client price ignored (L1)
  Status = Created → Pending
  IdempotencyKey assigned
        ↓
Provider session opened (ProviderPaymentId captured via adapter)
        ↓
Customer pays at provider
        ↓
Provider webhook  →  signature verified (L6)  →  inbox dedupe (L2)
        ↓
Adapter maps payload → ProviderPaymentResult (L8)
        ↓
Payment.MarkSucceeded(paidAtUtc)   (amount matches Amount)
        ↓  PaymentSucceeded (Outbox, L9)
Booking still PendingPayment + hold active?
   ├─ yes → Booking.Confirm(paymentId) → Confirmed (L3)
   └─ no  → do NOT confirm → reconcile / auto-refund (L4)
```

## Sequence — create payment on approval

```mermaid
sequenceDiagram
  participant Outbox
  participant PayApp as Payment Application
  participant Payment as Payment Aggregate
  participant Adapter as Provider Adapter
  participant Provider

  Outbox-->>PayApp: BookingApproved (bookingId, total snapshot)
  PayApp->>Payment: Create(amount=Booking.TotalPrice, key)
  PayApp->>Adapter: CreateSession(amount, currency, key)
  Adapter->>Provider: create payment (idempotency key)
  Provider-->>Adapter: providerPaymentId + redirect
  Adapter-->>PayApp: normalized result
  PayApp->>Payment: MarkPending(providerPaymentId)
  PayApp-->>Outbox: PaymentPending (pay link → notify)
```

## Sequence — webhook → confirm

```mermaid
sequenceDiagram
  participant Provider
  participant API as Payment Webhook API
  participant Verify as Signature Verify
  participant Inbox
  participant Adapter
  participant Payment
  participant Outbox
  participant Booking

  Provider->>API: POST /payments/webhook/{provider}
  API->>Verify: verify signature (L6)
  alt invalid
    Verify-->>API: reject
    API-->>Provider: 401 (no effect)
  else valid
    API->>Inbox: seen(eventId)? (L2)
    alt duplicate
      Inbox-->>API: already processed
      API-->>Provider: 200 (no second effect)
    else new
      API->>Adapter: map payload → ProviderPaymentResult (L8)
      API->>Payment: MarkSucceeded / MarkFailed
      Payment->>Outbox: PaymentSucceeded / PaymentFailed
      API-->>Provider: 200
      Outbox-->>Booking: Confirm(paymentId) if hold active (L3/L4)
    end
  end
```

## Timeout / expiry path

```text
15m elapses with no Succeeded
        ↓
Booking expire job → Booking Expired (hold released)
Payment → Expired (by payment expire job / reconcile)
        ↓
Provider session abandoned / cancelled at provider
```

## Late callback path (L4)

```text
Payment session still open at provider after Booking Expired
        ↓
Customer pays late → provider webhook (verified, deduped)
        ↓
Payment.MarkSucceeded — recorded as attempt + Succeeded row
        ↓
Booking is Expired (terminal) → NOT confirmed
        ↓
Raise reconciliation: PaymentSucceededForExpiredBooking
        ↓
Auto-refund per policy (07-refund-strategy.md)  →  Refunded
```

## Refund path (separate operation, L5)

```text
Booking cancellation decides refund amount (BR-BKG-014)
        ↓  RefundInstruction (Outbox)
Payment.AddRefund(amount, reason)
        ↓
Adapter → Provider refund (idempotency key)
        ↓
Refund Succeeded → RefundedAmount += amount
        ↓
Status → PartiallyRefunded | Refunded
        ↓  PaymentRefunded (Outbox) → BookingRefunded
```

## Responsibilities by step

| Step | Owner component |
|------|-----------------|
| Amount from snapshot | Payment aggregate (copies Booking `TotalPrice`) |
| Provider session / charge | Provider adapter (ACL) |
| Signature verify | Webhook endpoint + provider adapter |
| Dedupe | Inbox / idempotency store |
| Confirm decision | Booking aggregate (reacts to event) |
| Refund | Payment aggregate + adapter |
| Notify | Outbox → Notification |

## Sign-off

- [ ] Happy path + confirm-after-succeeded approved
- [ ] Late-callback → reconcile/auto-refund approved
- [ ] Refund lifecycle approved
