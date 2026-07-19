# Sprint 6.1 Phase 2 — Application + Persistence

**Status:** IN PROGRESS / ready for review  
**Depends on:** Sprint 6.1 Phase 1 — **APPROVED**

## Delivered

| Item | Location |
|------|----------|
| `IPaymentRepository` | `eHub.Application/Payments/Abstractions` |
| `CreatePaymentCommand` + Handler + Validator | `.../Commands/CreatePayment` |
| EF `PaymentConfiguration` + children | `eHub.Persistence/Configurations` |
| `EfPaymentRepository` / `InMemoryPaymentRepository` | Persistence / Infrastructure |
| Migration `AddPaymentPersistence` | `payments`, timeline, history, attempts, refunds |
| Constraints | `ck_payments_amount_positive`, `ck_payments_refunded_bounds`, `ux_payments_one_active_per_booking` |
| Outbox on create | `PaymentCreated` enqueued same UoW |
| Unit tests | `CreatePaymentCommandHandlerTests` |
| PG integration tests | `PaymentPostgresPersistenceTests` (Testcontainers) |

## CreatePayment rules (L1 / L7 / L9)

1. Amount = `Booking.TotalPrice` snapshot (never from client)
2. Booking must be `PendingPayment` + hold active; caller = renter
3. ≤1 active payment per booking (app check + partial unique index)
4. Idempotency via unique `IdempotencyKey` (replay returns existing)
5. Domain events → Outbox → (Phase 3+) consumers; never call Booking directly

## Explicit non-goals (Phase 3+)

- Provider ACL / SDK
- Webhook endpoint + inbox
- PaymentInbox table
- Outbox consumer → `Booking.Confirm`
- HTTP API controller (optional thin wrap later)
