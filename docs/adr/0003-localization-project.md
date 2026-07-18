# ADR 0003: Localization project

## Status

Accepted

## Context

`.resx` and resource accessors lived under Domain, mixing pure domain with presentation strings.

## Decision

Move codes + resources to `eHub.Localization`. Domain and upper layers depend on it for message resolution.

## Consequences

- Clear localization ownership.
- Domain still throws with localized text today; a future step may throw error codes only and localize at the API edge.
