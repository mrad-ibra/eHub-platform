# eHub

**Rent Anything. Anytime. Anywhere.**

Unified marketplace for listing and renting physical assets (vehicles, watercraft, equipment, generators, …) under one **universal `Asset`** model.

## Tech stack

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core 9, C#, MediatR (CQRS), FluentValidation, Serilog, OpenTelemetry |
| Data | PostgreSQL, EF Core, Redis |
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
docs/                   # architecture, ADR, Booking, Payment, …
```

## Getting started

### Option A — host API (recommended for day-to-day)

```bash
docker compose -f docker-compose.dev.yml up -d postgres redis pgadmin

dotnet ef database update \
  --project src/eHub.Persistence \
  --startup-project src/eHub.Api

dotnet run --project src/eHub.Api
```

API: `http://localhost:5xxx` (see launchSettings). Health: `/health`, metrics: `/metrics`.

### Option B — full stack in Docker

```bash
# First time: ensure migrations applied to the compose Postgres volume
docker compose -f docker-compose.dev.yml up -d postgres
dotnet ef database update \
  --project src/eHub.Persistence \
  --startup-project src/eHub.Api

docker compose -f docker-compose.dev.yml up --build
```

API container listens on **http://localhost:8080**.

Migrations are **not** applied inside `Program.cs` (deploy/ops owns schema changes).

### Ops notes

| Topic | Decision |
| --- | --- |
| **Migrations** | `dotnet ef database update` or a pipeline job — never auto on multi-instance startup |
| **Compose** | Postgres + Redis + pgAdmin + **API** (`docker-compose.dev.yml`) |
| **Connection** | Prefer Vault (`docs/ops/vault.md`). Fallback: `ConnectionStrings:DefaultConnection` env / appsettings. Empty → in-memory Booking |
| **Observability** | OTel traces/metrics, `X-Correlation-Id`, Prometheus `/metrics`, Serilog |

### Booking metrics

`booking_created_total` · `booking_conflict_total` · `booking_expired_total` · `idempotency_conflict_total` · `expire_worker_duration` · `expire_worker_failed_total`

## Roadmap

| Phase | Focus | Status |
| --- | --- | --- |
| **Done** | Sprint 5.2 — Booking Core (EF, EXCLUDE, expire worker, CI) | **APPROVED / COMPLETED** |
| **Now** | Sprint 6.0 — Payment Architecture Pack | **READY FOR ARCHITECT REVIEW** |
| **Next** | Sprint 6.1 — Payment implementation | After 6.0 APPROVED |
| **Soon** | Notification / Outbox consumer | Planned |
| **Later** | Redis caching, Search, richer OTel dashboards | Planned |

### Future modules (must stay separate)

- **Booking** — COMPLETED core  
- **Payment** — design pack in [docs/payment](docs/payment)  
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
