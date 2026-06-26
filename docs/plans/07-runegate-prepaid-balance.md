# Plan 7 — Runegate Prepaid Balance (deposit → wallet → spend on cards)

> **Status: ✅ CORE SHIPPED** (as of 2026-06-26). The deposit → wallet-credit → spend →
> ledger core, the user read surfaces, the money-path security hardening, and the
> reconciliation sweep all merged — PRs #16/#19 (core), #20/#22 (durability), #26/#29/#31
> (reconciliation sweep + trigger + endpoint hardening). The per-task checkboxes below are
> flipped to match the merged code (verified, not assumed). What remains is unchecked.
>
> **Remaining — code:**
> - [ ] **T3.4** — idempotency key on the spend endpoints (double-submit guard)
> - [ ] **T4.5 / T4.6** — user wallet dashboard + admin read-only balance/ledger view (the read *endpoints* T4.1–T4.3 are done; the UI screens are not)
> - [ ] **T5.6** — fix `PGCryptoModel.Commision` → `Commission` (ledger-cosmetic)
> - [ ] **T5.3 / T5.7 / T5.8 / T5.4 / T5.10** — optional DRY refactors (shared PGCrypto + WasabiCard HTTP helpers, shared fee calculator, decimal money-type at the boundary, dead-code deletion) — fee math is still inline in ~4 places
> - [ ] **One-time backfill** — `EnsureUserProvisioned` sweep over pre-feature users (new users self-provision; this is belt-and-braces)
>
> **Remaining — on-box / shakeout (no code):**
> - [ ] **T1.4** — apply the wallet/dedup unique indexes on the live DB (scripts exist + wired into deploy; the protection isn't live until a deploy runs them)
> - [ ] **T2.7** — operational cutover: drain/cancel open `Created` orders + brief new-order freeze (the legacy direct-to-card credit path is already removed in code)
> - [ ] **T7.2** — run the tiered dev-shakeout E2E once schema is applied
> - [ ] **Ops** — set a strong `SCHEDULER_SHARED_SECRET` before the next API.Callback deploy (now in the startup Preload); set the Runegate merchant deposit commission to ~0; extend the Plan 3 forensic queries to the new ledger
>
> *Original plan intro (for context):* a reusable prepaid balance funded by Runegate
> deposits and spent on KashCards, refactoring the money path it touches and auditing
> the security of that path.

## Overview (plain English)

Today, paying for a KashCard is **one crypto deposit per card action**. When a user
opens or tops up a card, the system shows them a deposit address and an exact amount;
the user sends that exact amount; Runegate (the `api.runegate.co` / "PGCrypto"
crypto gateway) detects it and fires a signed webhook; the webhook matches the
deposit to that specific card order **by address + exact amount** and provisions or
funds the card directly through WasabiCard. There is no stored balance — every card
action starts from zero and waits on a fresh on-chain payment.

We want to add a **prepaid balance** (a "wallet"):

1. **Deposit** — each user has a **persistent Runegate deposit address**. They can
   send crypto (USDT) to it at any time, in any amount, as often as they like.
2. **Credit** — when Runegate reports a deposit to that address, we **credit the
   user's prepaid balance** (instead of trying to match it to one specific card
   order).
3. **Spend** — the user opens cards and tops them up by **spending their prepaid
   balance**. No waiting on a new on-chain payment per action; the balance is debited
   instantly and the card is funded through WasabiCard.

The happy result: deposit once, hold a balance, spend it across many cards.

### The pleasant surprise: the scaffolding is already half-built

The data model for this already exists and is wired up at registration — it's just
**disconnected**. Every user, on registration
(`QryptoCard.INT/Script/Service/App/v1/UserV1Service.svc.cs` ≈ L198–252), already
gets:

- a **wallet row** — `tblM_User_Balance` (Currency `USDT`, `Balance = 0`), and
- a **persistent per-user Runegate static deposit address** —
  `tblM_User_Crypto_Deposit`, created via
  `PGCryptoService.addressStaticCreation()` (a real on-chain TRC20/USDT address in
  prod; a synthetic `T…` address in dev).

But nothing **funds** the wallet from a deposit, and nothing **spends** it on a card:

- The PGCrypto webhook
  (`QryptoCard.INT.Callback/Service/v1/CallbackV1Service.svc.cs`, `PGCrypto(...)`)
  only matches a deposit to a **card order** (`tblT_Card` / `tblT_Card_Deposit`) by
  `Address + Total + Created` and goes **straight to WasabiCard** — it never touches
  `tblM_User_Balance`.
- Card open / top-up (`CardV1Service.openCard`, ≈ L265) reuses the user's static
  address as a **per-order** payment address and waits for that exact deposit.
- The wallet is only ever written on the **refund** path (a failed Wasabi deposit
  credits `tblM_User_Balance`). It is never funded by a deposit and never spent.

So this plan is mostly **connecting two stubs that already exist** — plus the
money-safety rigor that connecting them demands.

### Current flow vs. target flow

```
CURRENT (deposit-per-action, no stored balance)
  open/top-up card ─▶ show static addr + exact amount ─▶ user pays exact amount
       ─▶ Runegate webhook matches addr+amount to the order ─▶ WasabiCard card funded
  (tblM_User_Balance is bypassed; always 0)

TARGET (prepaid balance)
  user pays any amount, any time ─▶ Runegate webhook ─▶ CREDIT tblM_User_Balance
       (ledger row in tblH_User_Balance)
  open/top-up card ─▶ DEBIT tblM_User_Balance ─▶ WasabiCard card funded
       (no fresh on-chain payment required)
```

## What exists vs. what's missing

| Capability | Today | This plan |
|---|---|---|
| Per-user persistent Runegate deposit address | ✅ created at registration (`tblM_User_Crypto_Deposit`) | Expose it to the user; make lifecycle robust |
| Per-user wallet row | ✅ created at registration (`tblM_User_Balance`, always 0) | Becomes the live prepaid balance |
| Ledger table | ✅ exists (`tblH_User_Balance`, append-only history) | Becomes the system of record for every credit/debit |
| **Credit wallet from a Runegate deposit** | ❌ webhook matches deposits to card orders only | **New webhook branch: deposit-to-static-address → credit balance** |
| **Spend wallet on card open / top-up** | ❌ card actions require a fresh per-order deposit | **New payment mode: debit balance → fund card via WasabiCard** |
| Show address / balance / history to user | ❌ no surface | Dashboard + API: deposit address (+QR), balance, ledger |
| Idempotent, race-safe money mutation | 🟡 order-claim pattern exists; wallet path absent | Single shared atomic credit/debit helper + dedup |

## Design decisions (sign-off gate)

Recommendations are defaults chosen to match the sister projects and the existing
money path. ✅ = I'll proceed with the recommendation unless you change it;
❓ = I need your input before building.

| # | Decision | Recommendation | Alternative | Status |
|---|---|---|---|---|
| R1 | Relationship to the current per-order direct-to-card flow | **Every deposit is a wallet credit; all card funding is from balance — from cutover.** No order-vs-wallet precedence: a deposit to a user's static address *always* credits the wallet, and card open/top-up *always* debits the wallet (Slice 3). The legacy "deposit exact amount → provision card directly" path (the `Address + Total + Created` match in `CallbackV1Service.PGCrypto`) is **removed**, not coexisted. **Transition:** any card orders left in `Created` (awaiting a direct deposit) are **drained/cancelled at cutover** — see the transition note under Slice 2. | Keep order-match precedence during a coexistence window | ✔ **decided: wallet-only; remove the legacy direct-to-card deposit path; drain `Created` orders at cutover** |
| R2 | What a deposit to a user's static address means | **It credits the prepaid balance.** A deposit to the persistent per-user address is a wallet top-up; the user then spends the balance. (Removes the fragile "exact amount per order" coupling.) | Keep address+amount order-matching as the primary path | ✅ |
| R3 | Wallet credit matching key | **Match by address → owning user** (`tblM_User_Crypto_Deposit.Address` → `UserID`), credit `tblM_User_Balance`, dedup on `PGCryptoID` (provider `TransactionID`). | Keep `Address+Total` matching (incompatible with arbitrary top-up amounts) | ✅ |
| R4 | System of record for balance | **`tblH_User_Balance` (append-only ledger) is authoritative;** `tblM_User_Balance.Balance` is a running cache updated in the same transaction. Every credit/debit writes a ledger row with `BalancePrevious`, `Amount`, `Balance`. | Trust the running total only | ✅ |
| R5 | Concurrency / double-spend safety | **One shared atomic mutation helper** doing a conditional `UPDATE … WHERE Balance >= @debit` (debit) / unconditional credit, 1-row-affected check, inside a transaction — reusing the atomic-claim pattern already in `CallbackV1Service`. No read-modify-write on balances. | Per-call ad-hoc EF read-modify-write | ✅ |
| R6 | Idempotency / replay | **Dedup every credit on `PGCryptoID`** (already the webhook dedup key) **+ a DB unique index** on the deposit/credit reference (the index is DB-gated, runs at shakeout). A replayed signed webhook credits at most once. | In-memory dedup only | ✅ |
| R7 | Money type | **`decimal` end-to-end** for balance math (`tblM_User_Balance`/`tblH_User_Balance` are already `decimal`). Bridge the `double`-typed deposit fields (`tblT_Card_Deposit.Amount/Total`) at the boundary; never do balance arithmetic in `double`. | Leave mixed `double`/`decimal` | ✅ |
| R8 | Networks / coins (v1) | **USDT on TRC20 only** (what `addressStaticCreation` + the webhook already assume). Structure the code so more networks/coins can be added, but don't build them now. | Multi-network day one | ✔ **confirmed against code:** TRC20/USDT is the *only* rail wired today (`UserV1Service` L213 `ne="TRC20"`; `CallbackV1Service` L415 `Symbol=="USDT"`) — R8 preserves the status quo, removes nothing |
| R9 | When the fee is charged | **Fee at spend, deposits credit at face value.** This *preserves today's behaviour*: the fee is already a **per-card-type fee** (`tblM_Card_Type.RechargeFeeRate`) computed at card open / top-up — `Fee = rate% × amount`, user pays `(CardPrice +) amount + Fee` gross, full `amount` lands on the card. So a wallet deposit credits face value (send $100 → balance $100), and the **existing** card-type fee is debited from the balance when a card is opened/topped-up. No new deposit fee is introduced. | Add a new top-up fee on the way in | ✔ **decided: fee at spend (unchanged from today); deposit credits face value** |
| R10 | Spend ordering vs. provider call | **Atomic debit-first, then provider, then reconcile** (resolved from runegate `createWithdrawal`): debit inside a `Serializable` transaction and commit **before** the WasabiCard call — no `BalanceHold` two-phase. On definitive provider failure, reverse the debit (compensating ledger row); on an **ambiguous** result (timeout/null) **do not auto-reverse** — leave it pending and reconcile via the WasabiCard webhook / cross-check (C3.b/C3.c). | Two-phase hold; or debit only after provider success | ✔ **decided: debit-first + reconcile-on-ambiguous** |
| R11 | Negative balance / overdraft | **Hard floor at 0**, enforced by the conditional `UPDATE` (R5). A debit that would go negative fails closed with a clear error; no path can drive a balance below zero. | Allow transient negative | ✅ |
| R12 | Minimum deposit / dust | **Keep today's card-action minimums unchanged; add only a small anti-dust floor on wallet credits.** Today minimums live on the *card action*, not the raw deposit: API tier hard-codes a **$20** floor on open/top-up; App tier uses per-card-type `DepositAmountMinQuotaForActiveCard`. Those stay as-is and govern spending. For the new credited surface, add a small **configurable floor (default $1)** on wallet credits, ledger-logging sub-floor deposits without crediting, to prevent dust/precision griefing. | Credit any amount / no deposit floor | ✔ **decided: card-action minimums unchanged; +$1 configurable anti-dust floor on credits** |
| R13 | Withdrawal of prepaid balance | **Out of scope for v1** (deposit-and-spend only). Note it as a future plan; do not build a crypto-out path now. | Include withdrawals | ✔ **confirmed:** no withdraw/cashout/payout method exists in kash-cards today (grep) — R13 preserves the status quo; a future withdrawal plan is where R16 step-up-auth returns |
| R14 | Schema changes | **Additive only**, shipped as an idempotent `deploy/sql/*.sql` script (same pattern as `create-token-tables.sql`); applied during the dev shakeout. Deltas: dedup unique index on the credit reference (`PGCryptoID`); **unique indexes `tblM_User_Crypto_Deposit(UserID, NetworkID)` + `tblM_User_Balance(UserID, Currency)`** (spike-confirmed missing — see T1.4); a wallet-credit ledger `Type`; optional `BalanceHold` usage. No backfill (other than the one-time address/wallet backfill in T1.2). | EDMX hand-edit / in-band migration | ✅ |
| R17 | Admin manual balance adjustment | **Out for v1 — read-only admin.** No write to `tblM_User_Balance` exists today (admins adjust commission/fee only); v1 keeps it read-only. A support-correction tool, if needed later, gets its own audited endpoint: role-gated (`isDeniedFinanceMutation` = Owner/Admin), writing a `tblH_User_Balance` ledger row through the same atomic helper (T5.1), never an ad-hoc field edit. | Add a manual credit/debit tool in v1 | ✔ **decided: read-only admin in v1** |
| R16 | Step-up auth on spend | **No step-up for v1** — spending balance opens/top-ups a card the user already owns (balance is non-withdrawable, R13), so it isn't a crypto-cash-out; this matches today (card actions ride the login bearer token, no re-auth). The sisters' per-transaction human approval (Fireblocks TAP) is for *crypto-out*, which doesn't apply here. Revisit when withdrawal (R13) is built. | Require OTP/2FA step-up before every spend | ✔ **decided: no step-up for v1** (revisit with withdrawal) |
| R15 | What amount a deposit credits (gross vs. net) | **Credit the webhook `Total` (net = `Amount − Commission`)** in code — never credit more than Runegate settled to the merchant (spike-confirmed in `runegate MONEY_FLOW.md`); record the commission in the ledger. **Ledger-amount convention (resolves the R15-vs-C6.d contradiction):** the ledger `Amount` column stores the **net balance delta**, *not* the gross deposit, so every row satisfies the canonical forensic tamper-check `BalancePrevious + Amount = Balance` (Plan 3 `tmp/forensics.sql`) with one uniform rule across credits, debits, refunds, and reversals. The commission is recorded in the separate `Commision` column and the **gross deposit is recoverable as `Amount + Commision`** — so nothing is lost. (This supersedes the earlier "record gross `Amount`" wording, which is incompatible with the deployed `BalancePrevious + Amount = Balance` invariant for any commissioned credit; the forensic query needs no commission-aware change.) **Product side:** Runegate's deposit commission is **admin-configurable per merchant** (`tblM_Company_Merchant_Commission`, `Dashboard.Admin/Setting/MerchantCommission.aspx`); **set the kash-cards merchant to 0/near-0** so deposits are face-value and kash-cards' only fee is the existing card `RechargeFeeRate` at spend (R9). Note: Runegate's admin UI currently enforces `0 < commission ≤ 100` (their Issue #489), so a *true* 0 needs either a direct DB set or a one-line relax of `CommissionInput.Validate` (we own Runegate). | Keep a deposit commission / credit gross | ✔ **decided: credit net in code; set Runegate merchant commission to 0/near-0** |

## Runegate gateway facts (confirmed from the gateway source)

These were the open unknowns; all four decisions above are now resolved against the
Runegate gateway itself (the `s16rv/runegate` codebase + its `CLIENT_API_SPEC.md` and
API docs). The wallet model is fully supported as designed:

- **Static address = a reusable per-customer deposit wallet, any amount.** The
  `v1/address/static/generate` endpoint is *"intended solely for deposit wallets …
  one address per customer per coin and they can transfer to this address for any
  amount."* This is exactly the wallet premise — no per-order invoice needed.
- **A deposit fires a signed webhook per transaction.** Runegate detects inbound
  funds and POSTs a webhook to the merchant's configured URL; transactions on a static
  address are independently listable (`v1/transaction/static`).
- **The webhook signature scheme matches kash-cards' verifier exactly.** Runegate
  signs the outbound webhook as `X-Runegate-Signature: t=<unix>,v1=hex(HMAC_SHA256(
  CallbackSigningKey, ts + "." + body))` — a 1:1 match with
  `QryptoCard.Sec/RunegateWebhookVerifier.cs`. (The RSA-SHA512/JWS scheme in
  Runegate's audit notes is for *inbound* custody-provider events, not the outbound
  merchant webhook.) Integration just needs the merchant `CallbackSigningKey` set on
  both sides (already an INTAKE item).
- **Runegate dedups webhooks server-side on `(externalReferenceId, status)`**, and
  kash-cards dedups on `PGCryptoID` (`TransactionID`) — belt-and-suspenders for R6.

## Sister-project callback patterns we adopt (runegate / qrypto-omni)

Runegate's `audit/bundles/loss-of-funds-webhook.md` is a money-webhook loss-of-funds
audit over the **same stack and lineage** as our PGCrypto handler — a directly reusable
reference. We adopt its hard-won patterns rather than re-deriving them:

- **The 10 money-flow invariants (I-1…I-10)** become the Slice 6 review checklist **+
  permanent regression tests** (their "armor even when already compliant" rule). Most
  relevant to the wallet credit: **I-1** signature gates all mutation (kash-cards already
  verifies before forward); **I-2** dedup at the DB layer (unique constraint, not
  check-then-insert); **I-3** dedup key is **per-event**, not per-transaction; **I-4**
  credit only on the specific status transition; **I-5** amount cross-checked, not
  body-trusted; **I-6** currency/coin matches our record; **I-7** user derived from our
  row, never the body; **I-8** balance read+write is atomic; **I-9** rejected events never
  credit.
- **Dedup via the existing-but-unused `tblH_Partner_Webhook_ID` table.** kash-cards
  already ships this table (entity + DbSet across the INT tiers) but **never wires it** —
  it dedups ad-hoc on `tblT_Card.PGCryptoID`. We wire it the house way: insert a row keyed
  on the event, backed by a **filtered unique index** (`WHERE Type='PGCrypto'`), and
  swallow the duplicate-key (`SqlException 2601`) as a no-op. Supersedes the
  filtered-index-on-ledger idea in C2.b.
- **Per-EVENT key (their F-0031a P0 lesson) — and why ours ended up bare.** The sister's
  dedup key is composite **`(TransactionID, Status)`** because *they* credit on status
  transitions, so collapsing PENDING+COMPLETED would skip a credit. **As built, our key is the
  `TransactionID` alone:** we gate the credit on `isPaid == 1` *before* any dedup row is
  written, so only the one confirmed event ever reaches dedup — and folding the free-form
  `Status` into the key would instead let a single confirmed deposit, redelivered with a
  different status string, write a distinct key and **double-credit**. Do NOT restore a
  composite `TransactionID|Status` key — see the comment in `WalletService.CreditDeposit`.
- **Atomic balance mutation (I-8 / their F-0066).** Runegate favours a `Serializable`
  transaction to match EF6 entity-tracking; kash-cards already has the **raw-SQL
  atomic-claim** idiom (the refund path), so we use atomic `UPDATE … WHERE … OUTPUT`
  (C2.c) — equivalent guarantee, no rowversion column, fits the existing code.
- **Cross-check, don't body-trust (I-5).** A wallet top-up has no pre-recorded amount to
  match against, so we **cross-check the webhook amount against Runegate's REST record**
  (`v1/transaction/static` / `v1/transaction/detail`, confirmed to exist) before
  crediting — this is T6.7, upgraded from "if exposed" to "recommended; endpoint confirmed."
- **Test shapes port directly:** `Concurrent_DuplicateEvent_CreditsOnce`,
  `SerializedReplay_SecondCallIsNoOp`, `SameReference_DifferentStatus_BothLand`,
  `DedupKey_NullComponent_RejectedNotDeduped`, `TwoDistinctWebhooks_BothLand` →
  Slice 2/7 tests.

Note (qrypto-omni): same `tblH_Partner_Webhook_ID` + `IsDuplicateKeyException` dedup
(their F-0031b) and HMAC-SHA256 verify via a `QryptoOmniSignatureVerifier` — confirming
this is the **house standard** for inbound money callbacks.

## Remaining open question

1. **Forensics overlap** — Plan 3 forensics looks for credits with no on-chain
   `Txhash`/`PGCryptoID`. The wallet introduces a new credited surface; confirm the
   forensic queries should be extended to the ledger (R4) before go-live. *(Not a
   blocker for build; a go-live checklist item.)*

## Slices

- **Slice 1 — Wallet & deposit-address foundation** (make the existing stubs first-class)
- **Slice 2 — Fund the wallet from Runegate deposits** (webhook credit branch) — *core*
- **Slice 3 — Spend prepaid balance on cards** (open / top-up debit) — *core*
- **Slice 4 — User-facing surfaces** (address + QR, balance, ledger; "pay from balance")
- **Slice 5 — Refactor the money path** (the explicit refactor thread)
- **Slice 6 — Security investigation & hardening** (the explicit security thread)
- **Slice 7 — Verification & red-team** (money surface → full model-diverse red-team)

---

## Slice 1 — Wallet & deposit-address foundation

Make the already-created wallet + static address first-class and reliable, so the
credit/spend slices have a solid base.

**Spike finding (drives the redesign below):** provisioning today lives in
`RegisterVerify` (`UserV1Service`), and runs **after** the account is already committed
active (`isActive/isVerified = 1` + the balance row saved ≈ L191–206). The two gateway
calls — `getCoin()` (≈ L212) and `addressStaticCreation()` (≈ L223) — are
**un-null-checked**; on a Runegate timeout/error they return `null`, the next line
throws, the method's `catch` swallows it, and the user is left **verified-and-active but
address-less while the verify response says "error"** — and a retry hits "session ended"
(`isVerify` already 1). So the fix is to *decouple* provisioning from verify, not to
patch the in-line failure.

- [x] **T1.1 — Decouple provisioning from OTP-verify.** Move wallet + deposit-address
  creation **out of the `RegisterVerify` transaction** into an idempotent ensure-step
  (T1.2). OTP-verify must always succeed once the code is valid; a gateway hiccup can
  never leave a half-provisioned account or return a spurious error. Remove the
  un-null-checked gateway dereferences from the verify path.
- [x] **T1.2 — Idempotent ensure-accessor.** `ensureWallet(userId)` /
  `ensureDepositAddress(userId)` that lazily provision on first access, **repair**
  users who registered before this feature or whose address creation failed, and never
  duplicate. Plus a one-time **backfill** pass over existing users at the shakeout.
- [x] **T1.3 — Single read accessor.** `getBalance(userId)` / `getDepositAddress(userId)`
  in the INT tier, replacing the ad-hoc `tblM_User_Crypto_Deposit`/`tblM_User_Balance`
  re-queries scattered across **4** sites (`App` + `API` `CardV1Service`, ≈ L274/752/302/845).
- [ ] **T1.4 — Uniqueness (DB-gated, ties to R14).** `init.sql` has **no unique
  indexes** on these tables (surrogate `ID` PK only). Add unique indexes
  `tblM_User_Crypto_Deposit(UserID, NetworkID)` and `tblM_User_Balance(UserID, Currency)`
  (active rows) so provisioning is race-safe via the constraint (not check-then-insert)
  and the Slice 2 credit-match resolves an address to **exactly one** wallet.
- [x] **T1.5 — Tests.** Unit-test the ensure/repair logic + accessors. Note: the
  **dev simulate-deposit harness** needed for D1.3 does not exist yet — the Smoke **T3
  lifecycle is a documented stub** (steps 2–4 are comments); the Slice 2 integration
  test is what implements it (signed callback against the synthetic dev address), so
  building the credit path also completes T3.

### Design concerns surfaced (deep dive)

- **C1.a — `RegisterVerify` is a 7-step non-atomic provisioning chain.** The method
  calls `db.SaveChanges()` **seven times** with no enclosing transaction
  (activate user → balance → deposit address → referral → commission), and the
  un-null-checked gateway calls (`getCoin` L212, `addressStaticCreation` L223) sit
  *mid-chain*. An NPE there (gateway timeout → `null`) is swallowed by the L291 catch,
  leaving the user **active + balance-row but missing address, referral, and commission**
  — and the verify response says "error". → **T1.1 widens:** decouple *all* post-verify
  provisioning into one idempotent `ensureUserProvisioned(userId)`; `RegisterVerify`
  only flips the OTP/verify gate.
- **C1.b — The 4 address-read sites change meaning under wallet-only.** `openCard`
  (≈ L265) and `createCardDeposit` (≈ L743) in the App tier, plus the API-tier twins
  (≈ L293/L836), today bail with *"No crypto address found for payment"* and stamp
  `x.Address = qry.Address`. Under R1 card actions gate on **balance**, not a deposit
  address → these 4 sites are reworked in Slice 3 (the static address becomes
  deposit-only; the error becomes "insufficient balance").
- **C1.c — DbContext lifetime.** Services hold a per-instance `DBEntities db` field
  (L20). Confirm the WCF `InstanceContextMode`; the ensure/credit helpers should use a
  short-lived context, not the long-lived field, to avoid stale tracked entities.
- **C1.d — Backfill cost.** Backfilling addresses calls `addressStaticCreation` once
  per existing user (a Runegate call each) — batch it, respect rate limits, idempotently
  skip users who already have an active address.
- **C1.e — Counter-ID race (adjacent).** Deposit IDs come from `tblM_Setting_Counter`
  read-increment-save (≈ L754–756) — racy under concurrency (duplicate IDs). Flagged for
  Slice 5 hardening since the spend path touches this code.

## Slice 2 — Fund the wallet from Runegate deposits *(core)*

The "deposit into an actual Runegate address" half. New branch in the verified
PGCrypto webhook handler.

**Spike findings (from the Runegate gateway source — drive the tasks below):**
- **Payload** = a `TransactionModel` (`runegate WebhookService.webhook`):
  `TransactionID, PaymentType, Symbol, Address, Amount, Commission, Total, isPaid,
  isActive, Status, …`. A static-address deposit is an *"UNKNOWN-External static-address
  receive"* that credits the merchant `bal.Balance += Total` where **`Total = Amount −
  Commission`** (`runegate MONEY_FLOW.md`). → **credit `Total` (net), not `Amount`** (R15).
- **`PaymentType` does not discriminate** order-payment from wallet-top-up — Runegate
  has no knowledge of kash-cards card orders, so *every* deposit kash-cards receives is
  the same static-receive type. Precedence must be kash-cards-side (T2.1).
- **Field-name bug:** Runegate sends `Commission`/`CommissionInPercentage`; kash-cards'
  `PGCryptoModel` has `Commision`/`CommisionInPercentage` (one `s`) → silently binds to
  `null`. Fix before reading commission into the ledger (also noted in Slice 5).
- **Status:** Runegate dedups on `<txHash>-<status>` and the payload carries
  `Status`/`isPaid`; the **existing** provisioning path never checks them. The credit
  branch must gate on the confirmed/settled status (T2.3).

- [x] **T2.1 — Wallet-credit branch (wallet-only, per R1).** In
  `CallbackV1Service.PGCrypto(...)`, a verified **confirmed** USDT deposit whose `Address`
  matches an active `tblM_User_Crypto_Deposit` row (→ owning `UserID`) **always credits
  that owner's wallet** — no order matching. `ensureWallet(userId)` (T1.2) first so a
  lazily-provisioned user has a wallet row. **Remove** the legacy `Address + Total +
  Created` card/deposit-order match (it becomes dead code once card funding is balance-only
  — see Slice 5 cleanup). Net effect: no order-vs-wallet ambiguity, and the old
  provision-without-status-check path is gone.
- [x] **T2.2 — Shared atomic credit (R5/R15).** Conditional `UPDATE tblM_User_Balance`
  + append to `tblH_User_Balance` (`BalancePrevious`, `Amount`, `Commission`, `Balance`,
  `Type = "Crypto Deposit"`, `TransactionID = PGCryptoID`) in one transaction; verify
  exactly 1 row affected. **Credit the webhook `Total` (net); record gross `Amount` +
  `Commission` in the ledger** for reconciliation.
- [x] **T2.3 — Confirmed-status gate + per-event idempotency (R6; adopts I-2/I-3/I-4).**
  Credit **only** on the confirmed/settled status transition (`isPaid`/`Status`),
  **exactly once**, via the existing `tblH_Partner_Webhook_ID` dedup table: insert a row
  keyed on the **provider `TransactionID` alone** (as built — the upstream `isPaid == 1` gate
  means only the confirmed event reaches dedup, so a `Status`-composite key would *enable* a
  double-credit rather than prevent one; see C2.b and `WalletService.CreditDeposit`), backed by
  a filtered unique index, and swallow the duplicate-key (`SqlException 2601`) as a no-op. A
  pre-confirmation or replayed delivery therefore cannot credit or double-credit. See
  "Sister-project callback patterns" above.
- [x] **T2.4 — Address-ownership safety + edge cases.** Credit strictly the user who
  owns the destination address; never infer the user from the webhook body. Edge
  handling: a deposit to an **unknown/inactive** address is ledger-logged + alerted,
  never credited; a deposit to a **banned/disabled** user's address is credited to the
  ledger but **held** (not spendable) pending review rather than dropped (the funds are
  real); a missing wallet row triggers `ensureWallet` before crediting.
- [ ] **T2.7 — Cutover transition (operational + code).** Remove the legacy direct-to-card
  deposit path (R1) and **drain open `Created` card/deposit orders at cutover**: cancel/
  expire them so a subsequent deposit credits the wallet (and the user re-opens the card
  from balance) rather than silently going unmatched. A short pre-cutover freeze on new
  `Created` orders avoids races. No money is lost — pre-cutover deposits already settled
  via the old path; only *unpaid* pending orders are drained.
- [x] **T2.5 — Min-deposit / dust** per R12 (anti-dust floor on the net credited amount).
- [x] **T2.6 — Tests.** Unit-test matching + credit (owned/unowned address, pending-vs-
  confirmed status, replay, min-deposit, net-vs-gross, decimal precision); DB-gated
  Integration test for verified-webhook → ledger credit (this implements the Smoke T3
  stub, per T1.5).

### Design concerns surfaced (deep dive)

- **C2.a — A failed credit is silently lost (no retry).** `CallbackV1Controller.pgcrypto`
  fire-and-forgets `sr.PGCrypto(...)` and **always returns 200**; the INT handler swallows
  every exception. A DB blip mid-credit → Runegate sees 200, never retries, and the
  deposit credit **vanishes** (real money). **Decision (signed off; refined against the
  sister gateways):** change the callback contract so the status code reflects the
  outcome, following the exact runegate / qrypto-omni house pattern rather than a blunt
  "non-200 on any failure":
    - **200** on a successful state mutation **and on a deduped duplicate** — a duplicate
      delivery is *not* a failure; the dedup row proves the prior atomic credit committed,
      so returning 200 stops the retraining safely (qrypto-omni rolls back + returns 200).
    - **401 / 400** on signature/validation failure *before* any mutation (forgery /
      malformed payload) — fail fast, no retry value.
    - **500 / 503** on an *operational* error (DB blip mid-credit, cross-check unreachable)
      → Runegate's retry schedule redelivers; T2.3 per-event idempotency makes the retry a
      no-op once the first attempt actually committed.
  Keep the `tblH_Partner_Webhook` journal for scheduled/manual replay/reconciliation. The
  key safety property (from the sisters): **dedup is a DB-level unique constraint +
  exception-swallow, never app-layer check-then-insert** — the constraint closes the
  concurrent-retry race the always-200 path silently lost money to. Add a sequential
  pre-check read of the dedup row (qrypto-omni QO-0081) for slow-200 / manual-resend
  redeliveries that aren't concurrent. *Confirmed convergent across both sisters:*
  runegate `PGCrypto.API.Callback/Controllers/v1/PaymentV1Controller.cs` (200-on-success /
  401-400-on-validation / 500-on-error) + `PaymentV1Service.svc.cs:174-186`
  (`IsDuplicateKeyException`); qrypto-omni `QryptoOmni.API/Controllers/v1/CallbackV1Controller.cs`
  + `QryptoOmni.INT/Security/SqlUniqueViolationDetector.cs`; rationale in runegate
  `audit/bundles/loss-of-funds-webhook.md` §18 (invariant I-2) and qrypto-omni
  `audit/fix-history/QO-0038.md` / `QO-0081.md`.
- **C2.b — Dedup via the existing dedup table (resolved by the sister pattern).** Rather
  than a filtered index on the ledger's `TransactionID` (which collides with refund rows
  that reuse it), use the **already-present `tblH_Partner_Webhook_ID` table** keyed (as built)
  on the **`TransactionID` alone** with a filtered unique index (`WHERE Type='PGCrypto'`)
  + `SqlException 2601` swallow — the runegate F-0031a/F-0031b house pattern, minus the
  `Status` component (our upstream `isPaid == 1` gate makes a `Status`-composite key a
  double-credit hazard, not a safeguard — see the per-EVENT note above and
  `WalletService.CreditDeposit`). See "Sister-project callback patterns." *(refines R14 / T6.1 / T2.3)*
- **C2.c — Atomic credit must capture before/after consistently.** Use SQL Server
  `OUTPUT` (`UPDATE … SET Balance = Balance + @amt OUTPUT deleted.Balance,
  inserted.Balance`) to read `BalancePrevious`/`Balance` for the ledger row atomically —
  no read-then-update race. *(refines T2.2)*
- **C2.d — Wrong-token / non-USDT deposit to the static address.** The handler processes
  `Symbol == "USDT"` only; another token sent to the (TRC20/USDT) address is real but
  unhandled. v1 policy: **log + alert for manual handling**, never silent-skip.

## Slice 3 — Spend prepaid balance on cards *(core)*

The "use that prepaid balance with KashCards" half. **Spike finding that reshapes this
slice:** `openCard`/`createCardDeposit` do **not** call WasabiCard today — they only
create a `Created` order and wait; the WasabiCard provisioning (`openCard`/
`openCardWithHolder`/`depositCard`, all **synchronous**, returning a model or `null` with
`code==200` = success) happens *in the callback* after the deposit lands. Under R1 (no
deposit wait), that provisioning **moves into the spend path**, and the legacy callback
provisioning block is removed (Slice 2 / T2.1).

The mechanic is **ported from runegate's `createWithdrawal`** — the closest analog
(debit-before-outbound), already red-teamed via F-0031d / F-0066 / F-0116.

- [x] **T3.1 — Spend path (open + top-up).** Add `openCardFromBalance` /
  `topUpFromBalance` (or a `payFromBalance` mode on `openCard`/`createCardDeposit`)
  replacing the static-address wait with a balance debit, then calling the
  **factored-out** WasabiCard provisioning step (extracted from the callback — C3.a). The
  4 address-lookup sites (C1.b) become balance checks; *"No crypto address found"* →
  *"Insufficient balance."*
- [x] **T3.2 — Atomic debit-first (adopts runegate `createWithdrawal` + I-8).** Inside a
  `BeginTransaction(Serializable)`: `UPDATE tblM_User_Balance SET Balance = Balance − @amt
  WHERE … AND Balance >= @amt OUTPUT deleted.Balance, inserted.Balance`; affected ≠ 1 →
  rollback + "insufficient" (R11 hard floor); else insert the `tblH_User_Balance` ledger
  row (`Type="Card Open"/"Card Topup"`, negative `Amount`, OUTPUT before/after). **Debit
  commits before the WasabiCard call** — never call the provider on an undebited balance.
  Same shared helper as the credit path (Slice 5 / T5.1).
- [x] **T3.3 — Provider outcome handling (definitive vs. ambiguous).** Clear success
  (`code==200`) → order `InProgress`, debit stands, the existing WasabiCard webhook
  finalizes to `Success`. Definitive failure (business error / known non-retryable code)
  → **reverse** the debit (compensating credit ledger row) + order `Failed`. **Ambiguous**
  (timeout / network `null`) → **do NOT auto-reverse** (would hand a free card if
  WasabiCard actually created it); leave the debit + a `PendingProvider` order and
  reconcile via the WasabiCard webhook or a sweep using `getCardInfo`/`getDepositOperation`
  — mirrors the existing `WebhookCrossCheckEvaluator` deposit-refund pattern.
- [ ] **T3.4 — Idempotency on spend (ports runegate `IdempotencyHash` / F-0031d+F-0116).**
  Spend endpoints take a client `IdempotencyKey` header (≤64, nullable for legacy); store
  `RequestBodyHash` (SHA-256 of client-supplied fields); filtered unique index +
  opportunistic pre-check + `IsDuplicateKeyException` race backstop that **rolls back the
  debit** and returns the cached result; body-hash mismatch → "IdempotencyKey reused with
  different body" (Stripe 422). Stops double-click / network-retry double-debit.
- [x] **T3.5 — Tests.** Unit: debit / insufficient / reverse / ambiguous-no-reverse
  branches. Integration (DB-gated): deposit-credit → open-from-balance → top-up-from-
  balance, asserting the ledger reconciles (`Σ credits − Σ debits == Balance`) and a
  double-submit debits once. Port runegate's `F0031d_IdempotencyKey_AttackTests` shapes.

### Design concerns surfaced (deep dive)

- **C3.a — Provisioning moves from callback to the synchronous spend path.** Factor the
  WasabiCard `openCard`/`openCardWithHolder`/`depositCard` block out of `CallbackV1Service`
  into a shared provisioning helper; the callback's deposit-triggered provisioning is
  deleted (R1). The card still finalizes via the WasabiCard `card_transaction` webhook,
  unchanged.
- **C3.b — Debit-before-provider ordering is load-bearing.** Per runegate's F-0066/H1
  fix: UPDATE (debit) first, commit, *then* call the provider — never provider-first (a
  crash mid-call would leave a card with no debit). The Serializable X-lock on the balance
  row serializes concurrent spends.
- **C3.c — Ambiguous result must not auto-reverse.** The single most dangerous spend bug:
  reversing a debit when the card was actually created = free card. Reconcile, don't
  auto-reverse (T3.3).
- **C3.d — Counter-ID race (C1.e) lives in this path.** `openCard` ID = `tblM_Setting_
  Counter[1]`, deposit = `[2]`, via read-increment-save (racy → duplicate IDs). Fix here
  or Slice 5; the idempotency key (T3.4) also mitigates double-submit.
- **C3.e — Card-type min/max still apply.** The per-card-type `DepositAmountMin/MaxQuota
  ForActiveCard` + the $20 API floor (R12) gate the *spend* amount, unchanged; balance
  sufficiency is an additional check.

## Slice 4 — User-facing surfaces

Mostly **extend**, not build — the spike found a lot already present (see deep dive).

- [x] **T4.1 — Balance read: extend the existing endpoint.** `POST /v1/user/balance`
  (`UserV1Service.getBalance`, Bearer, already IDOR-scoped by `UserID`) exists — point it
  at the live wallet; no new endpoint. Confirm it returns the running `Balance` + currency.
- [x] **T4.2 — Deposit-address read: NEW endpoint.** Add `getDepositAddress` reading
  `tblM_User_Crypto_Deposit` for the authenticated user (+ network/coin + QR-friendly
  payload), scoped `WHERE UserID == uid`; calls `ensureDepositAddress` (T1.2) so a
  not-yet-provisioned user gets one on first view. (No read endpoint exists today.)
- [x] **T4.3 — Ledger history: NEW endpoint.** Add `getLedger`/`getStatement` over
  `tblH_User_Balance` for the authenticated user, **paginated** (the card-trx list caps at
  "last 20"; the ledger needs paging), exposing `Type/Amount/Balance/Date/reference` and
  **hiding internal `BalanceID`**.
- [x] **T4.4 — `payFromBalance` on card open / top-up** (Slice 3 surface).
- [ ] **T4.5 — User dashboard (`QryptoCard.Dashboard`).** New wallet view: deposit
  address + QR (**reuse the existing `txdeposit.aspx` QR component**), live balance, and
  ledger history; add "pay from balance" to the card flows.
- [ ] **T4.6 — Admin read-only view (`QryptoCard.Dashboard.Admin`).** View a user's
  balance + ledger for support/reconciliation, **read-only**, mirroring the existing
  commission/fee admin pattern's role gate (`isDeniedAdminRead`). No balance *write* (R17).
- [x] **T4.7 — Smoke coverage.** Extend T2 (read: balance/address/ledger) and T3
  (mutation: credit → open-from-balance lifecycle, dev-only).

### Design concerns surfaced (deep dive)

- **C4.a — Reuse over build.** `getBalance` + the `txdeposit.aspx` QR component already
  exist; only `getDepositAddress` + `getLedger` (read) and the wallet page are net-new.
- **C4.b — Tiers (R-default).** User API + user Dashboard + read-only Admin. **Not** the
  Public/partner tier (APIKey identity) in v1 — the wallet is a user concept; partners
  keep managing cards via `/v1/card/balance`.
- **C4.c — Ledger exposure / IDOR.** Mirror the confirmed pattern (`getUserId(em)` →
  `WHERE UserID == uid`); never trust a body-supplied user/balance id; don't leak
  `BalanceID` or internal refs; page the results.
- **C4.d — Admin balance adjustment deliberately OUT (R17).** No write to
  `tblM_User_Balance` exists today; v1 keeps it that way. If support correction is needed
  later it gets its own audited, role-gated (`isDeniedFinanceMutation`), ledger-writing
  endpoint — not an ad-hoc field edit.

## Slice 5 — Refactor the money path *(refactor thread)*

Opportunistic, scoped to code this feature touches — not a blanket rewrite.

> **Sequencing (C5.b):** **T5.1 is a prerequisite, not cleanup** — Slices 2 and 3 both
> depend on the shared balance helper, so it is built **first** (right after Slice 1's
> accessor). The rest of Slice 5 (gateway dedup, type bridge, counter-race, spelling fix)
> stays as later cleanup.

- [x] **T5.1 — One atomic balance-mutation helper (a SAFETY fix, not just DRY).** The
  **only** existing balance mutation in the codebase is the refund path
  (`CallbackV1Service` L343–360): a **non-atomic read-modify-write**
  (`bal.Balance = bal.Balance + …`) on a row read with `FirstOrDefault()` and **no null
  check** (the carried T6.9 bug). Build one helper doing the atomic
  `UPDATE … WHERE [Balance >= @amt for debit] OUTPUT deleted.Balance, inserted.Balance`
  + ledger insert in one `Serializable` transaction, used by the credit (Slice 2), spend
  (Slice 3), **and** the refund path (migrate it through the helper — closes T6.9). The
  registration `.Add` (UserV1Service L205) and this are the only two balance-write sites,
  so the surface is fully bounded.
- [x] **T5.2** **De-duplicate `tblM_User_Crypto_Deposit` / balance lookups** behind the
  Slice 1 accessor (removes the ~4 copy-pasted queries across `App`/`API`
  `CardV1Service`).
- [ ] **T5.3** **De-duplicate the PGCrypto gateway HttpClient boilerplate.** Every
  method in `PGCryptoService.cs` (`addressStaticCreation`, `getCoin`, `getToken`,
  `addCustomer`, `createInvoice`) re-implements the same client setup, TLS pin,
  Basic-auth header, and `tblH_API_Log` write. Extract one helper; fix the
  **`HttpClient` not disposed** and the swallow-and-return-`null` error handling while
  there.
- [ ] **T5.6** **Fix the `PGCryptoModel` field-name mismatch** (spike-confirmed):
  `Commision`/`CommisionInPercentage` → `Commission`/`CommissionInPercentage` (with a
  `[JsonProperty]` alias if any in-flight payload used the old spelling) so the gateway's
  commission fields bind instead of silently deserializing to `null` — required before
  the ledger records commission (T2.2/R15).
- [ ] **T5.7 — WasabiCardService HTTP helper** *(mirror of T5.3; Slice 3 touches this
  gateway).* Its **7 methods** (`openCard`, `openCardWithHolder`, `depositCard`,
  `getCardInfo`, `getCardInfoSensitive`, `getCardTransaction`, `getDepositOperation`) each
  re-implement `new HttpClient()` + base URL + auth + logging + swallow-`null`, with the
  client undisposed. Extract one helper; dispose the client.
- [ ] **T5.8 — Shared fee calculator.** The fee math (`Fee = RechargeFeeRate/100 × amount;
  Total = …`) is copy-pasted across **4 sites** (App `openCard` L421, App `depositCard`
  L736, + the two API-tier twins) that Slice 3 reworks. Extract one calculator — a formula
  fix shouldn't need four edits on a money path.
- [x] **T5.9 — Atomic order-ID generation** (closes C1.e/C3.d). Replace the racy
  `tblM_Setting_Counter` read-increment-save (ID `1`=card, `2`=deposit) with an atomic
  increment (`UPDATE … OUTPUT` or a SQL `SEQUENCE`); concurrent opens can currently collide
  on the same ID.
- [ ] **T5.10 — Delete dead commented money-path code** in the methods we rewrite (the
  commented per-user-fee fallback in `openCard`/`depositCard`, the commented invoice block
  in `PGCrypto`, the commented deposit-balance block in `Wasabi`).

> **Noted but deliberately out of scope** (scope discipline — not a blanket rewrite):
> **`StatusModel`/`PGStatusModel`** duplicate constant classes are per-assembly copies, so
> dedup is structural, not a quick win — skip for v1. **Decomposing the giant
> `Wasabi`/`PGCrypto` callback methods** is avoided on the live money path pre-launch
> (beyond removing the PGCrypto provisioning branch per R1). The pervasive
> **`catch { op.Message = ex.Message }`** exception-text leak to clients is a real minor
> smell but not feature-specific → separate hardening pass.

### Design concerns surfaced (deep dive)

- **C5.a — The only existing balance mutation is unsafe** (refund path, non-atomic RMW +
  null-deref) → T5.1 replaces it and routes the refund through the atomic helper, closing
  T6.9. Only two balance-write sites exist (registration `.Add`, refund), so the surface
  is fully bounded.
- **C5.b — T5.1 is a prerequisite, built first** (Slices 2/3 depend on it) — see the
  sequencing note above.
- **C5.c — `PGCryptoModel` is a hand-written POCO** (not EDMX-generated) → the T5.6
  spelling fix is safe, no regeneration risk. The EDMX-generated entity classes
  (`tblM_User_Balance` etc.) must NOT be hand-edited; schema deltas go via `deploy/sql`
  (R14).
- **C5.d — The `double`↔`decimal` boundary is one place** (the credit/debit conversion);
  `double` on `tblT_Card*`/Wasabi models, `decimal` on balance/ledger. T5.4 centralizes it
  inside the helper; no balance arithmetic in `double`.
- **C5.e — Gateway-cleanup scope discipline (T5.3).** Disposing `HttpClient` is safe now;
  changing the swallow-`null`→throw error handling touches the registration gateway calls,
  so that behavior change is **coordinated with T1.1** (`ensureUserProvisioned` handles the
  throw) — not done in isolation.
- [ ] **T5.4** **Money-type consistency** (R7): make the deposit/credit/debit boundary
  convert `double`↔`decimal` in exactly one place; keep all balance arithmetic in
  `decimal`.
- [x] **T5.5** Confirm the refactor is behaviour-preserving via the existing Unit +
  Integration suites (and the new ones from Slices 2–3) before/after.

## Slice 6 — Security investigation & hardening *(security thread)*

The feature adds a **new credited money surface**, so it gets a focused review. Items
that turn out DB- or provider-gated are staged like the rest of the program (build the
code now; apply the index/migration at shakeout).

- [x] **T6.0** **Money-flow invariant checklist + regression armor (adopts I-1…I-10).**
  Walk the wallet credit + spend paths against the runegate loss-of-funds invariants
  (signature-gates-all-mutation, DB-layer per-event dedup, monotonic transition, amount
  cross-checked, currency/user from our row, atomic read+write, rejected-never-credits),
  and add a regression test per invariant **even where already compliant** (the house
  "permanent armor" rule). Port the runegate test shapes (see Sister-project patterns).
- [x] **T6.1** **Durable replay defense (R6; adopts I-2/I-3):** wire the existing
  `tblH_Partner_Webhook_ID` dedup table with a **filtered unique index** on the
  `TransactionID` alone (`WHERE Type='PGCrypto'`) + `SqlException 2601` swallow, so a
  replayed webhook cannot double-credit across process restarts. Ship the index as
  `deploy/sql/*.sql` (DB-gated); pre-apply a duplicate-probe/cleanup like runegate's
  `F0031a_duplicate_probe_and_cleanup.sql`.
- [x] **T6.2** **Race / double-spend audit:** prove the credit and debit helpers are
  safe under concurrent + replayed delivery (two webhooks, simultaneous spends,
  spend-during-credit). The conditional `UPDATE` 1-row check is the gate; verify no
  read-modify-write path exists.
- [x] **T6.3** **Address-ownership & spoofing:** confirm a deposit can only ever
  credit the address's owner; a forged/replayed body cannot redirect a credit
  (the signature is already verified upstream by `RunegateWebhookVerifier`; this is
  the post-verify authorization check).
- [x] **T6.4** **IDOR sweep** on the new read/spend endpoints (balance, address,
  ledger, pay-from-balance) using the Plan 4 / #12 subject-scoping patterns — a user
  can never read or spend another user's balance.
- [x] **T6.5** **Deposit-address write surface (re-scoped — see spike below).** The
  unauthenticated `AutomationV1Controller.InsertAddress` (old T3.4) was **removed by PR #13**
  (security loose-ends close-out) and is confirmed absent from the codebase (grep finds it
  only in docs now). The real property to assert and test: **no unauthenticated write to
  `tblM_User_Crypto_Deposit`, and the deposit address is immutable post-creation** — a
  planted/repointed address is the only way to redirect a credit (T2.4). Holds today (sole
  writer is authenticated registration; no `Update` path); the ensure/repair path (T1.2)
  must preserve it (create-if-missing only, never overwrite). Note: the `00-overview.md`
  P3·S3 progress row and `03-security-hardening.md` still show T3.4 as open — pre-existing
  doc lag vs the #13 closure, worth reconciling separately.
- [x] **T6.6** **Precision / overflow / negative guards:** decimal rounding, negative
  or zero amounts, and overflow on the credit/debit math fail closed (ties to R7/R11).
- [x] **T6.7** **Post-verify cross-check (defense-in-depth; adopts I-5).** Re-fetch the
  deposit from Runegate's REST record (`v1/transaction/static` / `v1/transaction/detail`,
  **confirmed to exist**) and confirm amount/status before crediting — a wallet top-up has
  no pre-recorded amount to match, so this is the I-5 substitute for "amount from our row."
  Mirrors `WebhookCrossCheckEvaluator` on the Wasabi path.
- [x] **T6.8** **Internal red-team** the credit/spend paths inside the worktree; fix
  findings before the PR.
- [x] **T6.9** **Fix the refund-path balance null-deref (carried low-sev).**
  `CallbackV1Service.Wasabi`'s deposit-fail refund dereferences the `tblM_User_Balance`
  row from `FirstOrDefault()` without a null check (a user with no balance row throws,
  leaving the deposit `Failed` with no refund). Centralizing balance mutation (T5.1) +
  `ensureWallet` (T1.2) closes it — null-guard + single-transaction claim+credit.

### Design concerns surfaced (deep dive)

- **C6.a — T3.4 is already closed (by PR #13).** `AutomationV1Controller.InsertAddress`
  was removed by #13 and is absent from the codebase; the deposit-address table's sole
  writer is authenticated registration, and there is no `Update` path (address is
  immutable). The credit-redirect threat (T2.4) is therefore already structurally closed —
  the work is to *assert* it with a test and keep it true through the ensure/repair path,
  not to "close an endpoint."
- **C6.b — The cross-check is no longer blocked.** `security-findings.md` item #1 marks it
  blocked on an unknown Runegate endpoint; the Runegate source supplies it
  (`v1/transaction/detail`, Basic-auth, base URL already configured in `PGCryptoService`).
  Building T6.7 closes that standing item.
- **C6.c — New money endpoints inherit the INT-tier isolation assumption**
  (`security-findings.md` #3) and the absent OTP/rate-limit controls (#4/#5). The
  deposit/spend/balance endpoints must sit behind the same perimeter + rate-limiting when
  those land; flag any change that widens INT exposure as a go/no-go gate.
- **C6.d — Ledger-tamper invariant.** Forensics (Plan 3 #9) checks
  `BalancePrevious + Amount == Balance` per ledger row. The credit/debit helper (T5.1)
  must preserve this invariant atomically (via the `OUTPUT` before/after), and the
  reconciliation test (T3.5) asserts it — so the new surface is forensics-clean by
  construction.

## Slice 7 — Verification & red-team

- [x] **T7.1** Build all affected projects + run `dotnet test QryptoCard.Tests.sln`
  (Unit: credit/debit/replay/precision logic; Integration: deposit→credit→spend
  lifecycle + ledger-reconciles invariant against LocalDB).
- [ ] **T7.2** Extend the Smoke tiers (T4.5) and run the dev-shakeout E2E once schema
  is applied.
- [x] **T7.3** **Full model-diverse external red-team** (Opus + Sonnet, separate
  headless sessions, verdicts posted verbatim on the PR) — this is a money-crediting +
  money-spending surface, the highest-risk class. Attack: double-credit via replay,
  double-spend via race, credit redirection / address spoofing, negative/overflow,
  cross-user IDOR, and reserve-then-confirm reconciliation gaps.
- [x] **T7.4** Open the gated PR (plain-English summary + technical section + red-team
  verdicts verbatim). Never merge without your go-ahead.

## Database changes (additive, DB-gated)

Mirrors the program's Band model: **code is built now in a worktree; the schema delta
applies during the dev shakeout / launch** as an idempotent script in `deploy/sql/`
(same pattern as `create-token-tables.sql`). Expected deltas, all additive, no
backfill:

- Unique index on the wallet-credit reference (`PGCryptoID`) for durable replay
  defense (T6.1).
- New ledger `Type` values (`Crypto Deposit`, `Card Open`, `Card Topup`, reversals) —
  data convention, no DDL.
- Optional use of the existing `BalanceHold` column for reserve-then-confirm (R10) —
  column already present on `tblH_User_Balance`.

Rollback for each is a drop of the additive index; the code path is feature-gated so it
can be dark-shipped ahead of the schema.

## Verification

Per the workflow: before any approval gate, **build the solution and the test
projects, and run the suites**, stating exactly what ran and the pass/fail/skip
counts. New money logic gets Unit tests; the full deposit→credit→spend→reconcile
lifecycle gets a DB-gated Integration test (LocalDB), and the dev-shakeout Smoke run
exercises it end-to-end. The money surface goes through the Slice 7 red-team gate
before merge.

## Risks

- **Double-credit / double-spend** — the headline money risk; mitigated by the single
  atomic mutation helper (R5), `PGCryptoID` dedup + DB unique index (R6/T6.1), and the
  red-team gate.
- **Reserve/confirm desync** — a debit with no card, or a card with no debit; mitigated
  by reserve-then-confirm with compensating ledger entries (R10/T3.3).
- **Ledger drift** — running total diverging from the append-only ledger; mitigated by
  writing both in one transaction and asserting `Σ credits − Σ debits == Balance` in
  tests (R4/T3.5).
- **Silent credit loss** — a failed credit returning 200 to Runegate (no retry) loses a
  real deposit; mitigated by non-200-on-failure + retry, the webhook journal for replay,
  and idempotency so retries are safe (C2.a / T2.3).
- **Cutover transition** — open `Created` orders stranded when the legacy direct-to-card
  path is removed; mitigated by draining/cancelling them with a brief new-order freeze
  (R1 / T2.7).

## What I need from you

All the prior ❓ decisions are resolved (R1, R9, R12 above; the Runegate behaviour is
confirmed from the gateway source). What remains:

1. **A final sign-off on this plan** as written (the resolved decisions + the slice
   plan), so implementation can start.
2. **Operational input, not blocking** — the merchant `CallbackSigningKey` / webhook
   URL wiring with Runegate (already tracked in `INTAKE.md`), and a yes on extending
   the Plan 3 forensic queries to the new ledger before go-live.
3. Then I'll proceed in a worktree: build → test → internal red-team → external
   model-diverse red-team → gated PR, stopping only at the merge gate.
