# Sprint 5.1 — Review disposition (CHANGES REQUIRED → remediation)

**Architect status after first review:** CHANGES REQUIRED  
**Remediation pass:** 2026-07-19

## P0 applied

| # | Issue | Fix |
|---|--------|-----|
| 1 | Past start date | Domain `EnsureStartNotInPast` + FluentValidation + `IClock` |
| 2 | Approve without TTL | `EnsureHoldActive` on Approve |
| 3 | Confirm without payment TTL | `EnsureHoldActive` on Confirm |
| 4 | Expired holds still block | `Booking.BlocksCalendar(now)` + list/add use `now` |
| 5–6 | Idempotency race / overwrite | Atomic `Begin` (TryAdd) + request hash + mismatch 409 + Complete/Abandon |
| 7 | Check≠insert atomicity | `AddAsync(booking, now)` under per-asset lock (InMemory); PG exclusion documented for EF |
| 8 | Fake Location / 501 GetById | `GET /bookings/{id}` implemented |
| 9 | Integration skeleton | `BookingsApiTests` unauthorized smoke |
| 10 | Permissions unused | JWT `permission` claims + policies `bookings.create/read/manage` |

## Also applied

- Single `now = clock.UtcNow` per create  
- Asset block vs occupied range (incl. buffer)  
- Specific error codes (driver/delivery/past/idempotency/hold expired)  
- `Version` → `AggregateVersion` (not snapshot schema)  
- Placeholder JWT `CHANGE_ME*` rejected at startup / token create  
- Idempotency header via localized `ValidationFailedException`

## Still open (P1 / production)

| Item | Notes |
|------|--------|
| EF Persistence | Empty project — required for real TX + exclusion constraint |
| Expire worker | Lazy calendar filter is interim; Hangfire job still needed |
| Booking number DB sequence | InMemory counter not multi-instance safe |
| Asset-level buffer field | Still platform default 1 day |
| Domain↔Localization coupling | Deferred architecture cleanup |
| Rate limit / security headers / CORS | Ops hardening |
| Full concurrent integration suite | Skeleton only |

## PostgreSQL concurrency target (when EF lands)

```sql
EXCLUDE USING gist (
  asset_id WITH =,
  occupied_period WITH &&
) WHERE (status IN ('PENDING_OWNER_APPROVAL','PENDING_PAYMENT','CONFIRMED','IN_PROGRESS'));
```

Plus unique `(user_id, idempotency_key)` and booking_number uniqueness.
