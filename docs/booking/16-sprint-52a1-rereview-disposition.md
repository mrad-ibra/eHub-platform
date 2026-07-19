# Sprint 5.2A.1 re-review disposition

**Sprint 5.2A.1:** APPROVED WITH REQUIRED FOLLOW-UPS  
**Production readiness:** NOT APPROVED  

## Follow-ups → Sprint 5.2B

| # | Item | Disposition |
|---|------|-------------|
| H1 | Expired hold still blocked by EXCLUDE | **ExpirePendingBookings** worker + IT |
| H2 | Expired idempotency takeover race | **Conditional UPDATE** reclaim |
| H3 | PG tests silent skip in CI | `EHUB_REQUIRE_POSTGRES_TESTS` + GitHub Actions |
| M1 | Lease outside TX | Documented (5 min processing TTL) |
| M2 | Idempotency unique → wrong semantics | Map to `BookingRequestInProgress`; Begin re-reads |
| M3 | Auto Migrate on startup | Not added — deploy-time `ef database update` |
| M4 | API not in compose | Documented — host `dotnet run` |

See [15-sprint-52b-expire-worker.md](15-sprint-52b-expire-worker.md).
