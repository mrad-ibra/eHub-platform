# ADR 0002: Universal Asset aggregate

## Status

Accepted

## Context

Marketplace listings span cars, boats, equipment, etc. Separate root entities per type would explode the model.

## Decision

One `Asset` aggregate root. Category/brand/model meaning comes from Catalog FKs (`CategoryId`, …). Internal complexity lives in components (`AssetLifecycle`, `AssetCommercialTerms`, `AssetMediaCollection`, …). Status is `AssetStatusCode` (smart enum).

## Consequences

- No Car/Boat/Excavator roots.
- Future concerns (GPS, Insurance, Reviews) become new components or separate aggregates/modules—not infinite growth of `Asset.cs`.
- Payment and Booking must not be nested inside Asset.
