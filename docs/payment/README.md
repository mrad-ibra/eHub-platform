# EPIC 6 — Payment

**Sprint 6.0:** Payment Architecture Pack — **READY FOR ARCHITECT REVIEW**  
**Implementation:** Sprint 6.1 starts **only after** this pack is **APPROVED**.

Design only. No code · no migrations · no provider SDK wiring in 6.0.

## Documents

| # | Document | Purpose |
|---|----------|---------|
| — | [README.md](README.md) | Index + locked principles + review checklist |
| 00 | [00-business-rules.md](00-business-rules.md) | BR-PAY-* |
| 01 | [01-aggregate-design.md](01-aggregate-design.md) | Aggregate boundary + model |
| 02 | [02-state-machine.md](02-state-machine.md) | Statuses + transitions |
| 03 | [03-payment-lifecycle.md](03-payment-lifecycle.md) | End-to-end flows |
| 04 | [04-provider-strategy.md](04-provider-strategy.md) | Provider ACL |
| 05 | [05-webhook-strategy.md](05-webhook-strategy.md) | Signature + inbox |
| 06 | [06-idempotency.md](06-idempotency.md) | Exactly-once effect |
| 07 | [07-refund-strategy.md](07-refund-strategy.md) | Partial / full refund |
| 08 | [08-database-design.md](08-database-design.md) | Logical ER |
| 09 | [09-api-contract.md](09-api-contract.md) | REST + webhook |
| 10 | [10-failure-scenarios.md](10-failure-scenarios.md) | F-PAY-* |
| 11 | [11-acceptance-edge-tests.md](11-acceptance-edge-tests.md) | AC / edges / tests |

## Locked principles (non-negotiable for 6.1)

| # | Principle |
|---|-----------|
| L1 | Amount from **Booking `TotalPrice` snapshot** — client price never trusted |
| L2 | Same webhook processed **once** (inbox unique key) |
| L3 | Booking `Confirmed` **only after** Payment `Succeeded` |
| L4 | Late webhook **must not** confirm an `Expired`/terminal Booking |
| L5 | Refund = **separate audited operation** (partial and full have distinct rules) |
| L6 | Webhook **signature validation mandatory** before any effect |
| L7 | Payment and Booking = **separate aggregates** (id-only) |
| L8 | Provider-specific models **never** enter Domain (adapter / ACL) |
| L9 | Payment → Booking effects only via **Outbox / events** |
| L10 | Failed payment must **not** leave Booking stuck forever — TTL + optional retry while hold active |

## Proposed model

```text
Payment
├── BookingId
├── Amount / Currency          ← from Booking snapshot
├── Provider / ProviderPaymentId?
├── Status
├── IdempotencyKey
├── FailureReason?
├── PaidAtUtc?
├── RefundedAmount
├── Version
├── StatusHistory[]
├── Attempts[]
└── Refunds[]                  ← separate audited rows

Statuses:
  Created | Pending | Authorized | Succeeded | Failed
  | Cancelled | Expired | PartiallyRefunded | Refunded
```

## Architect review checklist

- [ ] L1–L10 locked
- [ ] State machine + late-callback / Failed→TTL rules approved
- [ ] Partial vs full refund rules approved
- [ ] Webhook verify → dedupe → apply order approved
- [ ] Outbox boundary Booking↔Payment approved
- [ ] Acceptance scenarios in [11](11-acceptance-edge-tests.md) sufficient for 6.1

## Relationship to Booking (EPIC 5)

| Booking | Payment |
|---------|---------|
| Owns calendar, Soft/Hard Hold, price freeze | Owns money movement |
| `PendingPayment` + 15m TTL | Must succeed inside that window |
| Confirms only on `PaymentSucceeded` (hold active) | Emits events; never calls Booking directly |
| Expire worker releases hold | Failed/Expired payments; retry only while Booking still payable |

## Design-first order

Business Rules → Aggregate → State Machine → Lifecycle → Provider → Webhook → Idempotency → Refund → ER → API → Failures → AC/Tests → **(6.1) code**

## Related

- [../booking/README.md](../booking/README.md) — Booking Core COMPLETED  
- [../observability.md](../observability.md)  
- [ADR 0006](../adr/0006-domain-primitives.md)
