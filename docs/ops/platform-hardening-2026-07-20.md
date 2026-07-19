# Platform hardening notes (2026-07-20)

Architect review follow-ups applied:

| Item | Action |
|------|--------|
| Auth rate limiting | `RateLimiting:Auth` + `[EnableRateLimiting("auth")]` on login / verify / resend / forgot / reset |
| AggregateVersion | `ApplyTransition` / `MarkChanged` on Booking & Payment (not tied to `Raise`) |
| PreparationBufferDays | `AssetRentalRules.PreparationBufferDays` → CreateBooking snapshot |
| CORS | `Cors:AllowedOrigins` — empty disables; Dev allows localhost:3000 |
| Renter overlap | Decision pending — [15-renter-overlap-decision-pending.md](../booking/15-renter-overlap-decision-pending.md) |
| Catalog interim ADR | Backlog fields added — [0005](../adr/0005-catalog-module-interim.md) |
