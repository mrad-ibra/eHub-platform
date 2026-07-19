# eHub

**Rent Anything. Anytime. Anywhere.**

Unified marketplace for listing and renting physical assets (vehicles, watercraft, equipment, generators, …) under one **universal `Asset`** model.

## Tech stack

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core 9, C#, MediatR (CQRS), FluentValidation, Serilog |
| Data (planned) | PostgreSQL, EF Core, Redis |
| Realtime (planned) | SignalR |
| Frontend | Next.js, Tailwind CSS |
| Mobile | Flutter (later) |

## Solution layout

```text
src/
  eHub.Api
  eHub.Application      # vertical slices (Commands/Queries)
  eHub.Domain           # aggregates, VOs, rules
  eHub.Localization     # ErrorCodes + .resx
  eHub.Infrastructure
  eHub.Persistence
  eHub.SharedKernel
  eHub.Contracts        # reserved (empty) — feature DTOs live in Application
docs/                   # architecture, ADR, ERD, API, BRS, C4, …
```

## Getting started

```bash
# Infra (Postgres + Redis + pgAdmin)
docker compose -f docker-compose.dev.yml up -d

dotnet restore
dotnet build
dotnet test
dotnet run --project src/eHub.Api
```

Swagger is available in Development. See [docs/api.md](docs/api.md).

## Roadmap

| Phase | Focus | Status |
| --- | --- | --- |
| **Current** | **Sprint 5.2A — EF Persistence Foundation** (Booking DbContext, constraints, migration) | In progress |
| **Next** | Sprint 5.2B Expire Worker; deeper PG integration tests | Planned |
| **Soon** | Payment aggregate; Outbox/Inbox hardening; EF Core persistence for Booking | Planned |
| **Later** | Notification, GPS, Chat (SignalR), Search abstraction → OpenSearch | Planned |
| **Ops** | OpenTelemetry, rate limiting, compose.prod hardening, Dependabot/CodeQL | Planned |

### Future modules (must stay separate)

- **Booking** — own aggregate (availability, race conditions, distributed lock / concurrency)
- **Payment** — own aggregate/module (never inside Asset)
- **Notification** — email/SMS/push via domain events + outbox
- **GPS / Chat / Search** — own modules

## Development rules

1. Design-first: Business Rules → Domain Model → State Machine → Sequences → ER → API → AC / edges / failures / tests → then code.
2. Controllers stay thin; business rules live in Domain.
3. Cross-aggregate references are **Id-only** (no navigations on domain models).
4. Keep `Asset` lean via internal components; new concerns → new component or new module.
5. Errors → RFC 7807 ProblemDetails; strings → `eHub.Localization`.
6. Prefer typed repositories over generic CRUD stores.

More: [docs/README.md](docs/README.md).

## License

Proprietary — all rights reserved.
