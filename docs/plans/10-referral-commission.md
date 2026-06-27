# Referral commission

Pay a referrer a share of the **platform fee** earned when a user they referred completes a
card **buy or top-up**, credited to the referrer's wallet balance (spendable on their own
top-ups). This wires up the earn side of a commission feature whose data model and dashboard
were previously scaffolded but never paid out.

## 1. What it does

When a referred user's card buy/top-up is **confirmed successful**, the referrer earns
`rate × fee` (default **10% of the fee**, not the gross amount). The credit lands on the
referrer's `tblM_User_Balance` via the verified `WalletService` credit path and is recorded in
`tblT_Commission` for the dashboard "Total Commission" / "Commission history".

Worked example: a referee tops up $100 → platform fee is 2.5% = **$2.50** → referrer earns
**$0.25**; the platform keeps $2.25. Because the payout is a share of the fee (never the gross),
it can never cost more than the platform made.

## 2. Money flow it plugs into

The platform earns only on **card open** (a flat card price + a recharge fee) and **card top-up**
(a recharge fee); crypto deposits and card spending earn nothing. The referral payout is funded
out of that recharge-fee margin — hence the commission is computed against the **fee**, and is
**capped at the fee** so a single payout can never run at a loss.

## 3. Design

- **Earn hook:** `CardFinalizationService.FinalizeOpenSuccess` / `FinalizeTopUpSuccess`
  (`QryptoCard.INT.Callback`) — the single shared confirmed-success path (webhook +
  reconciliation sweep), status-guarded so it runs once per order. Each calls
  `ReferralCommissionService.PayForFinalizedSpend(refereeUserId, fee, orderId)`.
- **Referrer:** the referee's `tblM_User.InvitedBy` (the referrer's UserID, bound at
  registration from the referral code). Self-referral is guarded.
- **Rate:** the referrer's `tblM_User_Commission` rate (a fraction, default `0.1`), falling back
  to `tblM_Setting` ID 2, then a `0.1` constant. A rate outside `[0,1]` is logged as a likely
  percent-vs-fraction misconfiguration (the cap keeps it loss-proof regardless).
- **Math:** `ReferralMath.Commission` (in `QryptoCard.Sec`, pure / EF-free so it is unit-tested
  in isolation): `min(round(rate × fee, 2, AwayFromZero), fee)`, floored at 0; non-finite or
  non-positive inputs pay nothing.
- **Credit + idempotency:** `WalletService.CreditReferralCommission` (INT + Callback copies, in
  parity) does an atomic dedup+credit via the shared `Mutate` — ledger Type `"Referral
  Commission"`, deduped on the referee order id in `tblH_Partner_Webhook_ID`
  (Type `"ReferralCommission"`). One payout per order; a redelivered/raced finalize rolls back as
  `duplicate_event`.
- **Isolation:** the whole payout is best-effort and wrapped so it can never throw or roll back
  the card finalization (the money-critical path); failures are logged, not swallowed silently.

### Properties

- **Loss-proof** — payout capped at the fee earned; unit-tested across misconfigured rates.
- **No double-pay** — per-order dedup unique index (see §4) + status-guarded finalize.
- **No claw-back gap** — pays only on confirmed success; failed/abandoned spends never pay, and
  there is no post-success reversal path that would need to reverse a commission.
- **Abuse-resistant** — earning requires the referee to pay a real fee and the referrer gets only
  a fraction of it, so farming costs more than it earns; self-referral blocked.

## 4. Deploy prerequisite (load-bearing)

The no-double-pay guarantee depends on a **filtered unique index**,
`deploy/sql/migrations/0004-referral-commission-dedup-index.sql`
(`UNIQUE (TXID) WHERE Type = 'ReferralCommission'`). It is additive and idempotent, and is applied
by `vm-migrate.ps1` as one of the ordered schema migrations.

As a backstop, `ReferralCommissionService` checks the index exists before any payout and
**fails closed** (logs and skips, no payout) if it is missing — so a deploy that lands the code
before the index can never double-pay; payouts simply do not run until the index is present.

## 5. Deferred / not built

- **KYC-gating** the payout (pay only after the referee passes identity verification) — validated
  in the live environment, not the sandbox.
- **Single-transaction display row** — `tblT_Commission` is currently written best-effort after
  the wallet credit; on a rare write failure the balance is still correct (the balance ledger is
  the source of truth) and the gap is logged. Folding it into the credit transaction is a future
  refinement.
- **Tunable rate UI** — the rate is config-driven (`tblM_User_Commission` / `tblM_Setting`); an
  admin control to adjust it is not built.

## 6. Tests

- `ReferralMathTests` (unit) — the loss-proof cap, rounding, and non-finite/non-positive guards.
- `ReferralCommissionDedupTests` (integration) — credits once, replay dedupes (no double-pay),
  independent orders pay independently, non-positive fails closed; exercises the real dedup index
  via the INT `WalletService` copy (the Callback copy is byte-parity).
