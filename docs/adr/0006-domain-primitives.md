# ADR 0006: Domain primitives & strongly typed IDs

## Status

Accepted (direction) — implement incrementally; required before/during Booking sprint.

## Context

Lead review (9.9/10): too many primitive `string`/`Guid`/`decimal` fields. Money split across amount + currency; identity and contact data lack validation at the type boundary.

## Decision

Introduce domain primitives gradually:

| Primitive | Shape | Notes |
|-----------|--------|------|
| `Money` | `Amount` + `CurrencyId` (or Currency code) | Non-negative; arithmetic only in same currency |
| `GeoLocation` / keep enriching `AssetLocation` | Lat/Long/Address parts | Already a VO; tighten validation |
| `EmailAddress` | Normalized string | Used by Identity |
| `PhoneNumber` | E.164-ish normalized | Optional on profile |
| `Vin` / `LicensePlate` | When vehicle attributes appear | Category-specific; not on Asset root as required fields |
| `AssetId`, `BookingId`, `UserId`, … | Strongly typed Id wrappers | `readonly record struct` around `Guid` |

### Rules

- Prefer VO over primitive at aggregate boundaries.
- Serialization: APIs may still expose `Guid`/`string`/`decimal` in DTOs; map at Application edge.
- Do not big-bang rewrite Identity in one PR — add types as modules touch those fields.

## Consequences

- Fewer invalid states compile/run time.
- Booking/Payment money handling becomes consistent.
- Slight ceremony; worth it for marketplace money and ids.
