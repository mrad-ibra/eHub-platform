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
# Infra only (Postgres + Redis + pgAdmin) — API is not in compose
docker compose -f docker-compose.dev.yml up -d

# Apply EF migrations (not run automatically on API startup)
dotnet ef database update \
  --project src/eHub.Persistence \
  --startup-project src/eHub.Api

dotnet restore
dotnet build
dotnet test
dotnet run --project src/eHub.Api
```

Swagger is available in Development. See [docs/api.md](docs/api.md).

### Ops: database & Docker

| Topic | Decision |
| --- | --- |
| **Migrations** | Applied by **deploy/ops** (`dotnet ef database update` or a migration job). API **does not** call `Database.Migrate()` on startup (safer for multi-instance). |
| **Docker Compose** | Raises **PostgreSQL, Redis, pgAdmin** only. API runs on the host via `dotnet run` until an API image is added to compose. |
| **Connection** | `ConnectionStrings:DefaultConnection` (see `appsettings.Development.json` / env). Empty → in-memory Booking path. |

### Expire worker (already configurable)

`Jobs:ExpirePendingBookings` in `appsettings.json`: `Enabled`, `IntervalSeconds`, `BatchSize`, `RetryDelaySeconds`. Metrics: expired/skipped counts + duration (logging stub). On shutdown, the current batch stops accepting new rows and commits what was already prepared.

## Roadmap

| Phase | Focus | Status |
| --- | --- | --- |
| **Done** | Sprint 5.2B — Expire Worker, outbox table, CI PG gate | **APPROVED WITH MINOR COMMENTS** |
| **Next** | Notification / Outbox processing | Planned |
| **Soon** | Payment module | Planned |
| **Later** | Observability (OTel), Redis caching, Search | Planned |
| **Ops** | API container in compose; migrate job in pipeline | Planned |

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
