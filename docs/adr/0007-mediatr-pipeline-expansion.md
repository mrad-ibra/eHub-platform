# ADR 0007: MediatR pipeline expansion (planned)

## Status

Proposed

## Context

CQRS pipeline currently runs FluentValidation. Cross-cutting concerns will grow with Booking.

## Decision (planned order)

1. Logging / performance timing (OpenTelemetry activities)  
2. Authorization behavior (permission codes)  
3. Idempotency behavior (`Idempotency-Key` → store)  
4. Caching behavior for selected queries  

Do not add all at once; introduce with Booking and measure.

## Consequences

- Handlers stay thin.
- Requires careful ordering (Auth → Idempotency → Validation → Handler).
