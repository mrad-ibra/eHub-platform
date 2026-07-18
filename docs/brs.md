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

## Booking (Sprint 5.0 design — see [booking/](booking/README.md))

Canonical detail: `docs/booking/00-business-rules.md` (BR-BKG-*). Summary:

| ID | Rule |
|----|------|
| BKG-01 | Booking is its own aggregate (not nested in Asset) |
| BKG-02 | Inclusive calendar-day overlap rejected for blocking statuses |
| BKG-03 | PendingOwnerApproval / PendingPayment **do** hold availability |
| BKG-04 | TTL: 24h approval, 30m payment → Expired (not Cancelled) |
| BKG-05 | Host may reject only from PendingOwnerApproval (reason required) |
| BKG-06 | Instant Book off by default (v1); price frozen at create |
| BKG-07 | Asset stays Published; occupancy derived from bookings |
| BKG-08 | Create is idempotent (`Idempotency-Key`) |
| BKG-09 | Optimistic concurrency first; exclusion constraint at EF time |
| BKG-10 | Side effects via domain events + outbox |

## Payment (planned)

| ID | Rule |
|----|------|
| PAY-01 | Payment is its own aggregate/module |
| PAY-02 | Amounts are non-negative; currency from Catalog |
| PAY-03 | Provider webhooks are idempotent (inbox) |
