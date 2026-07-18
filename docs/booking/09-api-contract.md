# EHUB-509 — API Contract (draft)

**Base:** `/api/v1/bookings`  
**Auth:** JWT unless noted.  
**Errors:** RFC 7807 ProblemDetails.  
**Idempotency:** `Idempotency-Key` required on `POST /`.

## Endpoints

| Method | Path | Actor | Description |
|--------|------|-------|-------------|
| `POST` | `/bookings` | Renter | Create request |
| `GET` | `/bookings/{id}` | Renter/Host/Admin | Detail |
| `GET` | `/bookings/mine` | Renter | My bookings |
| `GET` | `/bookings/host` | Host | Incoming for my assets |
| `PATCH` | `/bookings/{id}/approve` | Host | POA → PendingPayment |
| `PATCH` | `/bookings/{id}/reject` | Host | POA → Rejected |
| `PATCH` | `/bookings/{id}/cancel` | Renter/Host | → Cancelled |
| `PATCH` | `/bookings/{id}/complete` | Host/System | InProgress → Completed |
| `PATCH` | `/bookings/{id}/extend` | Renter (host confirm?) | Extend end date |
| `POST` | `/bookings/{id}/start` | System/Host | Confirmed → InProgress |

Payment initiation may be `POST /payments` scoped by `bookingId` (Payment module) — not owned by Booking controller beyond events.

## POST /bookings

### Request

```json
{
  "assetId": "uuid",
  "startDate": "2026-08-01",
  "endDate": "2026-08-05",
  "driverRequested": false,
  "deliveryRequested": false,
  "pickup": { "useAssetLocation": true },
  "dropoff": { "useAssetLocation": true },
  "notes": "optional"
}
```

Headers: `Idempotency-Key: <string>`

### Response `201`

```json
{
  "id": "uuid",
  "status": "PendingOwnerApproval",
  "assetId": "uuid",
  "startDate": "2026-08-01",
  "endDate": "2026-08-05",
  "total": { "amount": 500.00, "currencyId": "uuid" },
  "expiresAtUtc": "2026-08-02T12:00:00Z"
}
```

### Errors

| Status | Code | When |
|--------|------|------|
| 400 | validation_failed | Bad dates |
| 404 | asset_not_found | |
| 409 | booking_conflict | Overlap |
| 409 | concurrency_conflict | Version race |
| 403 | booking_own_asset | Renter is host |

## PATCH .../approve

Empty body or `{ "note": "..." }` → `204` / booking DTO.

## PATCH .../reject

```json
{ "reason": "Vehicle unavailable that week" }
```

## PATCH .../cancel

```json
{ "reason": "Plans changed" }
```

## PATCH .../extend

```json
{ "newEndDate": "2026-08-08" }
```

## Feature ownership

Request/response DTOs live under Application `Bookings/` vertical slices — **not** in `eHub.Contracts`.

## Sign-off

- [ ] Route list approved  
- [ ] Idempotency header approved
