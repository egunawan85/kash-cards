# Non-security findings — open register

UX / functional / operational issues found while running the deployed stack that are **not**
security findings (security issues live in [`security-findings.md`](security-findings.md)).
Same format as the security register.

## Status legend
- **Open** — confirmed, not yet fixed.
- **Deferred** — acknowledged, parked for later by owner decision.
- **Fixed** — resolved (links the commit / PR).

---

## NS-1. Admin login page — cannot type into the input fields — *Deferred*

**Symptom.** On the admin dashboard login page (`https://admin-dev.s16.xyz/login`) the email
and password inputs cannot be typed into. The customer **registration** page works fine
(typing + OTP validated); the customer *login* page is unconfirmed.

**Ruled out (server-side) — the page is sound from the server's side:**
- All assets load (`HTTP 200`): `Content/plugins/global/plugins.bundle.{css,js}`,
  `Content/js/scripts.bundle.js`, `Content/css/style.bundle.css`.
- The inputs are plain `<input>` (`txtEmail` / `txtPassword`) — not `disabled`/`readonly`.
- The admin login page is **structurally identical** to the customer login page (same Web
  Forms template, same inputs, same JS bundles — only the logo and a "Sign up" link differ).
- The only inline scripts are harmless: anti-clickjacking frame-busting, a Bootstrap
  theme-mode setter, and a `hostUrl` variable. None of them touch the inputs.

**Likely cause (client-side, unconfirmed).** A runtime JS error in `scripts.bundle.js` /
`plugins.bundle.js` (they load but may throw), a Metronic overlay element covering the form,
a cached broken page state, or a browser extension. This needs browser-console (F12)
diagnosis, which can't be done from the server side.

**Next steps when picked up:**
1. Capture DevTools → Console errors on the login page (a thrown error there is the smoking gun).
2. Hard-refresh (Ctrl+Shift+R) / incognito / a different browser to rule out cache + extensions.
3. Confirm whether the customer login (`app-dev/login`) is *also* affected — that scopes it to
   the shared login template vs. an admin-only problem.

**Found:** testing the dev deployment (admin login), 2026-06-25.

---

## NS-2. OTP login verify collapses ~6 failure modes into one message — *Open (fix recommended)*

**Symptom.** `AuthV1Service.mintAfterOtpVerify` returns the identical `"Invalid OTP code or
session"` for at least six distinct conditions: empty input, wrong code, expired code, unknown
session, user-not-found, **and any internal exception** (e.g. a config / SQL error) caught by the
method's outer `catch`. The generic message is partly deliberate — the catch hides `ex.Message`
so a `SqlException` can't leak schema to the wire — but it also disguises **internal errors as
user errors**.

**Impact.** A deployment/config bug (a missing `AuthDbEntities` connection string — see PR #27)
presented to the operator *identically* to a user fat-fingering their OTP. Finding the real cause
required live instrumentation of the running WCF service, because the message actively
misattributed an infrastructure failure to a bad code.

**Recommended fix.** Keep the catch from surfacing `ex.Message`, but give the **internal-exception**
path a DISTINCT, non-leaky status (e.g. `Status="error"`, `Message="Authentication temporarily
unavailable"`) separate from the genuine bad-OTP returns — so config/infra failures are visible
instead of disguised. Optionally split expired vs incorrect vs unknown-session for the user-facing
paths (low sensitivity, since the caller already holds a valid session id). Touches money-tier auth
code, so it warrants its own red-team.

**Found:** diagnosing the OTP-login failure (PR #27), 2026-06-26.

---

## NS-3. Deploy is all-or-nothing (no app-only / per-tier) + leaves pools Stopped — *To do (approved, deferred to a later session)*

**What.** kash has exactly one deploy mode — the full `provision-and-bootstrap.sh --with-deploy`
pipeline (~30-45 min, rebuilds all 12 sites). There is no app-only redeploy and no per-tier
build/restart. Worse, the pipeline **leaves several app pools Stopped** at the end: after the
merged-main redeploy, `kash-int`, `kash-dashboard`, `kash-int-scheduler`, `kash-scrapper`, and
`kash-api-callback` were all Stopped and had to be started by hand (they started cleanly — no
faults, just never started by the deploy). A deploy that ends with the WCF money tier down is a
correctness gap.

**Why it matters.** Every code-only change forces the whole 30-45 min pipeline; much of the
multi-hour OTP debug was hand-rolling per-tier rebuilds the tooling should provide.

**Fix — model on the sibling apps.** runegate (PGCrypto) and qrypto-omni already ship this:
their `deploy-iis.ps1` is a multi-command tool (`setup`/`deploy`/`update`/`build [svc]`/`restart
[svc]`/`status`/`logs`). kash is the outlier (bundled into one orchestrator). Port that surface
as a laptop-side `deploy/deploy.sh <cmd> [svc]` (the box is NSG-dark, so it wraps `az
run-command`):
- `update` — app-only: fetch main -> clean-build all -> inject-secrets -> recycle (~5-10 min)
- `build [svc]` / `restart [svc]` — per-tier (`int`, `int-callback`, `api`, `dashboard`, ... aliases from sites.json)
- `status` / `logs [svc]`
- **a `start` step that guarantees every pool ends Started** (closes the gap above)
- **Open decision:** should `update` also re-publish schema, or stay strictly app-only?

**Also fold in:** strengthen the `.vault.example` comment so `SCHEDULER_SHARED_SECRET` is flagged
as a HARD startup dependency (API.Callback faults at startup if it's unset).

**Raised:** 2026-06-26, after the merged-main redeploy validated OTP login. Owner approved; not
done in that session.

---

## NS-4. Dashboard redesign — backend feature gaps surfaced by the FE↔BE review — *Open*

Found while planning the cardholder Dashboard redesign.
These are **missing/under-exposed features** the new design wants but the back-end does not
currently provide to the front end. None are bugs in shipped behaviour; they are scope the
redesign either builds (Phase 3) or hides for v1.

- **a) No balance-level deposit address exposed to the FE.** The prototype dashboard shows a
  standing balance + deposit address up front. `getDashboardData` returns commission/card
  stats only; `getBalance` returns a balance with **no address**; the only deposit `Address`
  hangs off `tblT_Card_Deposit` (per-card). The reusable balance-deposit capability exists in
  the INT tier (`WalletService`) but is **not plumbed to the Dashboard API**.
  → **Built** in the dashboard redesign money-path (security-reviewed).
- **b) Dashboard "recent activity" feed missing.** The prototype shows recent transactions
  across the account; today transactions are **card-scoped only** (`trxCardList`). No aggregate
  per-user feed exists. → **Decided: card-scoped for v1**; aggregate feed deferred.
- **c) 2FA toggle not exposed to the cardholder FE.** `enable2FA`/`get2FA` + `tblM_User_2FA`
  exist in the INT/WCF `UserV1Service`, but there is **no Dashboard-API route or `UserService`
  method** for them, so the Settings page can't read/flip 2FA without new plumbing. (Open
  question: is 2FA meant to be user-optional or mandatory?) → **Decided: hidden in v1**, deferred until mandatory-vs-optional is resolved (see [`deferred.md`](deferred.md) and the 2FA item in [`security-findings.md`](security-findings.md)).
- **d) No per-card-type card artwork from the backend.** `CardTypeModel` has no image/art/URL
  field, so the redesigned cards/buy/detail pages have no real per-BIN card render — only the
  prototype's static design assets. → **Decided: add a card-art field** (distinct per type, brand fallback).
- **e) No Settings/account features for: account deletion ("danger zone") and notification
  preferences.** No endpoints exist; hidden in the Settings page for v1.

**Found:** Dashboard FE↔BE review + redesign planning, 2026-06-26.

---

## NS-5. Referral/commission system was scaffolded but never paid out — *Resolved*

Surfaced while reviewing the cardholder Settings page (name/password/email-change/referral) for
FE↔BE wiring. The referral and commission surfaces were fully scaffolded but **earned nothing**:

- `tblM_User_Referral` (a per-user referral code) and `tblM_User.InvitedBy` (who referred whom)
  are written at registration, and the dashboard shows a "Commission Rate" + "Total Commission"
  tile and a "Commission history" panel — but **nothing ever wrote a `tblT_Commission` row**, so
  the earnings ledger was permanently empty and the rate was display-only, applied by nothing.
- The other Settings features were genuinely wired: first/last-name and password change (the
  latter verifies the current password), and email change is a real two-step OTP flow (binds the
  new address to the OTP at request time, re-checks uniqueness at confirm).

**Resolution:** the earn side is now built — a referrer is paid a share (default 10%) of the
platform **fee** on a referee's confirmed card buy/top-up, credited to their wallet balance, with
a loss-proof cap, per-order dedup, and an external red-team (Opus + Sonnet).

**Found:** Settings FE↔BE review, 2026-06-26.

---

## NS-6. Password-reset (and sibling) email links hardcoded to the wrong domain — *Open (fix recommended)*

**Symptom.** The cardholder "Forgot password" email sends a reset link to
**`https://kash.cards/newpassword?id=…`** — the *production* domain — regardless of environment.
On the dev box the link therefore leaves the dev deployment (different host, different DB), so a
dev-generated reset id can't be exercised and the flow is untestable from `app-dev.s16.xyz`.

**Root cause.** The base URL is a hardcoded literal, not environment-derived:
`QryptoCard.INT/Model/KeyModel.cs:23` —
`public static string QRYPTO_URL_FORGOT_PASSWORD = "https://kash.cards/newpassword?id=";`
consumed by `NotificationMailkitService.cs:350` (`id = KeyModel.QRYPTO_URL_FORGOT_PASSWORD + id`).
The sibling `WASABICARD_API_URL` directly above it (line 22) was already converted to
`SecretsConfig.Require(...)` by SD-1; this URL was missed.

**Same class of bug (other hardcoded, environment-wrong link domains):**
- `QryptoCard.INT/Script/Service/Admin/v1/AdminV1Service.svc.cs:662` — admin invitation link →
  `https://admin-dev.qrypto.trade:88/InvitedAccount?id=…` (a *different*, stale dev domain —
  `qrypto.trade:88`, not `s16.xyz`).
- `QryptoCard.INT/Model/KeyModel.cs:15` `QRYPTO_PAY_URL` and
  `QryptoCard.Dashboard.Admin/Models/KeyModel.cs:17` `PAYMENT_LINK` — legacy `qrypto.trade` pay
  links (likely unused in kash-cards; confirm and remove or env-ify).

**Impact.** Forgot-password and admin-invite flows can't be tested on dev. In prod the cardholder
reset happens to land on the right host (kash.cards) but is not environment-aware; the admin invite
points at a dead `qrypto.trade:88` host even in prod. A wrong silent default on an auth-bearing link
is the same risk class SD-1 fixed for the provider URL.

**Recommended fix.** Make the link base URL environment-derived like SD-1 did for
`WASABICARD_API_URL` — read it from config (e.g. a `DASHBOARD_BASE_URL` / `QRYPTO_URL_FORGOT_PASSWORD`
env var seeded per environment, defaulting to the dev host) so dev links resolve to `app-dev.s16.xyz`.
Audit the admin-invite + pay links in the same pass. Touches auth-adjacent notification code — low
risk, worth a quick internal review.

**Found:** Phase 2 shakeout click-through (forgot-password), 2026-06-27.

---

## NS-7. Login button (and other `.btn` inputs) lack a pointer cursor on hover — *Open (trivial fix)*

**Symptom.** The "Sign in" button on the cardholder login page shows the default arrow cursor on
hover instead of the pointer that signals clickability — inconsistent with the links and other
controls on the page, which do show the pointer.

**Root cause.** The button is an `<asp:Button>`, which renders as
`<input type="button" class="btn btn-primary btn-block btn-lg">` — an `<input>`, **not** a
`<button>`. In `Content/css/kash-auth.css`:
- `button { … cursor: pointer; … }` (line 62) gives `<button>` elements a pointer, and `<a>` links
  get one by default —
- but the `.btn` class (line 97) does **not** set `cursor`, so a `.btn` that renders as an `<input>`
  falls through to the default arrow.

**Impact.** Cosmetic affordance only — the button works. Affects any `.btn` rendered as
`<input>`/`<asp:Button>` across the NewDesign auth pages (login, register, forgot-password,
new-password).

**Fix.** Add `cursor: pointer;` to the `.btn` rule in `kash-auth.css` (covers all `.btn` regardless
of element type). CSS-only — deployable via `deploy.sh sync` (no rebuild). Worth checking the
in-app shell CSS (`app.css` / `premium.css`) for the same `.btn`-without-`cursor` gap.

**Found:** Phase 2 shakeout click-through (login page), 2026-06-27.
