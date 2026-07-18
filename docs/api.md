# API overview

Base: `/api/v{version}` (URL segment versioning). Default: **v1**.

Errors: RFC 7807 `application/problem+json` with `type`, `title`, `status`, `detail`, `instance`, `traceId`, `code`.

Interactive docs: Swagger UI in Development.

## Auth — `/api/v1/auth`

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| POST | `/login` | No | Access + refresh session |
| GET | `/me` | Yes | Current user |
| GET | `/sessions` | Yes | Active sessions |
| POST | `/sessions/{id}/revoke` | Yes | Revoke one |
| POST | `/sessions/revoke-others` | Yes | Keep current |
| POST | `/logout` | Yes | Revoke current/all |
| GET | `/login-history` | Yes | Recent logins |
| POST | `/confirm-email` | No | |
| POST | `/resend-verification` | No | |
| POST | `/forgot-password` | No | |
| POST | `/reset-password` | No | |

## Catalog — `/api/v1/catalog`

GET dictionaries (active by default), e.g.:

- `/categories`, `/categories/{id}/subcategories`
- `/brands`, `/brands/{id}/models`
- `/countries`, `/countries/{id}/cities`, `/cities/{id}/districts`
- `/currencies`, `/languages`, `/transmissions`, `/fuel-types`, …
- `/asset-statuses`, `/booking-statuses`, …

## Assets — `/api/v1/assets`

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| POST | `/` | Yes | Create draft |
| GET | `/mine` | Yes | Owner list |
| GET | `/{id}` | Anonymous | Detail |
| PUT | `/{id}` | Yes | Details |
| PUT | `/{id}/pricing` | Yes | |
| PUT | `/{id}/location` | Yes | |
| POST | `/{id}/media` | Yes | |
| POST | `/{id}/publish` | Yes | |
| POST | `/{id}/submit-approval` | Yes | |
| POST | `/{id}/approve` | Yes | Moderator path |
| POST | `/{id}/reject` | Yes | |
| POST | `/{id}/archive` | Yes | |

Success bodies return resource DTOs or `204 NoContent` for mutations without payload. Created resources return `201` + id.
