# ADR 0002: Universal Asset aggregate

## Status

Accepted

## Context

Marketplace listings span cars, boats, equipment, etc. Separate root entities per type would explode the model.

## Decision

One `Asset` aggregate root. Category/brand/model meaning comes from Catalog FKs (`CategoryId`, …). Internal complexity lives in components (`AssetLifecycle`, `AssetCommercialTerms`, `AssetMediaCollection`, …). Status is `AssetStatusCode` (smart enum).

## Consequences

- No Car/Boat/Excavator roots.
- Internal components act as **domain managers** inside the aggregate:
  - `AssetLifecycle` — status / version history
  - `AssetCommercialTerms` — pricing, location, rules, deposit, support
  - `AssetMediaCollection` / `AssetAvailability` / `AssetAttributeCollection`
- Future Insurance / GPS / Reviews / Maintenance → **new component** or **separate module/aggregate** (never Booking/Payment inside Asset).
- Payment and Booking must not be nested inside Asset.
