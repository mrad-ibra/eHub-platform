# eHub docs

| Doc | Purpose |
|-----|---------|
| [architecture.md](architecture.md) | Layers, modules, aggregate rules |
| [erd.md](erd.md) | Current + planned data model |
| [api.md](api.md) | HTTP surface (v1) |
| [brs.md](brs.md) | Business rules specification |
| [c4.md](c4.md) | C4 context / container / component |
| [sequences.md](sequences.md) | Key flow diagrams |
| [threat-model.md](threat-model.md) | Security threats & mitigations |
| [performance-budget.md](performance-budget.md) | Latency & cache targets |
| [booking/](booking/README.md) | **Sprint 5.0 gate** — Booking Domain Design pack (EHUB-500…510) |
| [booking-technical-design.md](booking-technical-design.md) | Pointer → `booking/` (legacy path) |
| [adr/](adr/README.md) | Architecture Decision Records |

## Sprint discipline

**Design-first development:** for Booking (and later critical modules), lock Business Rules, Domain Model, State Machine, Sequences, ER, API Contract, Acceptance Criteria, Edge Cases, Failure Scenarios, and Test Scenarios — **then** code.

**Sprint 5.0** = design only — **APPROVED WITH MINOR CHANGES**.  
**Sprint 5.1** = implementation — **green light** (see [booking/](booking/README.md)).

Especially for **Booking** (transactions, concurrency, availability) and **Payment** (separate aggregate).
