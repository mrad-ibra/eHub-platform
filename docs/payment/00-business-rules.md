# EHUB-600 — Payment Business Rules

**Status:** READY FOR ARCHITECT REVIEW  
Change only via explicit decision note after APPROVED.

## Core principle

> Money is authoritative on the platform. The **amount comes from the Booking `TotalPrice` snapshot**, the **effect comes from a verified provider webhook**, and the **Booking confirms only after Payment succeeds** — and only while the hold is still active.

---

## BR-PAY-001 — Amount source of truth (L1)

- Payment `Amount` + `Currency` are copied from the **Booking `TotalPrice` snapshot** at Payment creation.
- The client **never** supplies the amount. Any amount in the request body is **rejected** (`validation_failed` / ignored — API contract: reject if present).
- If the provider reports a **different** captured amount/currency than Payment `Amount`, treat as mismatch (F-PAY-11) — never silently accept.

---

## BR-PAY-002 — One active Payment per Booking

- A Booking has **at most one** non-terminal Payment at a time (`Created` / `Pending` / `Authorized`).
- Re-initiate while active → return **existing** Payment (idempotent), do not create a second row.
- A **new** Payment may be created only if the previous one is terminal-non-success (`Failed`, `Cancelled`, `Expired`) **and** the Booking is still `PendingPayment` with an **active** Hard Hold.

---

## BR-PAY-003 — Confirm only after Succeeded (L3)

- Booking → `Confirmed` **only** when Payment reaches `Succeeded` **and** Booking is still `PendingPayment` with an active hold.
- Signal path: Payment → Outbox → `PaymentSucceeded` → Booking consumer (L9). Booking never polls Payment synchronously.
- `Authorized` alone does **not** confirm (BR-PAY-006).

---

## BR-PAY-004 — Payment window / expiry (L4, L10)

| Concern | Rule |
|---------|------|
| Window | Must succeed within **Booking Payment TTL = 15 minutes** (starts at Approve / Instant Book). |
| On timeout | Payment → `Expired`; Booking → `Expired` (expire worker); Hard Hold released. |
| Late callback | `Succeeded` webhook after Booking is `Expired`/terminal → **must NOT confirm**. Record + reconcile → **auto-refund** (see BR-PAY-011 / [07](07-refund-strategy.md)). |
| Provider session | Configured ≤ Booking TTL to reduce late-callback race. |

---

## BR-PAY-015 — Failed payment must not stick forever (L10)

- Payment → `Failed` does **not** freeze the Booking forever in `PendingPayment`.
- Booking Hard Hold / 15m TTL **continues**; expire worker still releases the calendar if unpaid.
- While Booking remains `PendingPayment` + hold active, renter **may retry** (new Payment after previous is `Failed`/`Cancelled`/`Expired` — BR-PAY-002).
- UX: notify failure; do not imply infinite wait.

---

## BR-PAY-005 — Provider webhook is the source of effect

- Browser return URL is **advisory only** (UX). It never changes Payment status.
- Only a **verified** webhook (BR-PAY-008) moves Payment to `Succeeded` / `Failed` / refund settlements.
- Reconciliation poll is allowed fallback; same effect + idempotency rules.

---

## BR-PAY-006 — Authorize vs capture

- v1 default: **immediate capture** (`Pending → Succeeded`).
- `Authorized` reserved for future deposit/auth-capture; **not** wired to Booking confirm in v1.
- Capture failure after authorize → `Failed` + `FailureReason`.

---

## BR-PAY-007 — Idempotency (L2)

- Outbound create/refund: stable idempotency keys (see [06](06-idempotency.md)).
- Inbound webhook: same event processed **once**; replays ack with no second effect.

---

## BR-PAY-008 — Signature verification (L6)

- Every webhook **MUST** pass signature verification **before** parse-into-domain or any side effect.
- Failed signature → `401`; **no** state change; **no** inbox row marked processed.
- Verification lives in the provider adapter (BR-PAY-009).

---

## BR-PAY-009 — Anti-corruption / adapter (L8)

- Provider payloads are **never** bound into the Payment domain model.
- Adapter maps → normalized `ProviderPaymentResult` / `ProviderRefundResult`.
- Raw JSON may be stored **audit-only** (opaque), not as domain fields.

---

## BR-PAY-010 — Separate aggregates (L7)

- Payment holds `BookingId` (id-only); Booking holds `PaymentId?` (id-only).
- No shared domain transaction across aggregates; consistency is **eventual** via Outbox (L9).

---

## BR-PAY-011 — Refund is a separate operation (L5)

- Refund does **not** rewrite the original charge. It creates a `Refund` child + updates `RefundedAmount` + status.
- **Partial refund:** `0 < RefundedAmount < Amount` → status `PartiallyRefunded`.
- **Full refund:** `RefundedAmount == Amount` → status `Refunded`.
- Guard: each refund amount `> 0` and `≤ (Amount − RefundedAmount)`.
- Amount *policy* owned by Booking cancellation (BR-BKG-014); Payment **executes** the instruction.
- Late success on expired booking → **system full auto-refund** (L4).

---

## BR-PAY-012 — Audit from day one

- Every transition → `PaymentStatusHistory`.
- Every provider interaction → `PaymentAttempt` (normalized result; raw optional audit).
- Money/state changes are never silent.

---

## BR-PAY-013 — Currency

- Mirrors Booking money `CurrencyId`. Cross-currency out of scope v1.
- Webhook currency ≠ Payment → mismatch failure (F-PAY-11).

---

## BR-PAY-014 — Time via IClock

- All timestamps / expiry checks use `IClock` (same as Booking).

---

## BR-PAY-016 — Who may cancel a Payment

| Actor | May cancel? | When |
|-------|-------------|------|
| **Renter** | **Yes** | Own booking; Payment in `Created` / `Pending` / `Authorized` (before capture). Requires `payments.cancel`. |
| **Host** | **No** | Host rejects/expiry via **Booking** lifecycle (`Reject` / expire worker). Hosts do **not** cancel Payment rows. |
| **Admin** | **Yes** | Fraud / dispute / ops; requires `payments.cancel` + Admin role. Same status guards as renter. |

Terminal payments (`Succeeded`, `Failed`, `Cancelled`, `Expired`, `Refunded`, …) reject cancel via domain guards.

---

## BR-PAY-017 — Who may refund a Payment (Sprint 6.4 MVP)

| Actor | May execute refund? | May read refunds? | Notes |
|-------|---------------------|-------------------|-------|
| **Renter** | **No** (MVP) | **Yes** | Own booking via `payments.read` + ownership check. Future: renter **request** workflow. |
| **Host** | **No** | **Yes** | Own booking via `payments.read` + ownership check. |
| **Admin** | **Yes** | **Yes** | Requires `payments.refund` + Admin role. Manual ops / dispute handling. |
| **System** | **Yes** | N/A | Auto full refund on late success (BR-PAY-011 / L4) — outbox/worker path, not HTTP. |

MVP HTTP refund is **Admin-only execute**. Host/renter read their payment's refund history when they hold `payments.read` or `payments.refund.read` and own the booking.

Idempotency: `PaymentId` + `Idempotency-Key` header (unique DB index). Same key + different payload → **409**.

---

## Decision log (proposed — 2026-07-19)

| Topic | Proposed decision |
|-------|-------------------|
| Amount | Booking snapshot; reject client amount |
| Confirm | Outbox `PaymentSucceeded` only + active hold |
| TTL | 15m = Booking Payment TTL |
| Failed | Terminal Payment; Booking TTL continues; retry allowed while payable |
| Late callback | No confirm → full auto-refund |
| Capture | Immediate v1; `Authorized` reserved |
| Webhook | Signature + inbox unique + single TX with outbox |
| Aggregates | Separate, id-only, Outbox |
| Refund | Separate rows; partial vs full derivation |
| Cancel actors | Renter self-service; Host never; Admin fraud/dispute (BR-PAY-016) |
| Refund actors | Admin execute (MVP); Renter/Host read; System auto (BR-PAY-017) |
| API concurrency | Payment DTOs omit `AggregateVersion`; future updates use **ETag** / `If-Match` |

## Sign-off

- [ ] L1–L10 / BR-PAY-001…015 approved
- [ ] Failed≠stuck + late-callback auto-refund approved
- [ ] Partial vs full refund rules approved
