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
| [booking/](booking/README.md) | **Sprint 5.2 COMPLETED** — Booking Core |
| [payment/](payment/README.md) | **Sprint 6.0 READY FOR REVIEW** — Payment Architecture Pack |
| [observability.md](observability.md) | OTel, metrics, correlation, health |
| [ops/vault.md](ops/vault.md) | HashiCorp Vault secrets for eHub |
| [booking-technical-design.md](booking-technical-design.md) | Pointer → `booking/` (legacy path) |
| [adr/](adr/README.md) | Architecture Decision Records |

## Sprint discipline

**Design-first development:** lock Business Rules, Domain Model, State Machine, Sequences, ER, API Contract, Acceptance Criteria, Edge Cases, Failure Scenarios, and Test Scenarios — **then** code.

**Sprint 5.2** = Booking Core — **APPROVED / COMPLETED** ([disposition](booking/14-sprint-52-final-disposition.md)).  
**Sprint 6.0** = Payment design only — **READY FOR ARCHITECT REVIEW** ([payment/](payment/README.md)).  
**Sprint 6.1** = Payment implementation — only after 6.0 **APPROVED**.

Especially for **Booking** (transactions, concurrency, availability) and **Payment** (separate aggregate).
