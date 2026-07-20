# Sprint 6.2 — APPROVED / CLOSED

**Status:** **APPROVED / CLOSED**  
**Date:** 2026-07-20  

## Closeout fixes (architect)

| Item | Done |
|------|------|
| Permissions `payments.create` / `read` / `cancel` / `refund` | ✅ AuthPolicies + appsettings roles + controller |
| `PaymentProviderCodes.Test` constant | ✅ |
| Remove `AggregateVersion` from Payment DTOs (future: ETag) | ✅ Documented in BR decision log |
| Cancel actors BR-PAY-016 (Renter / Host no / Admin yes) | ✅ |

## Delivered earlier in 6.2

CreatePayment, Cancel, Expire, GetPayment, EF + migration, Outbox on write, API, unit + PG IT.

## Handoff

→ **Sprint 6.3 — Payment Provider Integration & Webhook Processing** (`docs/payment/15-sprint-63.md`)
