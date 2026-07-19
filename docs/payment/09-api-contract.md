# EHUB-609 — API Contract

**Status:** READY FOR ARCHITECT REVIEW

**Base:** `/api/v1/payments`  
**Auth:** JWT unless noted (webhook is signature-authenticated, not JWT).  
**Errors:** RFC 7807 ProblemDetails.  
**Idempotency:** `Idempotency-Key` required on `POST /payments`.

## Endpoints

| Method | Path | Actor | Description |
|--------|------|-------|-------------|
| `POST` | `/payments` | Renter/System | Initiate payment for a Booking |
| `GET` | `/payments/{id}` | Renter/Host/Admin | Payment detail |
| `GET` | `/payments/by-booking/{bookingId}` | Renter/Host/Admin | Payment(s) for a booking |
| `POST` | `/payments/webhook/{provider}` | Provider | Provider callback (signature auth) |
| `POST` | `/payments/{id}/refund` | Admin/System | Issue a refund (separate op, L5) |
| `POST` | `/payments/{id}/reconcile` | Admin/System | Force reconcile against provider |

> Payment initiation is normally driven by `BookingApproved` (Outbox). The `POST /payments` endpoint is the explicit/manual entry point and is idempotent per Booking.

## POST /payments

### Request

```json
{
  "bookingId": "uuid",
  "provider": "Stripe"
}
```

Headers: `Idempotency-Key: <string>`

> **No amount in the request.** Amount is taken from the Booking `TotalPrice` snapshot server-side (L1, BR-PAY-001). If the client sends an `amount` field, respond **`400 validation_failed`** (do not silently ignore in production API).

### Response `201` (or `200` on idempotent replay)

```json
{
  "id": "uuid",
  "bookingId": "uuid",
  "status": "Pending",
  "amount": { "amount": 500.00, "currencyId": "uuid" },
  "provider": "Stripe",
  "providerPaymentId": "pi_...",
  "redirectUrl": "https://provider/checkout/...",
  "expiresAtUtc": "2026-08-01T12:15:00Z",
  "version": 1
}
```

`expiresAtUtc` aligns with the Booking Payment TTL (15m).

### Errors

| Status | Code | When |
|--------|------|------|
| 400 | validation_failed | Missing `Idempotency-Key` / bad body |
| 404 | booking_not_found | Unknown `bookingId` |
| 409 | booking_not_payable | Booking not in `PendingPayment` / hold expired |
| 409 | payment_already_active | Non-terminal Payment exists (returns existing) |
| 403 | forbidden | Caller not the renter/authorized |

## POST /payments/webhook/{provider}

- Public route; **signature-authenticated** (L6). See [05-webhook-strategy.md](05-webhook-strategy.md).
- Body = raw provider payload.

| Status | Meaning |
|--------|---------|
| 200 | Verified + processed, or verified duplicate (no-op, L2) |
| 401 | Signature invalid — no effect |
| 400 | Valid signature, unparseable body |
| 404 | Unknown provider / payment reference |

Response body: minimal `{ "received": true }`. **No** domain state is exposed to the provider.

## POST /payments/{id}/refund

### Request

```json
{
  "amount": { "amount": 200.00, "currencyId": "uuid" },
  "reason": "Renter cancelled ≥ 48h before start"
}
```

### Response `202`

```json
{
  "refundId": "uuid",
  "paymentId": "uuid",
  "status": "Requested",
  "amount": { "amount": 200.00, "currencyId": "uuid" },
  "refundedAmountTotal": { "amount": 200.00, "currencyId": "uuid" },
  "paymentStatus": "PartiallyRefunded"
}
```

### Errors

| Status | Code | When |
|--------|------|------|
| 409 | payment_not_refundable | Status not `Succeeded`/`PartiallyRefunded` |
| 400 | refund_exceeds_remaining | `amount > Amount - RefundedAmount` (R2) |

## GET /payments/{id}

```json
{
  "id": "uuid",
  "bookingId": "uuid",
  "status": "Succeeded",
  "amount": { "amount": 500.00, "currencyId": "uuid" },
  "refundedAmount": { "amount": 0.00, "currencyId": "uuid" },
  "provider": "Stripe",
  "providerPaymentId": "pi_...",
  "paidAtUtc": "2026-08-01T12:03:11Z",
  "failureReason": null,
  "version": 3,
  "statusHistory": [
    { "from": null, "to": "Created", "atUtc": "..." },
    { "from": "Created", "to": "Pending", "atUtc": "..." },
    { "from": "Pending", "to": "Succeeded", "atUtc": "..." }
  ]
}
```

> `failureReason` is a **normalized** code, never a raw provider payload (L8).

## Feature ownership

Request/response DTOs live under Application `Payments/` vertical slices — **not** in `eHub.Contracts` (mirrors Booking convention).

## Sign-off

- [ ] Route list approved
- [ ] No-amount-in-request approved
- [ ] Webhook signature-auth contract approved
