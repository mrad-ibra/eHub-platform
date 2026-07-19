# EHUB-603 — Payment Lifecycle

**Status:** READY FOR ARCHITECT REVIEW

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

## Failed payment path (L10)

```text
Verified webhook → Failed
        ↓
Payment.MarkFailed(normalized reason)
        ↓
Booking remains PendingPayment (TTL still ticking)
        ↓
Notify renter · allow retry (new Payment) while hold active
        ↓
If still unpaid at 15m → Booking + Payment Expired (hold released)
```

Booking is **never** stuck forever waiting on a failed charge.

## Timeout / expiry path

```text
15m elapses with no Succeeded
        ↓
Booking expire job → Booking Expired (hold released)
Payment → Expired (payment expire / reconcile)
        ↓
Provider session abandoned / cancelled at provider
```

## Late callback path (L4)

```text
Booking already Expired (or other terminal)
Payment still Pending/Authorized OR provider still accepts pay
        ↓
Customer pays late → verified + deduped webhook
        ↓
If Payment still Pending/Authorized:
  MarkSucceeded (money recorded) → do NOT Confirm Booking
  → auto full refund → Refunded
If Payment already Expired/Failed/Cancelled:
  do NOT flip to Succeeded
  → audit attempt + reconcile/refund at provider if money moved
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
- [ ] Failed → TTL continues + retry approved (L10)
- [ ] Late-callback → no confirm + auto-refund approved
- [ ] Refund lifecycle approved
