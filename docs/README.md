# eHub docs

| Doc | Purpose |
|-----|---------|
| [architecture.md](architecture.md) | Layers, modules, aggregate rules |
| [erd.md](erd.md) | Current + planned data model |
| [api.md](api.md) | HTTP surface (v1) |
| [adr/](adr/README.md) | Architecture Decision Records |

## Sprint discipline

Before implementing a sprint: agree Aggregate / Value Objects / Domain Events / business rules, then code. Especially for **Booking** (transactions, concurrency, availability) and **Payment** (separate aggregate).
