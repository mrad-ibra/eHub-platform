# Sprint 6.3 — Payment Provider Integration & Webhook Processing

**Status:** **READY FOR ARCHITECT REVIEW**  
**Date:** 2026-07-20  

## Sprint goal

Provider-independent payment integration and webhook processing — **without** real Stripe/Payriff SDK wiring yet. Redis is **out of scope** for this sprint (deferred to Sprint 7/8 when availability cache, distributed lock, multi-instance rate limiting, or notification queue need it).

## Pre-6.3 security hardening (architect review)

| Item | Status |
|------|--------|
| `payments.*` permissions (not `bookings.create`) | ✅ |
| Idempotency replay checks BookingId + Provider + renter (UserId) | ✅ |
| `ux_payments_one_active_per_booking` → HTTP 409 | ✅ |
| `IX_payments_IdempotencyKey` unique → HTTP 409 | ✅ |
| Provider required in API; no silent `TEST` fallback | ✅ |
| `AllowTestProvider` false in production config | ✅ |
| Production fail-fast without EF / NullOutboxWriter | ✅ |
| Webhook inbox + signature (Sprint 6.3 scope) | ✅ |

## Scope delivered

| Area | Implementation |
|------|----------------|
| Provider abstraction | `IPaymentProvider`, `IPaymentProviderResolver`, normalized DTOs |
| Fake provider (full) | `FakePaymentProvider` — create/cancel/refund + HMAC webhook |
| Stripe skeleton | `StripePaymentProvider` — registered, routes webhooks, SDK pending |
| Payriff skeleton | `PayriffPaymentProvider` — registered, routes webhooks, SDK pending |
| Create payment → provider | `CreatePaymentCommandHandler` → adapter → `MarkPending` |
| Webhook endpoint | `POST /api/v1/payments/webhooks/{provider}` (signature auth) |
| Signature verification | Fake: HMAC + timestamp skew; skeletons: reject until wired |
| Webhook inbox | `payment_webhook_inbox` + unique `(Provider, ProviderEventId)` |
| Process webhook | `ProcessWebhookCommand` — verify → idempotency → transition → outbox |
| Outbox integration | `PaymentSucceeded` enqueued on capture |
| Payment → Booking flow | `PaymentOutboxProcessor` → `Booking.Confirm` (L9) |
| Migration | `AddPaymentWebhookInbox` |

## Provider routing

| Code | Adapter | Runtime |
|------|---------|---------|
| `TEST` | `FakePaymentProvider` | Fully functional (dev/IT) |
| `STRIPE` | `StripePaymentProvider` | Skeleton — create/refund throws; webhook 401 |
| `PAYRIFF` | `PayriffPaymentProvider` | Skeleton — create/refund throws; webhook 401 |

`PaymentProviderCode` (Domain) + `PaymentProviderCodes` (Application) keep provider identity typed and SDK-free (L8).

## Exit criteria

| Criterion | Status |
|-----------|--------|
| Provider abstraction ready | ✅ |
| Fake provider works end-to-end | ✅ |
| Stripe/Payriff skeleton registered | ✅ |
| Webhook endpoint exists | ✅ |
| Signature verification (Fake) | ✅ |
| DB-level webhook idempotency | ✅ |
| Payment state changes via webhook | ✅ |
| Outbox → Booking.Confirm | ✅ |
| Duplicate / invalid signature tests | ✅ |
| CI green (unit) | ✅ |
| PostgreSQL IT | ✅ (Docker / CI) |
| Redis | ⏭️ Not in this sprint |

## Test coverage

| Scenario | Test |
|----------|------|
| Resolver finds TEST / STRIPE / PAYRIFF | `PaymentProviderResolverTests` |
| Stripe skeleton not wired | `PaymentProviderSkeletonTests` |
| Create payment through fake provider | `CreatePaymentCommandHandlerTests` |
| Valid success webhook | unit + PG IT |
| Duplicate webhook | unit + PG IT |
| Invalid signature / expired timestamp | unit |
| Unknown provider / unknown event | unit |
| Late success → no Confirm | PG IT |
| Failed webhook | unit + PG IT |

## Architecture notes

- **L8:** No Stripe/Payriff SDK types in Domain/Application; skeletons live in Infrastructure only.
- **L2/L6:** Signature + timestamp before any effect; inbox dedupe on `(Provider, EventId)`.
- **L9:** Payment handlers never call `Booking.Confirm`; outbox consumer only.
- **Incomplete inbox (`Received`):** Retries reclaim and reprocess.
- **Late success (L4):** Consumer catches `ConflictException`; booking stays terminal.

## Handoff

→ **Sprint 6.4** — Refund API + partial/full refund + late-success auto-refund path.

→ **Sprint 7+** — Redis when concrete need (cache, lock, rate limit, notification queue).
