# EHUB-600 — Payment Business Rules

**Status:** DRAFT — awaiting Architect review.  
Change only via explicit decision note.

## Core principle

> Money is authoritative on the platform, not on the client or the provider. The **amount comes from the Booking `TotalPrice` snapshot**, the **effect comes from a verified provider webhook**, and the **Booking confirms only after Payment succeeds**.

---

## BR-PAY-001 — Amount source of truth (L1)

- Payment `Amount` + `Currency` are copied from the **Booking `TotalPrice` snapshot** at Payment creation.
- The client **never** supplies the amount. Any amount in the request body is ignored (or rejected as `validation_failed`).
- If the provider reports a **different** captured amount than the Payment `Amount`, the webhook is treated as a mismatch failure (see F-PAY-11) — never silently accepted.

---

## BR-PAY-002 — One active Payment per Booking

- A Booking has **at most one** non-terminal Payment at a time.
- Re-initiating payment while a `Pending`/`Authorized` Payment exists returns the **existing** Payment (idempotent), it does not create a second row.
- A new Payment may be created only if the previous one is terminal-non-success (`Failed`, `Cancelled`, `Expired`) **and** the Booking is still `PendingPayment` with an active hold.

---

## BR-PAY-003 — Confirm only after Succeeded (L3)

- Booking transitions to `Confirmed` **only** when its Payment reaches `Succeeded`.
- The signal travels **Payment → Outbox → Booking** (`PaymentSucceeded` event); Booking never reads Payment state synchronously.
- `Authorized` (auth-only) is **not** sufficient to confirm — capture must complete to `Succeeded` first (see BR-PAY-006).

---

## BR-PAY-004 — Payment window / expiry (L4)

| Concern | Rule (v1 locked candidate) |
|---------|----------------------------|
| Window | Payment must succeed within the **Booking Payment TTL = 15m** (starts at Booking Approve / Instant Book create). |
| On timeout | Payment → `Expired`; Booking → `Expired` (via booking expire job); Hard Hold released. |
| Late callback | A `Succeeded` callback arriving **after** the Booking is `Expired`/terminal **must NOT** confirm it. Payment records the success, raises a **reconciliation/auto-refund** signal, Booking stays terminal. |

> The payment provider session lifetime is configured to align with (or shorter than) the 15m Booking TTL to minimise the late-callback race.

---

## BR-PAY-005 — Provider webhook is the source of effect

- The **user's browser return URL is advisory only** (UX redirect). It never changes Payment status.
- Only a **verified provider webhook** (BR-PAY-008) moves a Payment to `Succeeded` / `Failed` / refund states.
- Polling the provider (reconciliation) is an allowed fallback but obeys the same effect rules and idempotency.

---

## BR-PAY-006 — Authorize vs capture

- v1 default flow is **immediate capture** (`Pending → Succeeded`).
- Auth-then-capture is supported by the model (`Authorized` state) for future deposit/hold flows but is **not** wired to Booking confirm in v1.
- Capture failure after authorize → `Failed` with `FailureReason`.

---

## BR-PAY-007 — Idempotency (L2)

- Every outbound provider create-payment call carries a stable **`IdempotencyKey`** (BR-PAY-002 uniqueness: one per Payment attempt).
- Every inbound webhook is deduplicated so the **same webhook is processed once** — replays/retries are acknowledged but produce no second effect (see [06-idempotency.md](06-idempotency.md)).

---

## BR-PAY-008 — Signature verification (L6)

- Every inbound webhook request **MUST** pass provider signature verification **before** any parsing-into-domain or side effect.
- Failed signature → `401` and **no** state change, **no** inbox row committed as processed.
- Verification uses provider-specific secret + scheme, isolated in the provider adapter (BR-PAY-009).

---

## BR-PAY-009 — Anti-corruption / adapter (L8)

- Provider payloads (create response, webhook body, error codes) are **never** persisted or bound directly into the Payment domain model.
- A per-provider **adapter** maps provider concepts → a normalized internal result (`ProviderPaymentResult`) before the domain reacts.
- Raw provider payloads may be stored **as audit only** (opaque JSON in an event/inbox row), not as domain fields.

---

## BR-PAY-010 — Separate aggregates (L7)

- Payment and Booking are **separate aggregates**. Payment holds `BookingId` (id-only); Booking holds `PaymentId?` (id-only).
- No shared transaction over both aggregates' domain state; consistency between them is **eventual**, via Outbox (L9).

---

## BR-PAY-011 — Refund is a separate operation (L5)

- A refund does **not** mutate the original charge in place. It is a distinct, audited operation producing a `Refund` record + status change (`PartiallyRefunded` / `Refunded`).
- Refund amount must be `> 0` and `≤ (Amount − RefundedAmount)`.
- Refund policy (how much, when) is owned by Booking cancellation rules (BR-BKG-014) and passed to Payment as an instruction. See [07-refund-strategy.md](07-refund-strategy.md).

---

## BR-PAY-012 — Audit + timeline from day one

- Every status transition writes a `PaymentStatusHistory` row (from, to, actor/system, at, reason).
- Every provider interaction (create, webhook, refund) writes a `PaymentAttempt`/audit row with the normalized result.
- Money and state changes are never silent.

---

## BR-PAY-013 — Currency

- Payment `Currency` mirrors the Booking money `CurrencyId`. Cross-currency conversion is out of scope for v1.
- A webhook reporting a different currency than the Payment → mismatch failure (F-PAY-11).

---

## BR-PAY-014 — Time via IClock

- All timestamps (`PaidAtUtc`, expiry checks, history `AtUtc`) use `IClock` — never raw `DateTime.UtcNow` in domain/application logic (consistent with Booking).

---

## Decision log (DRAFT — 2026-07-19)

| Topic | Proposed decision |
|-------|-------------------|
| Amount source | Booking `TotalPrice` snapshot — client price ignored |
| Confirm trigger | `PaymentSucceeded` via Outbox only |
| Payment TTL | 15m aligned to Booking Payment TTL |
| Late callback | Never confirms terminal Booking → reconcile/auto-refund |
| Capture model | Immediate capture v1; auth/capture reserved |
| Webhook | Signature verified + idempotent, source of effect |
| Aggregates | Separate, id-only refs, eventual via Outbox |
| Refund | Separate audited operation, ≤ remaining amount |
| Anti-corruption | Adapter → normalized `ProviderPaymentResult` |

## Sign-off

- [ ] Amount-from-Booking + confirm-after-succeeded approved
- [ ] Payment TTL + late-callback rule approved
- [ ] Aggregate boundary + refund model approved
