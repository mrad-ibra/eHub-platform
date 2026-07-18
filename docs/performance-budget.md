# Performance budget (draft)

Targets for the public API once EF + Redis are live. Adjust after load tests.

| Metric | Target (p95) | Notes |
|--------|--------------|-------|
| `GET /catalog/*` | < 50 ms | Cache-friendly; dictionaries |
| `GET /assets/{id}` | < 100 ms | AsNoTracking + projection |
| `GET /assets/mine` | < 150 ms | Indexed `OwnerId` |
| `POST /bookings` (planned) | < 300 ms | Contended path; measure under lock |
| Auth login | < 200 ms | BCrypt dominates |

## Capacity assumptions (initial)

- Concurrent users: design for 1k → 10k with horizontal API scale
- Asset list pages: cursor/offset pagination (add when search lands)
- Media: CDN URLs only in API (no binary proxy)

## Caching strategy (planned)

| Data | Store | TTL |
|------|-------|-----|
| Catalog dictionaries | Redis / memory | Long (invalidate on admin write) |
| Published asset cards | Redis | Short |
| Sessions | DB (+ optional Redis) | Token lifetime |

## DB indexes (when EF lands)

`Asset(OwnerId)`, `Asset(CategoryId)`, `Asset(StatusCode)`, `Asset(CreatedAtUtc)`, location FKs — see review item #15.
