# EHUB-508 — Database Design

**Status:** Logical model for Sprint 5.1 persistence. No migrations this sprint.

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
  Booking ||--o| Payment : "PaymentId optional"
  Booking ||--o{ OutboxMessage : emits

  Booking {
    uuid Id PK
    uuid AssetId FK
    uuid RenterId FK
    uuid HostId
    date StartDate
    date EndDate
    string Status
    decimal UnitAmount
    uuid CurrencyId
    decimal TotalAmount
    timestamptz ExpiresAtUtc
    timestamptz ConfirmedAtUtc
    int Version
    timestamptz CreatedAtUtc
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

  BookingDriver {
    uuid BookingId PK_FK
    bool Requested
    decimal FeeAmount
  }

  BookingDelivery {
    uuid BookingId PK_FK
    bool Requested
    decimal FeeAmount
    string AddressLine
  }

  BookingAttachment {
    uuid Id PK
    uuid BookingId FK
    string Url
    string Kind
  }

  Payment {
    uuid Id PK
    uuid BookingId FK
    string Status
    decimal Amount
    uuid CurrencyId
  }
```

## Tables (logical)

| Table | Notes |
|-------|--------|
| `Bookings` | Aggregate root row + period + money snapshot + status |
| `BookingStatusHistory` | Append-only transitions |
| `BookingTimeline` | Product-facing timeline |
| `BookingDrivers` | 0..1 | Optional driver add-on snapshot |
| `BookingDeliveries` | 0..1 | Optional delivery snapshot |
| `BookingAttachments` | Docs/photos related to booking |
| `Payments` | **Other aggregate** — listed for FK clarity |
| `OutboxMessages` | Shared infra |
| `IdempotencyKeys` | Create booking dedupe |

## Indexes (required at EF time)

- `(AssetId, StartDate, EndDate)`  
- `(AssetId, Status)` filtered where blocking  
- `(RenterId, CreatedAtUtc DESC)`  
- `(HostId, Status)`  
- `(ExpiresAtUtc)` where status in pending  
- Exclusion / range constraint for blocking overlaps (PostgreSQL)

## Sign-off

- [ ] Table list approved  
- [ ] Payment separate table approved
