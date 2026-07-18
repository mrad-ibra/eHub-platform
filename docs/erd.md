# ERD (current + planned)

Logical model. Persistence is still in-memory; this is the target relational shape.

## Current

```mermaid
erDiagram
    User ||--o{ RefreshToken : has
    User ||--o{ LoginHistoryEntry : has
    User ||--o{ UserRole : has
    Role ||--o{ UserRole : grants
    Role ||--o{ RolePermission : has
    Permission ||--o{ RolePermission : granted_by

    Category ||--o{ SubCategory : contains
    Brand ||--o{ Model : contains
    Country ||--o{ City : contains
    City ||--o{ District : contains

    User ||--o{ Asset : owns
    Category ||--o{ Asset : classifies
    Asset ||--o{ AssetMediaItem : has
    Asset ||--o{ AssetAvailabilityBlock : blocks
    Asset ||--o{ AssetTag : tagged
    Asset ||--o{ AssetFeature : features
    Asset ||--o{ AssetVersionEntry : history

    User {
        guid Id PK
        string Email
        bool IsEmailConfirmed
        bool IsActive
        bool IsDeleted
    }

    Asset {
        guid Id PK
        guid OwnerId FK
        guid CategoryId FK
        guid SubCategoryId FK
        guid BrandId FK
        guid ModelId FK
        string Title
        string StatusCode
        int VersionNumber
        bool IsDeleted
    }

    AssetMediaItem {
        guid Id PK
        guid AssetId FK
        string Kind
        string Url
        bool IsPrimary
    }
```

Catalog dictionaries (Currency, FuelType, AssetStatus, …) are flat lookup tables keyed by `Code`.

## Planned (Booking sprint)

```mermaid
erDiagram
    Asset ||--o{ Booking : booked_as
    User ||--o{ Booking : renter
    Booking ||--o{ Payment : pays
    Booking ||--o{ OutboxMessage : emits

    Booking {
        guid Id PK
        guid AssetId FK
        guid RenterId FK
        datetime StartUtc
        datetime EndUtc
        string Status
    }

    Payment {
        guid Id PK
        guid BookingId FK
        decimal Amount
        string Status
    }
```

**Rules:** Payment is its own aggregate. Booking owns availability conflict checks; use transactions + optimistic concurrency; side effects via outbox/domain events.
