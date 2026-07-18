# ADR 0004: Feature-owned DTOs (Contracts stays empty)

## Status

Accepted

## Context

An `eHub.Contracts` project exists. Dumping all DTOs there weakens feature ownership and creates a dumping ground.

## Decision

Keep command/query DTOs inside Application vertical slices. Reserve `eHub.Contracts` for future external/integration contracts only.

## Consequences

- Stronger cohesion per feature.
- Contracts project may stay empty until a real cross-boundary need appears.
