# EPIC 5 — Booking Engine (Sprint 5.0 Domain Design)

**Sprint:** 5.0 — Booking Domain Design  
**Rule:** **Zero implementation code** in this sprint. Design-first only.  
**Epic goal:** Ensure two people cannot reserve the same Asset for overlapping periods — correctly, under failure, under concurrency.

| Task | Document | Status |
|------|----------|--------|
| EHUB-500 | [00-business-rules.md](00-business-rules.md) | Draft for sign-off |
| EHUB-501 | [01-aggregate-design.md](01-aggregate-design.md) | Draft for sign-off |
| EHUB-502 | [02-state-machine.md](02-state-machine.md) | Draft for sign-off |
| EHUB-503 | [03-lifecycle.md](03-lifecycle.md) | Draft for sign-off |
| EHUB-504 | [04-availability-strategy.md](04-availability-strategy.md) | Draft for sign-off |
| EHUB-505 | [05-concurrency-strategy.md](05-concurrency-strategy.md) | Draft for sign-off |
| EHUB-506 | [06-domain-events.md](06-domain-events.md) | Draft for sign-off |
| EHUB-507 | [07-sequence-diagrams.md](07-sequence-diagrams.md) | Draft for sign-off |
| EHUB-508 | [08-database-design.md](08-database-design.md) | Draft for sign-off |
| EHUB-509 | [09-api-contract.md](09-api-contract.md) | Draft for sign-off |
| EHUB-510 | [10-failure-scenarios.md](10-failure-scenarios.md) | Draft for sign-off |
| Companion | [11-acceptance-edge-tests.md](11-acceptance-edge-tests.md) | AC / edges / tests |

## Design-first checklist (every Booking task later)

For each implementation story in Sprint 5.1+:

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

## Sign-off

| Artifact pack | Architect | Domain owner | Date |
|---------------|-----------|--------------|------|
| Sprint 5.0 (EHUB-500…510) | ☐ | ☐ | |

**Sprint 5.1 (code) is blocked until this pack is signed.**

## Related

- [ADR 0006 — Domain primitives](../adr/0006-domain-primitives.md)  
- [BRS](../brs.md)  
- Supersedes root [booking-technical-design.md](../booking-technical-design.md)
