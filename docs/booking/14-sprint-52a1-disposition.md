# Sprint 5.2A Review + 5.2A.1 disposition

**Sprint 5.2A:** APPROVED WITH COMMENTS  
**Scores (Architect):** Architecture 9.8 · Persistence Foundation 9.0 · Production Readiness 7.8  

## Comments disposition

| # | Item | Disposition |
|---|------|-------------|
| H1 | EXCLUDE USING gist missing | **Present** in `InitialBookingPersistence` (`bookings_no_overlap`). Comment + docs clarified. |
| H2 | App-level conflict in repository | Kept as UX; DB exclusion = correctness; **Npgsql mapping** via `PostgresExceptionMapper`. |
| M3 | Integration tests too weak | **Replaced** with Testcontainers suite (migrate, owned read-back, rollback, idempotency, parallel exclude, concurrency token). |
| M4 | `DateTime.UtcNow` in repo | **Fixed** — `IClock` injected. |
| M5 | JSON / GIN later | Noted for future snapshot growth. |
| P1 | Booking number sequence | Already `nextval('booking_number_seq')`. |

## Next

**Next:** Architect re-review of Sprint 5.2B ([15](15-sprint-52b-expire-worker.md)).
