# eHub Platform — Architecture

## Style

Modular Monolith + Clean Architecture + DDD + CQRS (MediatR).

```text
eHub.Api                → HTTP, auth, ProblemDetails, versioning
eHub.Application        → Commands/Queries (vertical slices), validators, ports
eHub.Domain             → Aggregates, VOs, domain rules
eHub.Localization       → Error/Message resources (.resx) — separate project
eHub.Infrastructure     → Adapters (JWT, email, in-memory repos, seed)
eHub.Persistence        → EF Core (planned)
eHub.SharedKernel       → Cross-cutting primitives
eHub.Contracts          → Reserved / empty — feature DTOs stay in Application
```

## Dependency rule

`Api → Application → Domain → Localization`  
Infrastructure / Persistence implement Application abstractions and are composed in Api.

## Current modules

| Module | Status |
|--------|--------|
| Identity | Live (auth, sessions, login history) |
| Catalog | Live (dictionaries) |
| Assets | Live (universal rentable aggregate) |
| Bookings | Planned |
| Payments | Planned (separate aggregate) |
| Notifications | Planned (domain events + outbox) |
| GPS / Chat | Planned (own modules, not inside Asset) |

## Aggregate rules

- Mutate only through Aggregate Root methods.
- Cross-aggregate references are **Id-only** (no navigations on the domain model).
- Keep Asset lean via internal components (`Lifecycle`, `Commercial`, `MediaCollection`, …).
- Do not fold Payment, Chat, GPS, or Notification into Asset.

## Read vs write

- Commands: load aggregate → domain method → `IUnitOfWork.SaveChangesAsync`.
- Queries: return DTOs from handlers; mapping stays in Application, not Controllers.
- When EF lands: use `AsNoTracking` + projection for reads; indexes on hot columns.

## Errors & observability

- RFC 7807 `application/problem+json` (`type`, `title`, `status`, `detail`, `instance`, `traceId`, `code`).
- Serilog structured logging; OpenTelemetry planned.
