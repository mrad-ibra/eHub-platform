# Sprint 6.3 — Payment Provider & Webhook Integration

**Status:** **READY FOR ARCHITECT REVIEW**  
**Date:** 2026-07-20  

## Scope delivered

| Area | Implementation |
|------|----------------|
| Provider abstraction | `IPaymentProvider`, `IPaymentProviderResolver`, normalized DTOs |
| Fake provider | `FakePaymentProvider` — HMAC webhook, no external SDK |
| Create payment → provider | `CreatePaymentCommandHandler` calls adapter → `MarkPending` |
| Webhook endpoint | `POST /api/v1/payments/webhooks/{provider}` (signature auth) |
| Webhook inbox | `payment_webhook_inbox` + unique `(Provider, ProviderEventId)` |
| Process webhook | `ProcessWebhookCommand` — verify → idempotency → state transition → outbox |
| Outbox consumer | `PaymentOutboxProcessor` → `Booking.Confirm` (L9) |
| Migration | `AddPaymentWebhookInbox` |

## Exit criteria

| Criterion | Status |
|-----------|--------|
| Provider abstraction ready | ✅ |
| Fake provider works | ✅ |
| Webhook endpoint exists | ✅ |
| Signature verification | ✅ |
| DB-level webhook idempotency | ✅ |
| Payment state changes via webhook | ✅ |
| Outbox written on success | ✅ |
| Duplicate webhook tests | ✅ unit + PG IT |
| Invalid signature tests | ✅ unit |
| CI green (unit) | ✅ 144 passed |
| PostgreSQL IT | ✅ skippable when Docker absent; runs in CI with `EHUB_REQUIRE_POSTGRES_TESTS=true` |

## Test coverage (architect checklist)

| Scenario | Test |
|----------|------|
| Create payment through fake provider | `CreatePaymentCommandHandlerTests` |
| Valid success webhook updates payment | `ProcessWebhookCommandHandlerTests`, `PaymentWebhookPostgresTests` |
| Duplicate webhook processed once | unit + PG IT |
| Invalid signature rejected | unit + `FakePaymentProviderTests` |
| Expired timestamp rejected | unit |
| Unknown provider rejected | unit |
| Unknown event type acknowledged safely | unit |
| Late success does not confirm expired booking | PG IT |
| Failed webhook marks payment failed | unit + PG IT |
| Outbox message created once | PG IT |
| Parallel duplicate webhooks safe | inbox unique index + duplicate path |

## Architecture notes

- **L8:** No Stripe/Iyzico types in Domain/Application.
- **L2/L6:** Signature + timestamp before any effect; inbox dedupe on `(Provider, EventId)`.
- **L9:** Payment handlers never call `Booking.Confirm`; outbox consumer only.
- **Incomplete inbox row (`Received`):** Retries reclaim and reprocess (crash-safe).
- **Late success (L4):** Consumer catches `ConflictException`; booking stays `Expired`.

## Handoff

→ **Sprint 6.4** — Refund commands/API + richer outbox paths (partial refund, auto-refund on late success).
