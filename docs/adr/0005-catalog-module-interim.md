# ADR 0005: Catalog stays in shared Domain/Application (for now)

## Status

Accepted (interim) — tracked tech debt

## Context

Review suggested a full Catalog vertical module (Application/Domain/Infrastructure/Persistence). Catalog is currently lookup dictionaries with typed repositories.

## Decision

Keep Catalog entities and typed repos in the shared Domain/Application layers until EF Persistence lands. Revisit extracting a Catalog bounded context when write-side admin APIs and caching requirements grow.

## Consequences

- Less structure churn now.
- Seed + list APIs remain simple.
- Follow-up ADR when Catalog becomes a first-class bounded context.

## Backlog (debt management)

| Field | Value |
|-------|--------|
| **Owner** | Platform / Catalog module owner |
| **Target sprint** | After Payment Sprint 6.1 + Notification consumer (tentative: Sprint 7.x) |
| **Risk** | Shared Domain grows; harder to scale Catalog writes independently; weak caching boundary |
| **Exit criteria** | Catalog write-side admin APIs exist; dedicated EF mappings; optional Redis cache; no cross-module leaks into Booking/Payment Domain |
| **Replacement design** | Extract `eHub.Catalog` (Domain/Application/Infrastructure/Persistence) + follow-up ADR superseding this one |
