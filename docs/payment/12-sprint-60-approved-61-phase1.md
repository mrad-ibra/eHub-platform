# Sprint 6.0 — Final disposition (Payment Architecture Pack)

**Status:** Sprint 6.0 — **APPROVED**  
**Date:** 2026-07-20  

Architect approved the Payment Architecture Pack. Implementation may proceed as **Sprint 6.1**.

## Locked principles (L1–L10)

Unchanged — see [README.md](README.md). Non-negotiable for 6.1+.

## Architect recommendations (non-blocking — apply in 6.1)

1. **Provider ACL** — keep adapter surface small: `CreatePayment` / `CancelPayment` / `RefundPayment` / `VerifyWebhook`. Domain stays provider-agnostic (opaque `PaymentProviderCode` only).
2. **Money VO** — use existing `Money` (`Amount` + `CurrencyId`); no raw `decimal` APIs on Payment.
3. **PaymentTimeline** — mirror Booking timeline for audit/debug (`Created`, `Pending`, `Paid`, `Failed`, `Refunded`, …).
4. **Outbox** — `PaymentSucceeded` must not call Booking directly: Domain Event → Outbox → Consumer → `Booking.Confirm`.

## Sprint 6.1 plan

| Phase | Scope |
|-------|--------|
| **1** | Payment Aggregate, VOs, Status, Timeline, Domain Events |
| **2** | CreatePaymentCommand, Repository, EF Mapping, Migration |
| **3** | Webhook endpoint, Provider abstraction, Idempotency |
| **4** | Refund wiring, Integration + PostgreSQL tests |

## Phase 1 delivered (this commit set)

- `eHub.Domain/Payments/*` — aggregate + children + events  
- Unit tests: `tests/eHub.UnitTests/Domain/Payments/PaymentAggregateTests.cs`  
- Localization keys for Payment errors  

## Explicit non-goals (later phases)

- EF / migrations  
- Provider SDK adapters  
- Webhook HTTP endpoint  
- Outbox consumer → Booking.Confirm  
