# ADR 0001: Modular Monolith + Clean Architecture + DDD + CQRS

## Status

Accepted

## Context

eHub needs clear module boundaries (Identity, Catalog, Assets, later Booking/Payment) without premature microservices.

## Decision

Ship a modular monolith with Clean Architecture layers, DDD aggregates, and MediatR CQRS vertical slices per use case.

## Consequences

- Modules can later extract to services with less rewrite.
- Requires discipline: no DbContext in Application, no cross-aggregate navigations, thin controllers.
- MediatR + FluentValidation pipeline is the standard request path.
