# eHub — Epic roadmap

High-level grouping for GitHub Milestones, release notes, and stakeholder communication.

## Epic 1 — Booking Platform ✅

**Status:** Complete  
**Sprints:** 5.x  

Booking aggregate, availability, concurrency, lifecycle, expire worker, API.

→ [booking/](booking/README.md)

---

## Epic 2 — Payment Platform

**Status:** In progress (6.4 review → 6.5 final)  
**Sprints:** 6.0 – 6.5  

| Sprint | Goal | Status |
|--------|------|--------|
| 6.0 | Architecture pack | ✅ |
| 6.1 | Domain | ✅ |
| 6.2 | Application + Persistence + API | ✅ |
| 6.3 | Provider abstraction + Webhook + Outbox | ✅ |
| 6.4 | Refund HTTP API + workflow | READY FOR REVIEW |
| **6.5** | **Real Stripe / Payriff integration** | **Next** |

→ [payment/](payment/README.md)  
→ [17-sprint-65.md](payment/17-sprint-65.md)

---

## Epic 3 — Communication Platform

**Status:** Planned  
**Sprints:** 7.x  

- Email notification
- SMS notification
- Push notification
- Notification outbox consumer
- Template system

Payment and Booking outbox events already provide triggers (`PaymentSucceeded`, `RefundSucceeded`, etc.).

---

## Epic 4 — Platform Engineering

**Status:** Backlog  
**Sprints:** 8+ (when concrete need arises)

| Area | Trigger to start |
|------|------------------|
| **Redis** | Availability cache, distributed lock, multi-instance rate limiting |
| **Search** | Catalog / listing search requirements |
| **Metrics & dashboards** | Production observability SLOs |
| **DLQ / backoff / poison messages** | Outbox consumer ops maturity |
| **Horizontal scaling** | Multi-instance deployment |

Redis is **not** required for Epic 3 notifications — PostgreSQL outbox + HostedService is sufficient initially.

---

## Maturity arc

```text
Early ZIPs          →  CQRS, Booking, Aggregate
Sprint 6.x          →  ACL, Provider abstraction, Idempotency, Audit
Sprint 6.5+         →  Contract testing, Production rollout, Sandbox certification
Epic 4              →  Architecture governance, Ops maturity, Scale
```
