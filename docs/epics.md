# eHub — Master Epic Roadmap

High-level grouping for GitHub Milestones, release notes, and stakeholder communication.

**Last updated:** 2026-07-20

---

## Status summary

| Epic | Name | Status |
|------|------|--------|
| ✅ 1 | Booking Platform | **100%** |
| 🟡 2 | Payment Platform | **~95%** — Sprint 6.5 qalıb (Phase 0 ✅) |
| ⏳ 3 | Communication Platform | Başlanmayıb |
| ⏳ 4 | Marketplace | Başlanmayıb |
| ⏳ 5 | Platform Engineering | Başlanmayıb |
| ⏳ 6 | Admin Platform | Başlanmayıb |
| ⏳ 7 | Production | Başlanmayıb |

### Recommended path (real SaaS / marketplace)

Portfolio üçün Epic 5–7-nin hamısı məcburi deyil. Real istifadəçi trafiki üçün prioritet:

1. **Sprint 6.5** — Real Stripe / Payriff
2. **Sprint 7.0** — Notification
3. **Sprint 7.1** — Cancellation policy + automatic refund
4. **Sprint 7.2** — Marketplace settlement (host payout, platform commission)

Bu dörd sprint tamamlandıqdan sonra platform real istifadəçilər üçün işlək səviyyəyə yaxınlaşır.

---

## Epic 1 — Booking Platform ✅

**Status:** Complete  
**Sprints:** 5.x

| Delivered | |
|-----------|---|
| Booking Aggregate | ✅ |
| Availability | ✅ |
| Timeline / Status History | ✅ |
| Expire Worker | ✅ |
| CQRS | ✅ |
| PostgreSQL + Outbox | ✅ |
| Metrics | ✅ |

→ [booking/](booking/README.md)

---

## Epic 2 — Payment Platform 🟡

**Status:** ~95% — Sprint 6.5 qalıb  
**Sprints:** 6.0 – 6.5

| Sprint | Scope | Status |
|--------|-------|--------|
| 6.1 | Payment Aggregate, Money VO, Timeline, PaymentAttempt, Events | ✅ |
| 6.2 | Payment API, Commands, Queries, EF, Migration, IT | ✅ |
| 6.3 | Provider abstraction, Webhook, Inbox, Idempotency, Outbox → Booking.Confirm | ✅ |
| 6.4 | Refund Workflow — API, partial/full, validation, audit, idempotency, events | ✅ |
| **6.5** | **Real payment providers** | **IN PROGRESS** |

### Sprint 6.5 phases

| Phase | Scope | Status |
|-------|-------|--------|
| **0** | `PaymentFailureReason`, `ProviderFailure`, Contract Tests, Architecture Tests | ✅ |
| **A** | Stripe SDK — Create, Cancel, Refund, VerifyWebhook, ParseWebhook, Sandbox | ☐ |
| **B** | Payriff REST adapter — Refund, Cancel, VerifyWebhook, Sandbox | ☐ |
| **C** | Fail-fast config, failure mapping, replay/parallel webhook tests, production docs | ☐ |

→ [payment/](payment/README.md)  
→ [17-sprint-65.md](payment/17-sprint-65.md)

---

## Epic 3 — Communication Platform ⏳

**Sprints:** 7.0 – 7.3

### Sprint 7.0 — Notification Module

| Layer | Scope |
|-------|-------|
| Domain | `Notification`, `NotificationStatus` |
| Application | `SendEmailCommand`, notification events |
| Infrastructure | SMTP, mail templates, outbox consumer |

**Event → notification triggers (already in outbox):**

```text
BookingConfirmed   → Email
PaymentSucceeded   → Email
RefundSucceeded    → Email
BookingCancelled   → Email
```

SMS / Push — same outbox pattern, later in 7.x.

### Sprint 7.1 — Cancellation Policies

| Rule | Refund |
|------|--------|
| Host cancel | 100% |
| 7+ days before | 100% |
| 3–7 days | 50% |
| &lt;3 days | 0% |

Automatic refund + penalty rules + policy engine.

### Sprint 7.2 — Marketplace Settlement

Host balance, commission, platform fee, payout, settlement ledger, accounting events.

### Sprint 7.3 — Dispute Module

Dispute, evidence, resolution, manual refund, admin decision, chargeback.

---

## Epic 4 — Marketplace ⏳

**Sprints:** 8.0 – 8.2

| Sprint | Scope |
|--------|-------|
| 8.0 | Catalog — category, search, filtering, sorting, availability search, price range |
| 8.1 | Review system — rating, host/guest rating, moderation |
| 8.2 | Wishlist — favorites, saved search, recommendation |

---

## Epic 5 — Platform Engineering ⏳

**Sprints:** 9.0 – 9.5  
**Trigger:** konkret ehtiyac (scale, cache, ops maturity) — portfolio üçün hamısı məcburi deyil.

| Sprint | Scope |
|--------|-------|
| 9.0 | Redis — availability cache, distributed lock, rate limiter storage |
| 9.1 | Search engine — Elasticsearch/OpenSearch, full text, geo, autocomplete |
| 9.2 | Performance — caching, indexes, query optimization, load testing |
| 9.3 | Operations — DLQ, retry, backoff, poison messages, claim batching |
| 9.4 | Observability — Grafana, Prometheus, dashboards, alerts, business metrics |
| 9.5 | Security — secrets rotation, API keys, provider key rotation, pen test |

Redis **not** required for Epic 3 notifications initially — PostgreSQL outbox + HostedService suffices.

---

## Epic 6 — Admin Platform ⏳

**Sprints:** 10.0 – 10.2

| Sprint | Scope |
|--------|-------|
| 10.0 | Admin dashboard — bookings, payments, refunds, users, search |
| 10.1 | Reports — revenue, refund, occupancy, provider health |
| 10.2 | Admin actions — manual refund, force cancel, disable user, resend notification |

---

## Epic 7 — Production ⏳

**Sprints:** 11.0 – 11.2

| Sprint | Scope |
|--------|-------|
| 11.0 | CI/CD — blue/green, Docker, Kubernetes, secrets |
| 11.1 | Horizontal scaling — multiple API/workers, Redis locks |
| 11.2 | Production readiness — DR, backup/restore, chaos testing, game day |

---

## Maturity arc

```text
Early ZIPs     →  CQRS, Booking, Aggregate
Sprint 6.x     →  ACL, Provider abstraction, Idempotency, Audit, Refund
Sprint 6.5     →  Contract testing, Real providers, Sandbox certification
Sprint 7.x     →  Communication, Policies, Settlement (business-ready)
Epic 5–7       →  Scale, Ops, Admin, Production hardening
```

---

## GitHub Milestones (suggested)

| Milestone | Epics / Sprints |
|-----------|-----------------|
| `booking-platform` | Epic 1 ✅ |
| `payment-platform` | Epic 2 — close on 6.5 Phase C |
| `communication-platform` | Epic 3 — 7.0–7.3 |
| `marketplace` | Epic 4 |
| `platform-engineering` | Epic 5 |
| `admin-platform` | Epic 6 |
| `production` | Epic 7 |
