# ADR 0005: Catalog stays in shared Domain/Application (for now)

## Status

Accepted (interim)

## Context

Review suggested a full Catalog vertical module (Application/Domain/Infrastructure/Persistence). Catalog is currently lookup dictionaries with typed repositories.

## Decision

Keep Catalog entities and typed repos in the shared Domain/Application layers until EF Persistence lands. Revisit extracting a Catalog bounded context when write-side admin APIs and caching requirements grow.

## Consequences

- Less structure churn now.
- Seed + list APIs remain simple.
- Follow-up ADR when Catalog becomes a first-class bounded context.
