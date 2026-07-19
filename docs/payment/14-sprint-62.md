# Sprint 6.2 — Payment Application + Persistence

**Status:** READY FOR ARCHITECT REVIEW  
**Depends on:** Sprint 6.1 Phase 1 Domain — **APPROVED**

## Exit criteria (checklist)

| Item | Status |
|------|--------|
| `CreatePaymentCommand` + Handler + Validator | ✅ |
| `CancelPaymentCommand` / `ExpirePaymentCommand` | ✅ |
| `GetPaymentQuery` | ✅ |
| `IPaymentRepository` + `EfPaymentRepository` + InMemory | ✅ |
| EF configurations + migration `AddPaymentPersistence` | ✅ |
| Outbox enqueue on create/cancel/expire | ✅ |
| `POST/GET /api/v1/payments` (+ cancel) | ✅ |
| Unit tests (domain + handlers) | ✅ |
| PostgreSQL IT (Testcontainers, skip without Docker) | ✅ |

## Explicit non-goals (Sprint 6.3+)

- Provider ACL / Stripe SDK  
- Webhook endpoint + signature + inbox  
- RetryPayment (needs provider)  
- Outbox consumer → `Booking.Confirm`  
- Refund HTTP API  

## Note on prior review ZIP

Current tree already has:

- `ApplyTransition` / `MarkChanged` for AggregateVersion (not `Raise`)  
- `AssetRentalRules.PreparationBufferDays` with CreateBooking fallback to default  

If a review ZIP still shows manual `AggregateVersion++` / hardcoded buffer only, it is stale relative to this branch.

## Next

After **APPROVED** → **Sprint 6.3** Provider adapter + Webhook + signature + inbox idempotency.
