# EHUB-508 — Database Design

**Status:** APPROVED WITH MINOR CHANGES (Architect 2026-07-19).  
Logical model for Sprint 5.1. No migrations this sprint.

## ER overview

```mermaid
erDiagram
  Asset ||--o{ Booking : "AssetId"
  User ||--o{ Booking : "RenterId"
  Booking ||--o{ BookingStatusHistory : has
  Booking ||--o{ BookingTimelineEntry : has
  Booking ||--o| BookingDriver : has
  Booking ||--o| BookingDelivery : has
  Booking ||--o{ BookingAttachment : has
  Booking ||--o| BookingAssetSnapshot : has
  Booking ||--o| BookingTerms : has
  Booking ||--o| Payment : "PaymentId optional"
  Booking ||--o{ OutboxMessage : emits

  Booking {
    uuid Id PK
    string BookingNumber UK
    uuid AssetId FK
    uuid RenterId FK
    uuid HostId
    date StartDate
    date EndDate
    int BufferDays
    string Status
    decimal UnitAmount
    uuid CurrencyId
    decimal TotalAmount
    timestamptz ExpiresAtUtc
    timestamptz ConfirmedAtUtc
    int Version
    timestamptz CreatedAtUtc
  }

  BookingAssetSnapshot {
    uuid BookingId PK_FK
    string Name
    string Brand
    string Model
    string PrimaryImageUrlsJson
    string HostDisplayName
    timestamptz CapturedAtUtc
  }

  BookingTerms {
    uuid BookingId PK_FK
    int MinRentalDays
    int MaxRentalDays
    string RulesJson
  }

  BookingStatusHistory {
    uuid Id PK
    uuid BookingId FK
    string FromStatus
    string ToStatus
    uuid ActorId
    timestamptz AtUtc
  }

  BookingTimelineEntry {
    uuid Id PK
    uuid BookingId FK
    string Code
    string Message
    uuid ActorId
    timestamptz AtUtc
  }
```

## Tables (logical)

| Table | Notes |
|-------|--------|
| `Bookings` | Root + period + BufferDays + money + status + BookingNumber + Version |
| `BookingAssetSnapshots` | Frozen asset display at create |
| `BookingTerms` | Frozen rental rules at create |
| `BookingStatusHistory` | Audit: Created → Approved → Rejected → Paid → Started → … |
| `BookingTimeline` | Product-facing timeline |
| `BookingDrivers` / `BookingDeliveries` / `BookingAttachments` | Optional children |
| `Payments` | Other aggregate |
| `OutboxMessages` / `IdempotencyKeys` | Infra |

## Indexes

- `BookingNumber` UNIQUE  
- `(AssetId, StartDate, EndDate)`  
- `(AssetId, Status)` filtered blocking  
- `(RenterId, CreatedAtUtc DESC)` / `(HostId, Status)`  
- `(ExpiresAtUtc)` where pending  
- Exclusion/range for occupied ranges including buffer (EF sprint)

## Sign-off

- [x] Number + Snapshot + Terms tables approved  
- [x] Payment separate approved
