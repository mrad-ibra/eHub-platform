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
| [adr/](adr/README.md) | Architecture Decision Records |

## Sprint discipline

Before implementing a sprint: agree Aggregate / Value Objects / Domain Events / business rules (update BRS), then code. Especially for **Booking** (transactions, concurrency, availability) and **Payment** (separate aggregate).
