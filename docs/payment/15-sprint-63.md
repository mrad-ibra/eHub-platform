# Sprint 6.3 — Payment Provider Integration & Webhook Processing

**Status:** **APPROVED / CLOSED**  
**Date:** 2026-07-20  
**Architect review:** v19 (security) + v20 (maturity) — no critical blockers

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

## Architect review (v19) — residual checks

| Check | Verdict |
|-------|---------|
| `payments.*` permissions | ✅ Fixed (not `bookings.create`) |
| Webhook endpoint + raw body | ✅ |
| Webhook inbox idempotency | ✅ `PaymentWebhookInbox` + `TryBeginAsync` |
| Unknown provider → **404** | ✅ Controller maps `unknown_provider` → `NotFound` |
| Invalid signature → **401** | ✅ Controller maps `invalid_signature` → `Unauthorized` |
| Duplicate / ignored → **200** | ✅ `Ok({ received: true, code })` |
| Stripe/Payriff signature | 🟡 **Skeleton** — `VerifyWebhook` → `false` (401); real HMAC in Sprint 6.4+ / provider sprint |
| `ParseWebhook` must not 500 | ✅ Handler catches provider exceptions → `unparseable` (200) |
| Inbox stuck after crash | 🟡 Non-blocker — status `Received` is reclaimable on retry; reconciliation worker backlog |
| Provider resolver | ✅ `ConcurrentDictionary` lookup (not switch) |
| Outbox → `Booking.Confirm` | ✅ `PaymentOutboxProcessor` |
| Redis | ⏭️ Infra/health only; no app features yet |

Payriff real integration should follow provider docs (e.g. Kapital Bank PG API) in a dedicated adapter sprint — skeleton is routing-only today.

**Architect conclusion (v19):** No critical blockers; architecture near production-ready. Next: real Stripe/Payriff verification, Refund API, notifications.

## Architect review (v20) — maturity

| Area | Rating | Notes |
|------|--------|-------|
| Payment → Webhook → Outbox → Booking | ✅ | L9 respected; late success handled |
| Webhook controller | ✅ | Raw body, headers, 401/404/200 mapping |
| Provider abstraction | ✅ | Correct dependency direction |
| Stripe/Payriff | 🟡 | Skeleton by design — wire before production go-live |
| Outbox consumer retry/DLQ | 🟡 | AttemptCount + log only; backoff/DLQ backlog |
| Outbox batch transactions | 🟡 | 50-msg batch + single SaveChanges; claim model backlog |
| Notification | 🟢 | Separate module; not in this sprint |
| Redis | 🟢 | Correctly deferred |

**Architect conclusion (v20):** Most stable ZIP to date; **production-oriented backend**. Remaining work = real providers, refund API, notifications, ops hardening (retry/DLQ).

## Handoff

→ **Sprint 6.4** — Refund HTTP API + partial/full workflow + late-success auto-refund.

→ **Sprint 6.5 / provider sprint** — Real Stripe + Payriff (Kapital PG) signature/SDK adapters.

→ **Sprint 7.0** — Notification module (email/SMS/push via outbox consumers).

→ **Backlog (ops)** — Outbox exponential backoff, poison/dead-letter, claim-and-complete batching.

→ **Sprint 7+/8** — Redis only when needed (availability cache, distributed lock, multi-instance rate limit).
