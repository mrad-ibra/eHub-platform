# Sprint 6.5 — Real Payment Provider Integration

**Status:** **IN PROGRESS** — Phase 0 **CLOSED** · Phase A **IN PROGRESS** (Stripe Checkout Session ACL)  
**Date:** 2026-07-20  
**Epic:** Payment Platform (Epic 2 — final sprint)

## Sprint goal

Move from Fake provider to **production-grade Stripe and Payriff adapters** — without changing Domain, Application, CQRS, or aggregate behavior. Only the Infrastructure ACL layer is wired.

```text
Domain / Application     →  unchanged
Infrastructure           →  ACL adapters
    ├── StripePaymentProvider   (Stripe.net)
    └── PayriffPaymentProvider  (REST / SDK)
```

This sprint answers **“how do we complete this at production level?”** — not **“what do we build?”**

## Architect review input (9.8/10 plan)

Three additions required for a **10/10 production-ready sprint**:

| # | Addition | Phase |
|---|----------|-------|
| 1 | `PaymentProviderContractTests` — shared `IPaymentProvider` certification | **Phase 0** (before Stripe) |
| 2 | Normalized `PaymentFailureReason` — no provider strings past Infrastructure | **Phase 0** types ✅ — Phase A/B mappers |
| 3 | Architecture CI test — no `Stripe.*` / `Payriff.*` in Domain or Application | **Phase 0** ✅ |

---

## Phase 0 — COMPLETE (2026-07-20)

| Deliverable | Location | Status |
|-------------|----------|--------|
| `PaymentFailureReason` enum | `Application/Payments/PaymentFailureReason.cs` | ✅ |
| `ProviderFailure` + normalized result records | `Application/Payments/Abstractions/` | ✅ |
| `PaymentProviderContractTests` + `FakePaymentProviderContractTests` | `tests/.../Contracts/` | ✅ |
| `PaymentProviderArchitectureTests` (NetArchTest.Rules) | `tests/.../Architecture/` | ✅ |
| `PaymentFailureReasonMapper` placeholder | `Infrastructure/Payments/` | ✅ (Phase A/B wiring) |
| Handlers use normalized failures (no raw provider strings) | CreatePayment / CreateRefund | ✅ |

| `PaymentFailureCodes` stable domain codes | `Application/Payments/PaymentFailureCodes.cs` | ✅ |
| Provider-independent contract hooks (webhook signing) | `PaymentProviderContractTests` abstract hooks | ✅ |
| Idempotency payload mismatch (create/refund) | `FakePaymentProvider` + contract tests | ✅ |
| Webhook `PaymentFailureReason?` (no raw strings) | `ProviderWebhookEvent` + mapper | ✅ |
| Enabled-only provider DI + production fail-fast | `PaymentProviderRegistration` | ✅ |
| Inbox + payment single transaction + lease reclaim | `EfPaymentWebhookInboxStore` + handler | ✅ |
| Factory-only provider result types | `ProviderOperationResults.cs` | ✅ |

### Phase 0 exit criteria

| Criterion | Status |
|-----------|--------|
| `PaymentFailureReason` provider-independent | ✅ |
| `ProviderFailure` model exists | ✅ |
| Fake provider passes contract tests (10 scenarios) | ✅ |
| Architecture tests in CI (+ csproj package guard) | ✅ |
| Domain/Application free of provider SDK deps | ✅ |
| Raw provider strings do not reach Application/Domain | ✅ |
| Unit tests green (186) | ✅ |

**Next:** Phase A — Stripe Checkout Session adapter (behavior tests green; sandbox opt-in remaining).

### Phase A progress (2026-07-20)

| Item | Status |
|------|--------|
| Folder layout `Providers/Stripe|Fake|Payriff` + `Common/` | ✅ |
| `Stripe.net` (Infrastructure only) | ✅ |
| Checkout Session create + RedirectUrl | ✅ |
| Idempotency key → Stripe `RequestOptions` | ✅ |
| Refund via PaymentIntent + cancel/expire session | ✅ |
| `StripeWebhookVerifier` + `StripeWebhookParser` | ✅ |
| `CurrencyCode` on provider requests + `IMinorUnitConverter` (AZN/USD/EUR) | ✅ |
| Enabled-only DI; Fake off in Production; Enabled→secrets fail-fast | ✅ |
| Mocked Stripe behavior tests | ✅ |
| Opt-in Stripe sandbox IT | ☐ |
| Polly retry/CB | ☐ Phase C |

---

## Phases (remaining)

Contract certification baseline is complete — **Fake** passes `PaymentProviderContractTests`. Stripe and Payriff must pass the same suite in Phase A/B.

---

### Phase A — Stripe

| Item | Detail |
|------|--------|
| Package | `Stripe.net` in `eHub.Infrastructure` only |
| Config | `Payments:Providers:Stripe` — `Enabled`, `ApiKey`, `WebhookSecret` |
| `CreatePaymentAsync` | PaymentIntent / Checkout Session (sandbox) |
| `RefundAsync` | Stripe Refund API |
| `CancelPaymentAsync` | Cancel / void as appropriate |
| `VerifyWebhook` | `Stripe-Signature` header + `EventUtility.ConstructEvent` |
| `ParseWebhook` | `payment_intent.succeeded`, `payment_intent.payment_failed`, `charge.refunded`, … |
| Tests | Contract tests + Stripe sandbox IT (opt-in CI secret) |

---

### Phase B — Payriff

| Item | Detail |
|------|--------|
| Client | REST ACL (or official SDK if available) — Infrastructure only |
| Config | `Payments:Providers:Payriff` — `Enabled`, `MerchantId`, `SecretKey`, `WebhookSecret` |
| Operations | Create, Refund, Cancel — same `IPaymentProvider` surface |
| Signature | Per Payriff / Kapital PG docs |
| Tests | Same contract suite + Payriff sandbox IT |

---

### Phase C — Cross-cutting production hardening

#### Failure normalization (Phase A/B — mapper wiring)

Types and result models exist (Phase 0). Provider-specific code → `PaymentFailureReason` mapping lands in `PaymentFailureReasonMapper` during Stripe/Payriff implementation.

#### Config validation (environment-aware fail-fast)

| Environment | Rule |
|-------------|------|
| **Production** | `Stripe.Enabled = true` and empty `ApiKey` or `WebhookSecret` → **startup FAIL** |
| **Production** | Same for Payriff when `Enabled = true` |
| **Development** | `Enabled = false` + empty secrets → **OK** (Fake provider used) |
| **Development** | `Enabled = true` + missing secrets → warn or fail (team choice; document in ops) |

`AllowTestProvider = false` in production (already enforced from 6.3).

#### Webhook replay & concurrency tests

| Scenario | Expected |
|----------|----------|
| Same webhook delivered **100×** | **1** payment state transition; 99× inbox dedupe / no-op |
| **50 parallel** identical webhooks | **1** state transition; no duplicate outbox / no double `Booking.Confirm` |
| Invalid signature | **401**; zero DB side effects |
| Provider timeout on create/refund | Normalized `ProviderUnavailable`; payment/refund in recoverable failed state |
| Retry after transient provider error | Idempotent replay succeeds |

#### Architecture governance (CI) ✅ Phase 0

`PaymentProviderArchitectureTests` runs on every CI build (NetArchTest.Rules).

---

## Scope

### In scope

- Phase 0 contract tests
- Stripe SDK integration (Phase A)
- Payriff adapter (Phase B)
- Real webhook signature verification (both)
- `CreatePaymentAsync`, `RefundAsync`, `CancelPaymentAsync`
- Normalized `PaymentFailureReason`
- Production config fail-fast
- Webhook replay / parallel IT
- Architecture CI test
- Sandbox E2E (Stripe + Payriff)
- `docs/payment/17-sprint-65.md` + README updates

### Out of scope (later sprints)

| Item | Sprint |
|------|--------|
| Refund `Pending` + async provider callback | 6.5 tail or 7.x |
| Notification reactions on refund/payment events | 7.0 |
| Redis (cache, lock, rate limit) | Epic 4 |
| DLQ / exponential backoff / poison messages | Epic 4 |
| Renter refund request workflow | 7.x+ |
| `RefundReason` enum | Optional pre-6.5 or parallel |

---

## Exit criteria

### Functional

| Criterion | Status |
|-----------|--------|
| Stripe Create → Pending | ☐ |
| Stripe Refund (partial + full) | ☐ |
| Stripe Cancel | ☐ |
| Stripe Webhook → `PaymentSucceeded` → Booking confirm (sandbox E2E) | ☐ |
| Payriff Create + webhook (minimum happy path) | ☐ |
| Payriff Refund | ☐ |
| Fake passes **contract tests** | ✅ |

### Reliability

| Criterion | Status |
|-----------|--------|
| Duplicate webhook (100×) → single transition | ☐ |
| Parallel webhook (50×) → single transition | ☐ |
| Invalid signature → 401, no side effects | ☐ |
| Provider timeout → normalized failure | ☐ |
| Idempotent create/refund retry | ☐ |

### Architecture

| Criterion | Status |
|-----------|--------|
| `PaymentProviderContractTests` green for Fake | ✅ |
| Fake + Stripe + Payriff pass contract tests | ☐ |
| ACL isolation — no provider types in Domain/Application | ✅ |
| `PaymentFailureReason` result models (no raw strings in handlers) | ✅ |
| Architecture test in CI | ✅ |
| Unit + PG IT + sandbox (opt-in) green | ☐ |

---

## Test matrix

| Test class | Type | Provider |
|------------|------|----------|
| `PaymentProviderContractTests` | Shared contract | Fake, Stripe, Payriff |
| `StripePaymentProviderTests` | Unit + sandbox | Stripe |
| `PayriffPaymentProviderTests` | Unit + sandbox | Payriff |
| `PaymentProviderArchitectureTests` | Architecture | All assemblies |
| `PaymentWebhookReplayPostgresTests` | PG IT | Fake (deterministic) + Stripe (sandbox) |
| `PaymentWebhookParallelPostgresTests` | PG IT | Fake + Stripe |
| `StripeSandboxEndToEndTests` | E2E (opt-in) | Stripe test mode |

Existing coverage from 6.3–6.4 remains; this sprint **extends** — does not replace.

---

## Dependencies

- Sprint 6.4 **APPROVED** (Refund workflow + `RefundAsync` call path)
- Stripe test account + webhook signing secret (CI secrets / Vault)
- Payriff sandbox credentials + signature documentation

---

## Handoff

→ **Epic 3 — Communication Platform** — Sprint 7.0 (email / SMS / push, notification outbox consumer, templates)

→ **Epic 4 — Platform Engineering** — Redis, search, metrics, DLQ, horizontal scaling (when concrete need arises)
