# Threat model (draft)

Lightweight STRIDE-oriented notes. Revisit before public launch.

| Threat | Area | Mitigation (current / planned) |
|--------|------|--------------------------------|
| Spoofing | Auth | JWT + refresh rotation; hashed refresh tokens |
| Tampering | Assets | Owner checks in handlers; domain status locks |
| Repudiation | Auth | Login history; audit fields (`CreatedBy`/`UpdatedBy`) |
| Information disclosure | API | ProblemDetails without stack in prod; no secrets in logs |
| Denial of service | API | Planned: rate limiting, Redis |
| Elevation of privilege | Roles | Permission catalog; tighten ApproveAsset with policies |
| Payment fraud | Payment | Planned: idempotent webhooks (inbox), server-side amount checks |
| Double booking | Booking | Planned: optimistic concurrency + availability conflict + optional distributed lock |

## Secrets

- Never commit real JWT keys / DB passwords.
- `docker-compose.prod.yml` requires env secrets.
- Prefer User Secrets / vault in real environments.
