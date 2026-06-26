# Pre-Production-Cutover Polish — WasabiCard Sandbox

## Status checklist (as of 2026-06-26)

✅ **Signed off 2026-06-26 — executing.** The §7 decisions are approved; slices are being
built and shipped in parallel worktrees, coordinated live in Issue #51 (merge stays a human gate).

**What this phase is.** Take what is already shipped and deployed to the dev shakeout
box and polish it to a realistic, practical state: exercise every money/card flow
end-to-end against the **WasabiCard sandbox** and confirm each is correctly wired
front-to-back — front-end UI → REST API → INT/WCF money tier → third-party providers
(WasabiCard sandbox; Runegate) — plus the visual/UX layer.

**What this phase is NOT.** Not the production cutover. No prod WasabiCard calls and **no
real-money movement** — the Runegate gap is closed with a dev-only, environment-gated
test-credit tool (§4), not a real deposit. No git-history scrub, no provider rotation, no
DB move — those remain Plan 1/2/5 launch gates. The launch crypto/bcrypt data migration
(D15/D16) stays deferred to cutover.

**Where things stand (verified this week):**
- Plans 1–8 are largely done. Security/auth code complete (operational rotations + the
  launch crypto/bcrypt migration deferred to cutover). The money engine (Plan 07) and the
  cardholder UI (Plan 08: wallet panel, buy/top-up → pay-from-balance, settings, full
  sidebar re-skin) are **shipped and deployed** to the dev shakeout box.
- Dev box: `vm-kash-dev` / `rg-kash-dev` (Azure sub "qrypto"), NSG-dark, reached via
  `az vm run-command`. Live sites: `app-dev.s16.xyz` (cardholder) + `admin-dev.s16.xyz`
  (admin). Deploy via `deploy/deploy.sh` — `sync <tier>` is the markup/CSS fast lane (no
  rebuild); `update`/`build` for code changes.
- Open item carried from Plan 08: **DD-7** distinct per-card-type artwork (needs a
  `.cs`/model change; see Plan 08 §2.3 / NS-4d).

---

## 1. Goal & definition of done

"Realistic and practical" means, for each money/card flow in scope, we have **observed
it work end-to-end against the sandbox** — not read the code and inferred it works:

1. **Wired front-to-back.** A user action in the cardholder UI produces the correct REST
   call → INT/WCF money-tier call → WasabiCard sandbox call, and the response propagates
   back to a correct, legible UI state. No silent failures, no dev-host leaks, no dead
   controls.
2. **Provider round-trips confirmed.** WasabiCard sandbox accepts our signed requests
   (X-WSB-API-KEY + RSA-SHA256 X-WSB-SIGNATURE, TLS 1.2) and returns expected payloads;
   webhooks (if the sandbox fires them — unknown U2, §6) are received, signature-verified,
   and acted on.
3. **Visually polished.** The flows present at the bar set by the shipped NewDesign — the
   sidebar app chrome, wallet panel, card lists/details, settings. UX gaps surfaced during
   the click-through are fixed or explicitly deferred with a reason.

A flow is **done** when it has a recorded pass in the §5 flow matrix (manual click-through
with the request/response observed, or an automated check), the UI state is correct on both
success and induced-failure paths, and any defects found are fixed or logged.

**Sandbox-build vs. live-test split (SD-10).** Because no card product is provisioned on
the sandbox merchant (U6) and we are **not** pursuing provisioning, "done" splits in two:

- **Built + verified in sandbox now:** all code changes (SD-1, SD-2, DD-7 — SD-9 needs no
  code, already verified fail-closed at the edge) compiled + unit/integration-tested; the **wallet money path** end-to-end (test
  credit → balance → ledger → deposit-address panel); the **buy/top-up money logic up to
  the provider boundary** (balance debit, fee math, insufficient-balance, and the
  provider-failure → refund/rollback path — exercisable because the WasabiCard call fails
  cleanly with no card product); and all visual/UX polish.
- **Validated in the live environment later:** the **card-issuance E2E success paths**
  (successful buy / top-up / `/card/sensitive` PAN-CVV / `/card/transaction` / cancel),
  real provider-driven webhooks, KYC behavior, and confirming the (already fail-closed)
  webhook verifier accepts a real WasabiCard signature (SD-11 (c); needs a real webhook
  sample). Prod additionally requires WasabiCard IP-whitelisting.

---

## 2. Architecture recap — the two provider integrations

kash-cards depends on two third parties. They are wired differently, and only one is
sandbox-reachable today.

| Provider | Role | Tier(s) | Auth | Base URL today |
|---|---|---|---|---|
| **WasabiCard** | Issues & funds prepaid cards; emits card webhooks | INT (`QryptoCard.INT`) + Callback (`QryptoCard.INT.Callback`) | `X-WSB-API-KEY` + RSA-SHA256 `X-WSB-SIGNATURE`, TLS 1.2 | sandbox-reachable — see §3 |
| **Runegate / PGCrypto** | Per-user crypto deposit address; webhook credits the wallet balance | INT + Callback | HTTP Basic | **PROD only — hardcoded** `https://api.runegate.co`, no sandbox override (§4) |

The money loop is: **USDT deposit → Runegate webhook credits wallet balance → user
spends balance to buy/top-up a WasabiCard card.** The WasabiCard half is sandbox-testable.
The Runegate "crypto-in → balance-credit" half is **not** reachable in a card-only sandbox
— that is the central gap this phase has to design around (§4).

---

## 3. WasabiCard sandbox enablement

### 3.1 The INT/Callback URL mismatch (the env-switch fix)

The two tiers select the WasabiCard base URL inconsistently:

- **Callback tier** reads it from the environment, defaulting to **PROD**:
  `QryptoCard.INT.Callback/Model/KeyModel.cs:13` —
  `SecretsConfig.GetOptional("WASABICARD_API_URL", "https://api-merchant.wasabicard.com")`.
- **INT tier** **hardcodes a static literal** (currently the sandbox URL), ignoring the
  env var entirely: `QryptoCard.INT/Model/KeyModel.cs:16` —
  `public static string WASABICARD_API_URL = "https://sandbox-api-merchant.wasabicard.com";`

So today the two tiers happen to agree only by coincidence (the INT literal is sandbox).
A single env flip cannot move both tiers together, and a prod-config box would send the
callback tier to prod while INT stayed pinned to the sandbox literal — a latent
split-brain.

**Fix (SD-1):** make **both** tiers read the same `WASABICARD_API_URL` env var **and make
it Required — no silent default** (fail fast at `Application_Start` if unset). The deploy
plumbing already injects `WASABICARD_API_URL` per pool (sourced from KV, seeded from
`deploy/secrets/.env`, which sets the sandbox URL), so requiring it is safe. This matters
more than it first appears: because the **same credentials work for both sandbox and prod**
(SD-8), the URL is the *only* switch between test and real money — a wrong silent default
is unacceptable, so a misconfigured box must refuse to start rather than quietly hit the
wrong environment. Pure code change in `QryptoCard.INT/Model/KeyModel.cs` (+ tighten the
callback tier's `GetOptional(...)` to `Require`); no deploy-script change needed.

### 3.2 Credentials — already in hand (SD-8)

**Not a blocker.** The WasabiCard credentials (`WASABICARD_API_KEY`,
`WASABICARD_PUBLIC_KEY`, `WASABICARD_PRIVATE_KEY`, `WASABICARD_PRIVATE_KEY_XML`,
`WASABICARD_WSBPUBLIC_KEY`) are **the same for sandbox and prod** and are already seeded
(`deploy/secrets/.vault` present; injected per pool by `inject-secrets.ps1`). Production
access is gated **only by server-IP whitelisting** — the dev box is not whitelisted, which
is a helpful backstop, but **the `WASABICARD_API_URL` switch is the real sandbox/prod
boundary** (hence SD-1: make it Required, never defaulted). The auth code (signing,
headers, TLS 1.2) is already implemented. Nothing to obtain.

### 3.4 Finding — the WasabiCard webhook signature IS verified (the original "not verified" finding was wrong)

> **⚠️ CORRECTION (2026-06-26).** The finding below — that the WasabiCard webhook
> signature is never verified — is **incorrect and superseded**. It traced only the INT
> WCF tier (`CallbackV1Service.Wasabi`) and missed that that tier is **not
> internet-facing**. The webhook is verified **fail-closed, over the exact raw body,
> before any parse or forward**, at the public edge — and has been since **2026-06-24**
> (commit `b9f054b`, "Verify webhook signatures at the deposit callback before
> crediting"), two days before this plan was written. **SD-9 and SD-11 are therefore
> moot; slice S2 (Phase 1, §9) is resolved by prior work and ships no code.** See the
> corrected description immediately below; the original (wrong) analysis is kept after it
> for the record.

**Corrected understanding — the actual topology.** WasabiCard's webhook does not reach the
INT WCF service directly. It hits the public REST edge
`QryptoCard.API.Callback/Controllers/v1/CallbackV1Controller.wasabi()` (`[Route("v1/payment/wasabicard")]`),
which:

1. reads the **exact raw request bytes** (`Request.Content.ReadAsByteArrayAsync()`),
2. rejects an empty body, then
3. calls `WasabiSignatureVerifier.Verify(X-WSB-SIGNATURE, rawBody, WASABICARD_WSBPUBLIC_KEY)`
   and **returns 401 fail-closed on mismatch — before any parse**, and only then
4. forwards the **exact verified bytes** to the INT tier
   (`sr.Wasabi(category, signature, requestId, UTF8.GetString(rawBody))`).

The INT WCF handler `CallbackV1Service.Wasabi` doesn't re-verify because it can only be
reached **through that edge**: the INT callback tier is network-isolated and gated by the
`X-Int-Auth` shared secret (`IntAuthBehavior` / `INT_CALLBACK_SHARED_SECRET`), so it
receives only already-verified payloads. The same edge commit also verifies the
**Runegate/PGCrypto** deposit webhook fail-closed (`RunegateWebhookVerifier`, `X-Runegate-Signature`),
which likewise corrects the "related finding" note in §7.

**What actually remains (live, not a sandbox code task):** the verifier is proven against
self-signed fixtures but has not yet been confirmed against a **real** WasabiCard
signature (gated on **U2** — whether the sandbox fires webhooks at all). That is a
live-environment validation, not a build task, and it does not change the fact that the
edge is already fail-closed today.

---

**Original finding (SUPERSEDED — retained for the record; do not action).** *Surfaced
during this phase's code trace; the trace was incomplete (it stopped at the INT WCF tier
and never looked at the API.Callback edge).* The WasabiCard callback handler
`CallbackV1Service.Wasabi(cat, sign, req, a)`
(`QryptoCard.INT.Callback/Service/v1/CallbackV1Service.svc.cs:60`) receives the
`X-WSB-SIGNATURE` as `sign` and only **logs** it (`tes.Header = sign;`, line 67) — it does
not verify it. From the INT tier alone this *looked* like the signature was never checked.
It is — at the edge (see the correction above), so the impact described here (forged
finalization/auth/refund records) does **not** apply: a forged webhook is rejected with
401 before it ever reaches this handler.

### 3.3 Unknowns to resolve early

These shape how much of §5 we can actually exercise. **The 2026-06-26 doc spike
(`wsb.gitbook.io/wasabicard-doc`) did not answer U1–U3 — none are documented** — so each
needs a live sandbox spike (we hold working creds) or one email to WasabiCard staff. The
spike also surfaced **two new unknowns, U4–U5**, that are higher-risk than the original
three.

- **U1 — Funding the sandbox merchant wallet. ✅ RESOLVED (live spike 2026-06-26).** The
  sandbox merchant wallet is **already pre-funded** — `getAccountInfo` returns
  `accountType: WALLET`, `availableBalance: 9999918.3 USD`, `frozenBalance: 0`. No funding
  action is needed to back test card buys/top-ups. (The §4.1 merchant-float reconciliation
  remains a *production* concern; in the sandbox it's pre-funded.)
- **U2 — Do webhooks fire in the sandbox?** Undocumented. Webhook delivery requires
  WasabiCard to register our callback URL ("provide the callback address from the
  merchant… configured in the merchant backend") — i.e. a staff/dashboard step, and our
  box is NSG-dark/tunnel-only. If delivery isn't available, exercise the webhook paths with
  **synthetic signed POSTs** (we hold the keys). (Docs: `api/webhooks`.)
- **U3 — KYC & sensitive data in sandbox.** Undocumented (auto vs. manual, sandbox
  behavior). Docs: holder approval is tracked via the `card_holder` webhook **or** the
  `/holder/query` list, and "when the cardholder is approved, the next step will be taken."
  Sensitive PAN/CVV is returned only **post-activation** (physical) / via `card-info-for-
  sensitive` (virtual). *To confirm whether sandbox auto-approves KYC and returns test PAN.*
- **U4 (NEW) — v1 vs v2 API drift. 🟡 CORE RESOLVED (live spike 2026-06-26).** Our gateway
  calls **v1** paths (`/card/openCard`, `/card/cardTypes`, `/card/holder/create`,
  `/card/holder/query`, `/card/deposit`, `/card/info`, `/card/sensitive`,
  `/card/transaction`, `/card/cancel` — `WasabiCardService.cs:236–878`). The current docs
  describe a **v2 issuance flow** (`/card/v2/cardTypes`, `cardholder-create-v2`,
  `/card/v2/createCard`, `card-info-for-sensitive`). The spike confirmed **v1 auth + read
  works** (`/account/info` → HTTP 200 with our creds + signing). **Both** the v1
  `/card/cardTypes` **and** the v2 `/card/v2/cardTypes` return HTTP 200 with an **empty
  list** — so the empty result is **not** a version mismatch; it's the U6 provisioning gap
  below. The v1-vs-v2 *create* question (`openCard` vs `/card/v2/createCard`) can only be
  settled once card products exist; deprioritised until then.
- **U6 (NEW, BLOCKER) — no card products/BINs are enabled on the sandbox merchant.** Both
  `/card/cardTypes` (v1) and `/card/v2/cardTypes` (v2) return `data: []` (live spike
  2026-06-26). A card type / BIN is **required** to open a card (`cardTypeId` is a required
  field of both `openCard` and v2 `createCard`), so **the entire buy-card / top-up flow is
  blocked until WasabiCard enables card products on our sandbox merchant.** This is a
  **WasabiCard staff action** — the top ask of the §0.1 email. (The merchant *wallet* is
  funded — U1 — there is simply no card product to spend it on yet.)
- **U5 (NEW) — the `card_holder` (KYC) webhook is not handled.** `CallbackV1Service.Wasabi`
  has **no `card_holder` branch** (grep: zero `holder` references). Holder/KYC approval is
  therefore only observable synchronously during `openCard` or by polling `/holder/query` —
  not reactively. If sandbox KYC is **async**, the buy-card flow may stall at "pending
  holder" with nothing to advance it. *Confirm sandbox KYC timing; if async, handling
  `card_holder` (or polling on a timer) becomes in-scope.*

---

## 4. The Runegate deposit-side gap

The crypto-deposit → balance-credit half runs through Runegate/PGCrypto, **hardcoded to
PROD** (`QryptoCard.INT/Model/KeyModel.cs:14`, HTTP Basic). Runegate *has* a sandbox, but
**it doesn't carry USDT** — the stablecoin the deposit path settles in — so the genuine
"USDT in → wallet credit" loop still cannot be exercised in a sandbox. The real
address-provisioning + webhook-delivery integration stays a **cutover-time verification**;
no real PROD deposit is made in this phase.

### 4.1 The merchant-float reconciliation (operational gap, noted for launch)

How card funding really works (owner-confirmed): the merchant (us) holds a **balance with
WasabiCard** and spends it to open/fund cards on users' behalf. Separately, a user's USDT
deposit lands at their per-user Runegate address and accrues to **our Runegate balance**.
These are **two different pots.** Nothing in the codebase moves money from the Runegate
pot to the WasabiCard pot — so in production the WasabiCard merchant balance drains as
users buy cards and must be **topped up** (manually or via an automated sweep:
withdraw from Runegate → deposit to a WasabiCard merchant deposit address) or card issuance
halts at zero balance.

**This phase does not build that sweep** — it's a launch/operational item (flagged here so
it isn't forgotten). It is also why **U1** (funding the sandbox merchant balance) has no
in-app answer: the merchant balance is funded out-of-band, so the sandbox needs a
dashboard/staff top-up to back test card buys.

**Resolution (SD-2): a dev-only test-credit tool, defended in depth.** To put a testable
balance behind card buys/top-ups, we drive the **existing, already-shipped credit path**
(`WalletService.CreditDeposit`, or a synthetic `PGCrypto` webhook to the Callback tier),
not a raw "set balance = X". Because crediting a wallet is **minting money**, it is walled
three ways:

1. **Environment hard-gate (load-bearing, fail-closed).** The path refuses to run unless
   `QRYPTO_ENVIRONMENT` is the explicit dev/sandbox value. This makes prod money-minting
   *physically impossible* — even an attacker with full root-admin in prod cannot use it.
2. **Root-admin only.** Highest-privilege gate (per owner ask) — defense in depth.
3. **Audit-logged.** Every use recorded.

*Never auth alone for a money-minting capability — the environment gate is the wall;
admin-only is the second line.* This injection is straightforward precisely because the
`PGCrypto` webhook itself is signature-light (its T2.5 signature wire-up was deferred; see
the §7 note) — which is also why its **prod** protection is the planned Cloudflare edge
IP-lock, unaffected by this dev-only tool.

---

## 5. Scope — flows to test & polish

Each flow is exercised front-to-back and recorded in the matrix. "FE→API→INT→provider"
names the hops to confirm at each layer.

### 5.1 WasabiCard-dependent flows (sandbox)

| Flow | Entry (FE) | Chain | Provider endpoint(s) |
|---|---|---|---|
| **Buy card** | `card/carddetail.aspx` Buy | `CardService.openCard` → REST → INT `CardSpendService.OpenCard` → WasabiCard `openCard`/`openCardWithHolder` (+ `createHolder`) | `/card/openCard`, `/card/holder/create`, `/card/holder/query` |
| **Top-up** | `card/mycarddetail.aspx` | `CardService.depositCard` → INT `CardSpendService.TopUp` → WasabiCard `depositCard` | `/card/deposit` |
| **Card detail / balance** | card detail view | card info/balance read | `/card/info`, `/card/balanceInfo` |
| **Sensitive (PAN/CVV)** | OTP-gated reveal | sensitive read | `/card/sensitive` |
| **Transactions / history** | `txcard.aspx` | tx read | `/card/transaction` |
| **Cancel card** | card detail action | cancel | `/card/cancel` |
| **Webhooks** | n/a (inbound) | edge `CallbackV1Controller.wasabi()` verifies `X-WSB-SIGNATURE` **fail-closed before forward** (✅ §3.4 correction, SD-9 moot) → INT `CallbackV1Service.Wasabi` (via `X-Int-Auth`) | `card_transaction` (create/deposit finalize), `card_auth_transaction` (spend auth), `card_3ds` (OTP), `card_fee_patch`, `card_holder` (KYC) |

### 5.2 Wallet / balance flows (Plan 07 surface)

- **Deposit address + balance + ledger** display on the dashboard wallet panel
  (`getDepositAddress`/`getLedger`/balance read methods, IDOR-scoped).
- **Pay-from-balance** on buy and top-up (server-authoritative fee at spend).
- The crypto-in credit that backs the balance is gated on **SD-2** (§4).

### 5.3 Visual / UX layer

Walk every in-scope page against the shipped NewDesign bar: sidebar chrome, dashboard
wallet panel, card lists/details, settings, transaction history. Log UX defects found
during click-through; fix small ones inline, batch larger ones. Iterate via
`deploy.sh sync` (CSS/markup fast lane) where no rebuild is needed.

### 5.4 DD-7 card artwork (in scope — SD-4)

Distinct per-card-type artwork: a nullable art field on the card-type data (INT card-type
service → API `/v1/card/type` → `CardTypeModel`), images under `Content/media/cards/`,
static brand card as fallback. **Source the images from the NewDesign template** (already
on disk locally, untracked) — that look is the intended bar. Touches `.cs` (needs a full
`update`, not `sync`). Folds into the visual-polish pass.

---

## 6. Test strategy & how to drive webhooks on an NSG-dark box

- **Primary: manual click-through** of each §5 flow on `app-dev.s16.xyz`, observing the
  request/response at each hop (INT-tier logs + the WasabiCard sandbox dashboard). This is
  the fastest path to "observed it work" and matches the polish goal.
- **Automated checks where cheap:** extend the existing test projects
  (`QryptoCard.Tests.*`) with sandbox-targeted integration checks for the read paths and
  signing, where they can run without a live merchant-wallet balance. Mutating sandbox
  calls (open/deposit/cancel) are exercised manually first; automate only the stable ones.
- **Webhooks on an NSG-dark box (U2-dependent):**
  - If the sandbox **does** deliver webhooks, it needs a reachable callback URL — the box
    is NSG-dark/tunnel-only, so confirm the Cloudflare tunnel exposes the callback route
    (or register the sandbox to the dev callback host) before relying on provider events.
  - If it does **not** (or we can't expose a route), **synthesise signed webhooks**: POST
    correctly-signed payloads (we hold `WASABICARD_PRIVATE_KEY`/`WSBPUBLIC_KEY`) to
    `CallbackV1Service` from on-box via `az vm run-command`, exercising verify-and-act
    without depending on provider delivery. Same technique closes the Runegate credit stub
    in §4 Option A.

---

## 7. Decisions to hammer out (sign-off gate)

Recommendations are defaults I'll proceed with unless changed. ❓ = genuinely needs your
input.

| # | Decision | Resolution (signed off 2026-06-26) |
|---|---|---|
| **SD-1** | INT/Callback WasabiCard URL fix | ✔ **Make both tiers read `WASABICARD_API_URL` and make it REQUIRED — no silent default** (fail fast at startup if unset). Rationale strengthened by SD-8: since the **same credentials work for both sandbox and prod**, the URL is the *only* switch between test and real money — a wrong silent default is therefore unacceptable. The deploy already injects `WASABICARD_API_URL` per pool, so requiring it is safe (a misconfigured box won't start rather than silently hit the wrong environment). Change in `QryptoCard.INT/Model/KeyModel.cs` (+ tighten the callback tier from `GetOptional(...,prod-default)` to `Require`). |
| **SD-2** | Runegate deposit gap | ✔ **Build a dev-only test-credit tool, defended in depth.** Runegate *has* a sandbox but it doesn't carry USDT, so the real USDT-in path still can't be exercised. We credit a test wallet by driving the existing verified credit path (`WalletService.CreditDeposit` / a synthetic `PGCrypto` webhook). **Three walls:** (1) **environment hard-gate, fail-closed** — refuses unless `QRYPTO_ENVIRONMENT` is the dev/sandbox value (the load-bearing control: makes prod money-minting physically impossible even with full admin); (2) **root-admin only** (per owner ask); (3) **audit-logged**. *Never auth alone for a money-minting path — the env gate is the wall, admin-only is defense in depth.* No real PROD deposit. |
| **SD-3** | Seeding | ✔ **Prefer sandbox-generated data over fabricated seed.** If the WasabiCard sandbox lets us actually generate card opens / top-ups / transactions that flow into our DB (U1–U3 dependent), we use *those* — more realistic than hand-seeded rows. We create only the **minimal test user accounts** needed to log in and click through; their wallet balances come from the SD-2 test-credit tool. Anonymized prod-data load stays deferred. **Update (shipped, S7 / [#59](https://github.com/egunawan85/kash-cards/pull/59)):** a deterministic **synthetic dev-seed generator** now backs a realistic dataset (~25 users with wallets/ledger/cards/transactions), emitted as committed idempotent SQL and applied **dev-only** by `vm-seed.ps1`; the anonymized *prod-data* load stays deferred, with a documented fit-to-prod-aggregates hook left in the generator. |
| **SD-4** | DD-7 card artwork | ✔ **In scope.** Harvest the card artwork from the **NewDesign template** (already on disk locally, untracked) — the owner likes that look. Vendor the images into `Content/media/cards/`, add the nullable art field (INT card-type → API `/v1/card/type` → `CardTypeModel`), render per-type with the static brand card as fallback. Needs a full `update` (touches `.cs`), not the `sync` fast lane. |
| **SD-5** | Test strategy | ✔ **Manual click-through primary** + targeted automated checks for read/signing paths; mutating sandbox calls exercised manually first, automate only the stable ones. |
| **SD-6** | Webhook driving | ✔ Confirm tunnel exposure if the sandbox delivers (U2); otherwise **synthesise signed webhooks on-box** (we hold the keys). Same harness backs the SD-2 test-credit tool. |
| **SD-7** | Red-team posture | ✔ **Internal-only, per money-touching change.** No external (sandbox, no real money) unless a change alters the prod money/auth surface. The SD-2 test-credit tool gets an internal red-team. (SD-9 needs no change — already verified fail-closed at the edge.) |
| **SD-8** | Credentials | ✔ **Not a blocker — we already have working credentials.** The **same** WasabiCard creds work for sandbox and prod (`.vault` is seeded); prod is gated *only* by **server-IP whitelisting**, which the dev box does not have — a useful backstop, but **the `WASABICARD_API_URL` switch is the real sandbox/prod boundary** (→ reinforces SD-1's "Required, no default"). |
| **SD-9** | WasabiCard webhook signature not verified (§3.4) | ⤫ **MOOT — already fixed (2026-06-26 correction).** The premise was wrong: the signature is already verified **fail-closed, over the exact raw body, before parse/forward** at the public edge (`CallbackV1Controller.wasabi()`, commit `b9f054b`, 2026-06-24). The INT WCF handler is reached only through that edge (network-isolated + `X-Int-Auth`). No code change; nothing to wire. See the §3.4 correction. |
| **SD-10** | Sandbox card-product gap (U6) — pursue or defer? | ✔ **Defer to live; no WasabiCard email.** We do **not** chase sandbox card-product provisioning. Build everything sandbox can exercise now (see split below); the **card-issuance E2E** (successful buy / top-up / sensitive / transactions / cancel, real provider webhooks, KYC) is validated in the **live** environment later (prod needs WasabiCard IP-whitelisting). Sandbox stays the build/verify environment for code + the wallet/money-logic paths. |
| **SD-11** | SD-9 rollout safety (verifier unproven vs. real WasabiCard signatures) | ⤫ **MOOT as a build step — the edge is already fail-closed.** The monitor-first staging was predicated on SD-9 being unbuilt; it isn't. Verification already runs over the exact raw body at ingress and rejects on mismatch (it never went through a monitor phase). The one genuine residual is the SD-11 (c) step — confirm the verifier accepts a **real** WasabiCard signature, gated on **U2** (does the sandbox fire webhooks?). That is a **live** validation, not sandbox code. If a real sample ever fails, revisit then; do not pre-emptively weaken the edge to monitor mode. |

**Related finding — also already corrected (2026-06-26):** the note below claimed the
**Runegate/PGCrypto** webhook performs no cryptographic signature check. That is **no longer
true** — the same edge commit (`b9f054b`) verifies it fail-closed via
`RunegateWebhookVerifier` over `X-Runegate-Signature` at `CallbackV1Controller.pgcrypto()`,
before parse/credit. The SD-2 dev test-credit tool does **not** depend on the PGCrypto
webhook being signature-light; it routes through the internal `WalletService` credit path
and is walled by the environment hard-gate (see SD-2). *Original note (superseded):* the
`PGCrypto(...)` webhook "also performs no cryptographic signature check — its T2.5 signature
wire-up was deferred in Plan 3; it relies on user-from-our-record + `isPaid` +
`TransactionID` idempotency + the planned Cloudflare edge IP-lock for prod."

**Facts still to gather early (one smoke test or one email to WasabiCard staff): U1 (fund
the sandbox merchant wallet), U2 (do webhooks fire in sandbox?), U3 (KYC auto-approved +
does `/card/sensitive` return test PAN/CVV?).** These gate how much of §5 — and SD-3's
"let the sandbox generate the data" — we can actually exercise.

---

## 8. Workflow & sequencing

Per the global worktree-driven workflow: surface these decisions and **STOP for sign-off**
before any worktree or code edit. After sign-off:

1. **Resolve the facts (no code):** confirm U1–U3 via a quick sandbox smoke test or one
   WasabiCard email (fund merchant wallet / do webhooks fire / KYC + sensitive). Creds are
   already in hand (SD-8); nothing to obtain.
2. **Env-switch fix (SD-1)** — small worktree: make `WASABICARD_API_URL` Required in both
   `QryptoCard.INT` and `QryptoCard.INT.Callback`; build + run suites; deploy via `update`.
   Confirm the dev box still resolves to the sandbox URL after the change.
3. ~~**WasabiCard webhook signature verify (SD-9)**~~ — **already done (no code).** Verified
   fail-closed at the edge since commit `b9f054b` (2026-06-24); see the §3.4 correction.
4. **Dev-only test-credit tool (SD-2)** — env-hard-gated + root-admin-only + audit-logged,
   routing through `WalletService.CreditDeposit` / synthetic `PGCrypto` webhook; internal
   red-team (the env gate is the security-critical surface).
5. **Flow shakeout (§5)** — drive each flow front-to-back; let the sandbox generate data
   where it can (SD-3); record passes/defects in the matrix; fix wiring/UX defects in
   focused worktrees (one per cohesive change), iterating via `deploy.sh sync` for
   markup/CSS.
6. **DD-7 card art (SD-4)** — art field + template images; full `update`.
7. **Per-change verification + internal red-team (SD-7)** before each PR; PRs branch off
   `main` from a worktree, never stacked; **merge stays a human gate**. The SD-2 test-credit
   tool is the money/security-touching change that gets an internal red-team (SD-9 needed no
   change — already verified at the edge).

**Verification bar (every gate):** build the affected projects + test projects, run the
relevant suites, state pass/fail/skip counts. "Verified" = ran it and saw it pass.

**Out of scope / still launch-gated:** prod WasabiCard, prod DB move + swap, provider
rotations, git-history scrub, the launch crypto/bcrypt data migration, Plan 07 T2.7
cutover (drain `Created` orders) + T1.4 unique indexes. This phase polishes the sandbox;
it does not cut over.

---

## 9. Execution checklist — phases → slices → tasks

`[ ]` = not started · `[~]` = in progress · `[x]` = done. Each slice ≈ one reviewable PR
off `main` from its own worktree (never stacked); **merge is a human gate.** Plain-English
intent first, then the task list. Dependencies are called out per slice.

### Phase 0 — Enablement (unblock; no flows can be trusted until this lands)

**Slice 0.1 — Resolve the sandbox unknowns U1–U6.** *(no code; gates how much of Phase 2 is exercisable)*
Doc spike + live spikes done (2026-06-26). Progress below; the remainder needs one
**WasabiCard email** for the staff-gated items.
- [x] **Live spike: `getAccountInfo`** → HTTP 200; creds + signing valid; merchant wallet pre-funded **$9,999,918.3 USD** → **U1 resolved**, **U4 auth/read proven**
- [x] **Live spike: `cardTypes` (v1) + `card/v2/cardTypes` (v2)** → both HTTP 200, `data: []` → **U6 found** (no card products enabled); empty result is provisioning, not version
- [ ] **U6 (BLOCKER)** — WasabiCard to **enable card products/BINs** on our sandbox merchant (top email ask); re-spike `cardTypes` to confirm
- [ ] **U2** — confirm whether **webhooks fire** + how to register our callback URL on an NSG-dark box (else synthetic signed POSTs)
- [ ] **U3** — confirm **KYC auto-approval** + that `/card/sensitive` returns **test PAN/CVV**
- [ ] **U4/U5 (blocked on U6)** — once a card product exists, spike `openCard` (v1) vs `/card/v2/createCard` to settle v1-vs-v2, and observe **KYC timing** (sync/auto vs. async → whether `card_holder` handling / `/holder/query` polling is in-scope)
- [ ] Draft + send the WasabiCard email (U6 product enablement, U2 callback registration, U3 sandbox KYC)
- [ ] Record all answers in §3.3 (drives SD-3, and whether U4/U5 add scope)

**Slice 0.2 — Make `WASABICARD_API_URL` Required in both tiers (SD-1).** *(no dependency)*
The INT tier hardcodes the URL; the callback tier defaults to prod. Since the same creds
work in both environments, the URL is the *only* sandbox/prod switch — so make both tiers
read it from env and **refuse to start if it's unset**, rather than silently defaulting.
- [ ] `QryptoCard.INT/Model/KeyModel.cs:16` — replace the hardcoded literal with `SecretsConfig.Require("WASABICARD_API_URL")`
- [ ] `QryptoCard.INT.Callback/Model/KeyModel.cs:13` — tighten `GetOptional(...,prod-default)` to `Require`
- [ ] Build INT + Callback + test projects; run suites; record pass/fail/skip
- [ ] Deploy via `update`; confirm both tiers resolve to the **sandbox** URL on the dev box
- [ ] PR (`worktree-09-url-required`); no red-team (config plumbing)

**Slice 0.3 — Synthetic dev-seed dataset (SD-3 follow-on). — ✅ DONE ([#59](https://github.com/egunawan85/kash-cards/pull/59)).**
Runegate can't fund a realistic dataset in the sandbox, so a deterministic generator fabricates
~25 users with wallets, a chained ledger, cards and card transactions — emitted as committed
idempotent SQL and applied **dev-only** by `vm-seed.ps1` (sqlcmd, no rebuild). One loginable demo
cardholder (real inbox for the emailed OTP) + display-only filler; entirely fabricated, no prod data.
- [x] `deploy/scripts/dev-seed/generate-dev-seed.ps1` — deterministic generator → committed SQL
- [x] `deploy/sql/seeds/seed-dev-synthetic.sql` — idempotent, `5eed%`-namespaced, internally consistent (ledger `prev+amt=bal`)
- [x] `vm-seed.ps1` applies it inside the `Env -eq 'dev'` gate; demo email/password spliced via `sqlcmd -v`
- [x] `deploy.sh seed` reload lane (data-only; no build/IIS)
- [x] Verified against the real dacpac on LocalDB (counts + ledger invariants + idempotency); light internal red-team (env-gate)

### Phase 1 — Security & money-path corrections (the two changes with teeth)

**Slice 1.1 — Verify the WasabiCard webhook signature (SD-9). — ✅ ALREADY DONE (prior work; no code this phase).**
The original premise ("the webhook logs `X-WSB-SIGNATURE` but never checks it") was a
mis-trace of the INT WCF tier. The signature is already verified **fail-closed, over the
exact raw body, before any parse or forward** at the public edge
(`QryptoCard.API.Callback/Controllers/v1/CallbackV1Controller.wasabi()`, commit `b9f054b`,
2026-06-24); the INT handler is reached only through that edge (network-isolated +
`X-Int-Auth`). See the §3.4 correction and SD-9/SD-11. Nothing to wire.
- [x] Verified at ingress (`CallbackV1Controller.wasabi()`): raw bytes read, empty body rejected, `WasabiSignatureVerifier.Verify(...)` → **401 on mismatch before parse**, then exact bytes forwarded to INT
- [x] Forged/tampered/absent/empty are rejected at the edge (the original "internal red-team" items are satisfied by the edge's fail-closed return)
- [ ] *Live only (gated on U2):* confirm the verifier accepts a **real** WasabiCard signature once the sandbox actually fires a webhook — not a sandbox code task

**Slice 1.2 — Dev-only test-credit tool (SD-2).** *(no dependency; needed before Phase 2 spend flows)*
Runegate's sandbox has no USDT, so we can't rehearse the real deposit. Build a tool that
credits a test wallet through the *existing* credit path — walled so it can never run in
prod. The environment gate is the security-critical surface.
- [ ] Implement via `WalletService.CreditDeposit` / synthetic `PGCrypto` webhook (reuse the real dedup/idempotency path, not a raw balance set)
- [ ] **Wall 1 (load-bearing):** hard-gate on `QRYPTO_ENVIRONMENT` == dev/sandbox, **fail-closed** — no-op + log otherwise
- [ ] **Wall 2:** root-admin-only authorization
- [ ] **Wall 3:** audit-log every invocation (who/when/amount/target)
- [ ] Tests: dev + root-admin → credits; **prod env → refuses even with root-admin**; non-admin → refused; replay → deduped
- [ ] Build + run suites; record counts
- [ ] **Internal red-team:** prove the env gate cannot be bypassed; confirm no prod code path reaches the credit
- [ ] PR (`worktree-09-test-credit-tool`)

### Phase 2 — Money/card flow shakeout (sandbox, front-to-back)

> Gate: Phase 0 + Phase 1 merged (URL switch correct, webhook verified, test balances
> available). Each slice = drive the flow on `app-dev.s16.xyz`, confirm **every hop**
> (FE → API → INT → WasabiCard sandbox), confirm the **success** path *and* an **induced
> failure** path renders a correct, legible UI state. Record each in the §5.1 matrix.
> Defects → focused fix worktrees (markup/CSS via `deploy.sh sync`; code via `update`).

**Slice 2.1 — Wallet panel (address + balance + ledger).**
- [ ] Deposit address + QR renders (IDOR-scoped to the logged-in user)
- [ ] Balance reflects a test credit (from Slice 1.2); ledger lists the entry
- [ ] Induced-failure (read error) surfaces a message, not a blank/zero state

**Slice 2.2 — Buy card (open) end-to-end.**
- [ ] Buy on `card/carddetail.aspx` → `CardService.openCard` → `/v1/card/open` → INT `CardSpendService.OpenCard` → WasabiCard `openCard`/`openCardWithHolder` (+ `createHolder`)
- [ ] Balance **debited** (pay-from-balance, server-authoritative fee); order transitions correct
- [ ] Holder creation/KYC path observed (U3-dependent); `card_transaction:create` finalization lands
- [ ] Failure path: insufficient balance / provider error → refund + legible UI message (no silent redirect)

**Slice 2.3 — Top-up end-to-end.**
- [ ] Top-up on `card/mycarddetail.aspx` → `CardService.depositCard` → `/v1/card/deposit` → INT `CardSpendService.TopUp` → WasabiCard `depositCard`
- [ ] Balance debited; `card_transaction:deposit` finalization lands; card balance reflects it
- [ ] Failure path: refund via `WalletService.CreditRefund` + cross-check (`getDepositOperation`); legible UI

**Slice 2.4 — Card detail / balance / sensitive.**
- [ ] `/card/info` + `/card/balanceInfo` render correctly
- [ ] OTP-gated `/card/sensitive` reveals test PAN/CVV (U3-dependent); decrypt path works

**Slice 2.5 — Transactions / history.**
- [ ] `txcard.aspx` lists `/card/transaction` data; empty + populated states both legible

**Slice 2.6 — Cancel card.**
- [ ] Cancel action → `/card/cancel`; state + UI update; failure path legible

**Slice 2.7 — Webhook event types (verify-and-act).** *(uses real sandbox events if U2 = yes, else synthetic signed POSTs)*
- [ ] `card_transaction` (create + deposit finalize) — happy + failure/refund branches
- [ ] `card_auth_transaction` (spend auth) — incl. failure email
- [ ] `card_3ds` (OTP) ; `card_fee_patch` ; `card_holder` (KYC) recorded correctly
- [ ] All exercised **through the fail-closed edge verifier** (`CallbackV1Controller.wasabi()`, §3.4 correction) — confirm forged/tampered are rejected with 401 before forward

### Phase 3 — Visual / UX polish

**Slice 3.1 — DD-7 per-card-type artwork (SD-4).** *(touches `.cs` → full `update`)*
- [ ] Add nullable art field: INT card-type service → API `/v1/card/type` → `CardTypeModel`
- [ ] Vendor template card images into `Content/media/cards/`; map per card type
- [ ] Render per-type art with the static brand card as fallback
- [ ] Build + run suites; visual verify; light internal review (non-money)
- [ ] PR (`worktree-09-card-art`)

**Slice 3.2 — UX defect sweep.** *(markup/CSS via `deploy.sh sync`; one focused PR per cohesive change)*
- [ ] Walk every in-scope page vs. the NewDesign bar (chrome, wallet panel, lists, details, settings, history)
- [ ] Log defects found during the Phase 2 click-through; fix small inline, batch larger
- [ ] Confirm no dropped server-control wiring after any markup touch

### Tracking matrix (fill during Phase 2)

| Flow | Hops confirmed | Success | Induced failure | Defects | Status |
|---|---|---|---|---|---|
| Wallet panel (2.1) | | | | | [ ] |
| Buy card (2.2) | | | | | [ ] |
| Top-up (2.3) | | | | | [ ] |
| Card detail/sensitive (2.4) | | | | | [ ] |
| Transactions (2.5) | | | | | [ ] |
| Cancel (2.6) | | | | | [ ] |
| Webhooks (2.7) | | | | | [ ] |

---

## 10. Orchestration (parallel sessions)

Live coordination board: **GitHub Issue
[#51](https://github.com/egunawan85/kash-cards/issues/51)** — shared state lives there + in PR
state, never in a repo file. Claim = branch + draft PR; dependency gate = PR merge state;
merge to `main` is a human gate.

**Wave A — parallel sessions, disjoint files, no inter-deps:**
- **S1** SD-1 URL-Required · ~~**S2** SD-9/SD-11 webhook verify~~ **(✅ closed — already
  done; the webhook is verified fail-closed at the edge, see §3.4 correction / SD-9)** ·
  **S3** SD-2 test-credit tool · **S4** SD-4 card art · **S7** SD-3 synthetic dev-seed (✅ shipped).

**Wave B — sequential / gated:**
- **S5** wallet money-path verification — after **S3** merges (needs the test-credit tool).
- **S6** UX polish sweep — after **S4** + relevant re-skins; opportunistic, last.

**Deferred to live (not sessions):** card-issuance E2E success paths, real provider webhooks,
KYC, and confirming the already-fail-closed webhook verifier accepts a real WasabiCard
signature (SD-11 (c); there is no monitor→fail-closed flip to make — the edge is already
fail-closed).

**Unattended-run rule (verified against the `git-write-guard` hook):** create branch+worktree
in one step (`git worktree add -b <branch> .claude/worktrees/<dir> origin/main`); commit/push
**inside** the worktree (auto-approved); use `gh` for PR/issue ops. Avoid `git
checkout`/`switch`/`restore`/`stash`/`reset`/`rebase`/`pull`/`merge` and any push to `main`
(all prompt). Never commit in the main checkout; never merge (discipline — `gh pr merge` isn't
hook-gated).
