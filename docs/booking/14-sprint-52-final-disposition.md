# Sprint 5.2 — Final disposition (Booking Core closeout)

**Status:** Sprint 5.2 — **APPROVED**  
**Booking Core — COMPLETED**  
**Date:** 2026-07-19  

## Checklist verification

| Check | Result |
|-------|--------|
| `ExpirePendingBookingsHostedService` registered once | ✅ Single registration after EF/in-memory branch (`DependencyInjection.cs`) |
| Unit tests | ✅ 119 passed |
| Integration tests (local, no Docker) | ✅ 2 API smoke passed; 8 PG tests **Skipped** (Docker absent) |
| Integration tests (CI) | ✅ `EHUB_REQUIRE_POSTGRES_TESTS=true` — PG suite mandatory on GitHub Actions |
| Migrations on empty DB | ✅ Applied to remote empty DB previously; suite includes migrate + EXCLUDE + sequence |
| Parallel booking (PG EXCLUDE) | ✅ Covered by `ParallelOverlappingInserts_OneSucceeds_OneConflictsViaExclusion` |
| Expired hold blocks until worker | ✅ `ExpiredHold_StillBlocksViaExclusion_UntilWorkerExpires` |
| Idempotency replay / mismatch | ✅ Unit + PG IT coverage |
| CI pipeline | ✅ `.github/workflows/ci.yml` |

## Delivered in 5.2

- EF Persistence (`eHub.Persistence`), exclusion constraint, sequence, concurrency token  
- Atomic idempotency lease takeover  
- Expire worker + outbox table  
- Observability foundation (OTel, correlation, metrics, health) — follow-on polish  
- Docs pack through Sprint 5.2B approvals  

## Explicit non-goals (still open platform work)

- Auto `Database.Migrate()` on API startup (deploy-owned)  
- Full Notification consumer / Payment implementation  
- Production provider integrations  

## Handoff

Next: **Sprint 6.0 — Payment Architecture Pack** (`docs/payment/`) → Architect review → Sprint 6.1 implementation.
