# EPIC 5 — Booking Engine (Sprint 5.0 Domain Design)

**Sprint:** 5.0 — Booking Domain Design  
**Rule:** Zero implementation code in this sprint.  
**Epic goal:** Prevent double-booking the same Asset for overlapping periods — under Soft Hold, buffer, failure, and concurrency.

## Architect sign-off

| Artifact pack | Decision | Architect | Date |
|---------------|----------|-----------|------|
| Sprint 5.0 (EHUB-500…510 + companion) | **APPROVED WITH MINOR CHANGES** | ✅ | 2026-07-19 |

Minor changes applied:

1. Soft Hold for `PendingOwnerApproval` (calendar reserved; released on expire/reject/cancel)  
2. Approval TTL **12h**; Payment TTL **15m** (timer starts only after Approve)  
3. Booking Buffer (default 1 day, owner-configurable)  
4. Booking Number, Asset Snapshot, Rental Terms snapshot, Version  

**Sprint 5.1 (Create Booking + engine) — green light** after these edits are in the pack (done).

| Task | Document | Status |
|------|----------|--------|
| EHUB-500 | [00-business-rules.md](00-business-rules.md) | Approved (minor changes applied) |
| EHUB-501 | [01-aggregate-design.md](01-aggregate-design.md) | Approved (minor changes applied) |
| EHUB-502 | [02-state-machine.md](02-state-machine.md) | Approved (minor changes applied) |
| EHUB-503 | [03-lifecycle.md](03-lifecycle.md) | Approved (minor changes applied) |
| EHUB-504 | [04-availability-strategy.md](04-availability-strategy.md) | Approved (minor changes applied) |
| EHUB-505 | [05-concurrency-strategy.md](05-concurrency-strategy.md) | Approved |
| EHUB-506 | [06-domain-events.md](06-domain-events.md) | Approved |
| EHUB-507 | [07-sequence-diagrams.md](07-sequence-diagrams.md) | Approved (minor changes applied) |
| EHUB-508 | [08-database-design.md](08-database-design.md) | Approved (minor changes applied) |
| EHUB-509 | [09-api-contract.md](09-api-contract.md) | Approved (minor changes applied) |
| EHUB-510 | [10-failure-scenarios.md](10-failure-scenarios.md) | Approved (minor changes applied) |
| Companion | [11-acceptance-edge-tests.md](11-acceptance-edge-tests.md) | Approved (minor changes applied) |

## Design-first checklist (Sprint 5.1+ stories)

1. Business Rules  
2. Domain Model  
3. State Machine impact  
4. Sequence Diagram  
5. ER impact  
6. API Contract  
7. Acceptance Criteria  
8. Edge Cases  
9. Failure Scenarios  
10. Test Scenarios  

## Related

- [ADR 0006 — Domain primitives](../adr/0006-domain-primitives.md)  
- [BRS](../brs.md)  
- [booking-technical-design.md](../booking-technical-design.md) (pointer)
