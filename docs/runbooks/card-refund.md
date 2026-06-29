# Admin card refund runbook

How an operator refunds a WasabiCard, what the refund actually does, its safety
properties, and how to recover the rare stuck state.

## What it does

`POST v1/admin/card/refund` with `{ "OrderId": "<id>" }` (a card **open**-order id OR a
**top-up** order id — both resolve to the same physical card). The acting admin is taken
from the **bearer token**, never the body. The refund:

1. **Cancels the whole card** at WasabiCard (`cancelCard`). WasabiCard has no partial
   withdraw, so a refund is always a whole-card cancel — refunding from a top-up id cancels
   the same card as the open id.
2. **Credits the buyer's wallet** with the amount the cancel **actually returned** to the
   merchant wallet, capped at the card's pre-cancel available balance. Only the **unused**
   balance is recoverable — anything the cardholder already spent is gone. The platform fee
   (the 3% taken at purchase) is **not** refunded.
3. **Claws back any referral commission** paid on that card's orders (open + top-ups).

It finalizes **synchronously from the `cancelCard` response** — it does **not** depend on the
WasabiCard callback/webhook (deliberately unlike the buy flow, whose webhook dependence can
strand orders when no callback is registered).

## Authorization

- **Root admin (Owner) only**, deny-by-default. Every attempt — including refusals and errors
  — is audited to `tblH_Auth_Log` (`EventType = 'admin_card_refund'`).

## Deploy requirement (IMPORTANT)

Requires migration **`0010-card-refund-dedup-indexes.sql`** (filtered unique indexes that make
the no-double-refund guarantee real). Deploy with:

```
ENV=<env> ./deploy/deploy.sh update --with-schema
```

The refund **fails closed** (`dedup_index_missing`) and refuses to cancel anything if the
`UIX_tblH_Partner_Webhook_ID_CardRefund_TXID` index is absent.

## Money-safety properties

- **No double-refund:** a one-row claim (`success -> refund pending`) elects a single winner,
  and a per-card dedup index makes the buyer credit idempotent on the card number.
- **No credit on an unconfirmed cancel:** an ambiguous `cancelCard` (null / timeout / throw)
  leaves the order `refund pending` and **never credits**.
- **No money minted:** the credit is the cancel-**confirmed** returned amount, capped at the
  pre-cancel balance — spend in the read→cancel window (or a cancel fee) cannot over-credit.
- **Commission clawback fails closed:** if the referrer already spent the commission, the
  clawback is recorded as a shortfall (wallet never driven negative) and the buyer refund
  still completes.

## How to run

Authenticate to the admin API as a root admin (bearer token), then:

```
POST https://<api-admin-host>/v1/admin/card/refund
Authorization: Bearer <admin token>
Content-Type: application/json

{ "OrderId": "QRYCRDBUYxxxxxxxxxxxx" }
```

Success returns the refunded amount, the buyer's new balance, and how many commissions were
reversed. (The seed admin's credentials live in Key Vault `SEED-ADMIN-PASSWORD`; the prod
admin email is in `SEED-ADMIN-EMAIL`.)

## Stuck-state recovery

The refund persists the cancel-confirmed amount as a `CardRefundIntent` row **before** crediting,
so the rare failure modes are recoverable:

- **Cancel succeeded but the wallet credit failed** (e.g. a transient DB error) → the order is
  left `refund pending` with the intent persisted. Funds are safe back in the merchant wallet.
  **Re-running the same refund resumes** and completes the credit from the persisted amount
  (no second cancel) — idempotent on the card.
- **Cancel was never confirmed** (ambiguous result, no intent persisted) → re-running returns
  `refund_pending_unconfirmed`. This needs **manual review**: confirm with WasabiCard whether
  the card was actually cancelled before deciding to credit or revert. No funds moved.

## Verification

- The order moves `success -> refunded`; the card reads cancelled at WasabiCard (`getCardInfo`).
- The buyer's `tblM_User_Balance` increases by the refunded amount, with a `Card Refund` row in
  `tblH_User_Balance`.
- Any referrer's commission is reversed (`Referral Commission Reversal` ledger row); the
  `tblT_Commission` row's payout is offset.
- An `admin_card_refund` audit row exists in `tblH_Auth_Log`.
