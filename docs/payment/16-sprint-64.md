# Sprint 6.4 — Refund HTTP API + Workflow

**Status:** **READY FOR REVIEW**  
**Date:** 2026-07-20

## Sprint goal

Secure, idempotent, audited refund flow for authorized operators — with DB-level concurrency protection so parallel partial refunds cannot exceed the payment total.

## Scope delivered

| Area | Implementation |
|------|----------------|
| Domain | `BeginRefund` / `CompleteRefund` / `FailRefund`; `Refund.IdempotencyKey`; guard via `EnsureRefundAmount` + `ck_payments_refunded_bounds` |
| Events | `RefundRequested`, `RefundSucceeded`, `RefundFailed`, `PaymentPartiallyRefunded`, `PaymentFullyRefunded`, `PaymentRefunded` |
| Commands | `CreateRefundCommand` (MVP); queries `GetRefundQuery`, `ListPaymentRefundsQuery` |
| Provider | `IPaymentProvider.RefundAsync` — `FakePaymentProvider` succeeds; Stripe/Payriff skeleton unchanged |
| HTTP | `POST /api/v1/payments/{paymentId}/refunds` (`Idempotency-Key` header) |
| HTTP | `GET /api/v1/payments/{paymentId}/refunds`, `GET /api/v1/refunds/{refundId}` |
| Auth | `payments.refund` (create), `payments.refund.read` (list/get); **Admin-only execute** (BR-PAY-017) |
| Idempotency | Unique `(PaymentId, IdempotencyKey)` → `ux_payment_refunds_payment_idempotency` |
| Concurrency | `AggregateVersion` token + check constraint + PG mapper → 409 |
| Migration | `AddRefundIdempotency` |

## Refund state machine (MVP)

```
Requested → Succeeded   (provider OK + CompleteRefund)
Requested → Failed      (provider error + FailRefund)
```

Manual approval (`Approved → Processing`) deferred — not required for MVP.

## Authorization (BR-PAY-017)

| Actor | Execute | Read |
|-------|---------|------|
| Admin | ✅ `payments.refund` + Admin role | ✅ |
| Renter / Host | ❌ (future: request workflow) | ✅ own booking via `payments.read` / `payments.refund.read` |
| System | ✅ auto-refund worker (not HTTP) | N/A |

## API error mapping

| Condition | HTTP |
|-----------|------|
| Payment not found | 404 |
| Refund not allowed (status / no provider id) | 409 |
| Amount exceeds remaining | 400 / 409 (domain / DB) |
| Currency mismatch | 400 |
| Idempotency payload mismatch | 409 |
| Missing `Idempotency-Key` | 400 |
| Forbidden (non-admin create) | 403 |

## Exit criteria

| Criterion | Status |
|-----------|--------|
| Refund API works (Fake provider) | ✅ |
| Authorization separated | ✅ |
| Domain guards complete | ✅ |
| Idempotency DB-protected | ✅ |
| Outbox events written | ✅ |
| Unit tests | ✅ |
| PostgreSQL parallel partial refund IT | ✅ |
| Business rules BR-PAY-017 | ✅ |
| OpenAPI via controller attributes | ✅ |

## Test coverage

| Scenario | Test |
|----------|------|
| Partial / full refund | `CreateRefundCommandHandlerTests` |
| Idempotent replay | unit + PG IT |
| Payload mismatch | unit |
| Non-admin forbidden | unit |
| Failed payment rejected | unit |
| Amount exceeded | unit |
| Provider failure → Failed refund | unit |
| Parallel partial refunds ≤ total | `PaymentRefundPostgresTests` |
| Refund idempotency constraint mapping | `PostgresExceptionMapperTests` |
| Domain partial → full | `PaymentAggregateTests` |

## Deferred (6.5+)

- Renter **refund request** + Host/Admin approve workflow
- Real Stripe/Payriff `RefundAsync`
- Notification / Booking reactions on refund outbox events
- HTTP ETag concurrency on Payment updates

## Architect notes

Primary risk — two parallel refunds exceeding payment total — mitigated by:

1. Domain pre-check `RemainingRefundable`
2. Optimistic concurrency (`AggregateVersion`)
3. PostgreSQL `ck_payments_refunded_bounds`
4. Unique refund idempotency index (duplicate key safety)
