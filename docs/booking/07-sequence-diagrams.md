# EHUB-507 — Sequence Diagrams

**Status:** APPROVED WITH MINOR CHANGES (Architect 2026-07-19). Soft Hold + 12h/15m TTLs.

## 1. Create booking (host approval)

```mermaid
sequenceDiagram
  actor Customer
  participant API as Booking API
  participant App as Application
  participant Avail as Availability Check
  participant Booking as Booking Aggregate
  participant Outbox
  participant Host as Owner Notify

  Customer->>API: POST /bookings + Idempotency-Key
  API->>App: CreateBookingCommand
  App->>Avail: CanBook(asset, period)
  Avail-->>App: OK / Conflict
  alt Conflict
    App-->>API: 409
    API-->>Customer: ProblemDetails
  else OK
    App->>Booking: Create → Soft Hold POA (+12h, Number, Snapshot)
    App->>Outbox: BookingCreated
    App-->>API: 201 BookingId + BookingNumber
    API-->>Customer: 201
    Outbox-->>Host: Notify host (async)
  end
```

## 2. Approve → pay → confirm

```mermaid
sequenceDiagram
  actor Owner
  actor Customer
  participant API
  participant Booking
  participant Payment
  participant Outbox
  participant Provider

  Owner->>API: PATCH .../approve
  API->>Booking: Approve → Hard Hold PP (start 15m timer)
  Booking->>Outbox: BookingApproved
  Outbox-->>Payment: Create pending payment
  Outbox-->>Customer: Pay link / notify
  Customer->>Provider: Pay
  Provider->>API: Webhook
  API->>Payment: Inbox + MarkPaid
  Payment->>Booking: Confirm(paymentId)
  Booking->>Outbox: BookingConfirmed
```

## 3. Expire pending

```mermaid
sequenceDiagram
  participant Job as ExpirePendingBookingsJob
  participant Booking
  participant Outbox

  Job->>Booking: Find ExpiresAtUtc < now (POA/PP)
  loop each
    Booking->>Booking: Expire()
    Booking->>Outbox: BookingExpired
  end
```

## 4. End-to-end modules

```text
Customer → Booking API → Availability → Booking Aggregate
                ↓
            Outbox → Payment → Provider
                ↓
            Outbox → Notification → Owner/Customer
```

## Sign-off

- [ ] Create sequence approved  
- [ ] Pay confirm sequence approved
