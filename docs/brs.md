# Business Rules Specification (BRS)

Living document. Expand before each domain sprint. Error codes live in `eHub.Localization`.

## Identity

| ID | Rule |
|----|------|
| ID-01 | Login requires confirmed email when confirmation is enabled |
| ID-02 | Inactive users cannot authenticate |
| ID-03 | Refresh tokens are stored hashed; rotation revokes previous |
| ID-04 | Soft-deleted users are not resolvable by email/id |

## Catalog

| ID | Rule |
|----|------|
| CAT-01 | Codes are uppercase, max length enforced |
| CAT-02 | Child items (SubCategory/Model/City/District) must reference a valid parent |
| CAT-03 | Soft-deleted catalog rows are excluded from default lists |

## Asset

| ID | Rule |
|----|------|
| AST-01 | Root entity is always `Asset` (never Car/Boat/…) |
| AST-02 | Catalog links are Id-only (`CategoryId`, `BrandId`, …) |
| AST-03 | Publish / submit-approval requires pricing + location + ≥1 image |
| AST-04 | Pending approval locks owner edits |
| AST-05 | Archived assets cannot be published |
| AST-06 | Status transitions via `AssetStatusCode` smart enum only |
| AST-07 | Availability blocks cannot overlap |
| AST-08 | Mutate media/pricing/availability only through the Asset root |

## Booking (planned)

| ID | Rule |
|----|------|
| BKG-01 | Booking is its own aggregate (not nested in Asset) |
| BKG-02 | Conflicting date ranges on the same asset are rejected |
| BKG-03 | Create is idempotent given client idempotency key |
| BKG-04 | Optimistic concurrency on Asset/Booking version |
| BKG-05 | Side effects (email, analytics) via domain events + outbox |

## Payment (planned)

| ID | Rule |
|----|------|
| PAY-01 | Payment is its own aggregate/module |
| PAY-02 | Amounts are non-negative; currency from Catalog |
| PAY-03 | Provider webhooks are idempotent (inbox) |
