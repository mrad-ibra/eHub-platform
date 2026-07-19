# EPIC 6 — Payment

**Sprint 6.0:** Payment Architecture Pack — **DRAFT — awaiting Architect review**

Design only. No code, no migrations, no provider integration this sprint. This pack defines the aggregate, state machine, provider anti-corruption boundary, webhook + idempotency strategy, refund model, and the failure/acceptance matrix that Sprint 6.1 will implement against.

> **Sprint 6.1 implementation starts only after Architect sign-off is APPROVED on this pack.** Until then every document below is a proposal, not a contract.

## Documents

| # | Document | Purpose | Status |
|---|----------|---------|--------|
| — | [README.md](README.md) | Index + locked principles | DRAFT |
| 00 | [00-business-rules.md](00-business-rules.md) | Payment business rules (BR-PAY-*) | DRAFT |
| 01 | [01-aggregate-design.md](01-aggregate-design.md) | Payment aggregate boundary + model | DRAFT |
| 02 | [02-state-machine.md](02-state-machine.md) | Statuses + transitions | DRAFT |
| 03 | [03-payment-lifecycle.md](03-payment-lifecycle.md) | End-to-end lifecycle | DRAFT |
| 04 | [04-provider-strategy.md](04-provider-strategy.md) | Provider strategy + anti-corruption adapter | DRAFT |
| 05 | [05-webhook-strategy.md](05-webhook-strategy.md) | Signature verify + inbox dedupe | DRAFT |
| 06 | [06-idempotency.md](06-idempotency.md) | Idempotency keys + exactly-once effects | DRAFT |
| 07 | [07-refund-strategy.md](07-refund-strategy.md) | Refunds as separate audited operation | DRAFT |
| 08 | [08-database-design.md](08-database-design.md) | Logical ER + tables + indexes | DRAFT |
| 09 | [09-api-contract.md](09-api-contract.md) | REST + webhook contract | DRAFT |
| 10 | [10-failure-scenarios.md](10-failure-scenarios.md) | Failure matrix (F-PAY-*) | DRAFT |
| 11 | [11-acceptance-edge-tests.md](11-acceptance-edge-tests.md) | AC / edges / test scenarios | DRAFT |

## Locked principles (must hold across the whole pack)

| # | Principle |
|---|-----------|
| L1 | **Amount is authoritative from Booking.** Payment amount is taken from the Booking `TotalPrice` snapshot — the client-supplied price is **never** trusted. |
| L2 | **Webhook exactly-once.** The same provider webhook is processed **once**; replays are deduplicated (idempotent). |
| L3 | **Confirm after Succeeded.** A Booking becomes `Confirmed` **only after** Payment reaches `Succeeded`. |
| L4 | **No late confirm.** A late payment callback **must NOT** confirm an `Expired` (or otherwise terminal) Booking. |
| L5 | **Refund is separate.** Refund is a distinct operation with its own audit history — never an in-place status edit. |
| L6 | **Signatures verified.** Every provider webhook signature **MUST** be verified before any effect. |
| L7 | **Separate aggregates.** Payment and Booking are **separate aggregates**, linked only by id (no object references, no shared transaction on domain state). |
| L8 | **Anti-corruption.** Provider responses are **not** bound directly into the domain model — they cross an adapter / anti-corruption layer. |
| L9 | **Outbox to Booking.** Payment outcomes reach Booking **via the Outbox** (async, idempotent consumer) — never a direct in-process aggregate call. |

## Initial model (proposed)

```text
Payment: BookingId, Amount, Currency, Provider, ProviderPaymentId,
         Status, IdempotencyKey, FailureReason, PaidAtUtc, RefundedAmount
Statuses: Created, Pending, Authorized, Succeeded, Failed,
          Cancelled, PartiallyRefunded, Refunded, Expired
```

## Relationship to Booking (EPIC 5)

- Booking owns the **calendar + hold + price freeze**; Payment owns the **money**.
- Booking `PendingPayment` (Hard Hold, 15m timer from Approve) is the **only** state from which a payment may succeed into `Confirmed`.
- Payment TTL/timeout aligns with the Booking Payment TTL — see [00-business-rules.md](00-business-rules.md) BR-PAY-004.
- Cross-aggregate coupling is via events only: `BookingApproved` → create Payment; `PaymentSucceeded` → `BookingConfirmed`.

## Design-first checklist (this pack)

Business Rules → Aggregate → State Machine → Lifecycle → Provider/ACL → Webhook → Idempotency → Refund → ER → API → Failures → AC/Edges/Tests → (6.1) code

## Related

- Booking pack: [../booking/README.md](../booking/README.md)
- [ADR 0006 — Domain Primitives](../adr/0006-domain-primitives.md)
