# Dashboard — Wiring Correctness & NewDesign Adoption

## Status checklist (as of 2026-06-26)

📝 **Draft for alignment.** Produced from a read-only front-end↔back-end review of
the **cardholder Dashboard** (`QryptoCard.Dashboard`, *not* the Admin app). The
review traced every interactive control (buttons, link-buttons, anchors,
grid/repeater commands, JS `onclick`) to its code-behind handler and on to the
`Services/*` → `AuthClient` → `/v1/*` REST call, and walked the core customer flow
end-to-end (sign up → OTP → login → buy card → deposit).

**Headline:** the Dashboard is in good shape — **no primary action button is
broken.** Every `OnClick`/`onserverclick` resolves to a real, non-stub handler;
every `Services/*` method maps to a present `/v1/*` endpoint; the full
sign-up→OTP→login→buy→deposit chain is wired. Defects are one real functional bug,
a handful of design-independent code-behind nits, some dead placeholder links, and
one **information-architecture gap** (no balance-level deposit address on the
dashboard) that the redesign requires us to close.

**By phase:**
- [ ] **Phase 1 — Wiring & correctness fixes** (design-independent; ship first) → §1
- [~] **Phase 2 — NewDesign visual/IA adoption** — pre-login pages **done via PR #35**;
      logged-in app pages + sidebar chrome + Settings remain → §2
- [ ] **Phase 3 — Wallet UI = Plan 07 T4.5** (the money engine is already shipped &
      red-teamed; this exposes the read methods to the Dashboard tier + builds the UI) → §3
- [ ] **Opportunistic refactors** (dedup/rename, folded into the touching phase) → §4

**Decisions locked (signed off 2026-06-26):**
- **DD-0 Doc shape:** one umbrella doc (this file), executed as **separate phases /
  slices with separate PRs**.
- **DD-1 Sequencing:** **bugs first, then redesign** — Phase 1 ships before Phase 2.
- **DD-2 Redesign depth:** **full NewDesign adoption** — match the prototype's look
  *and* information architecture, including the dashboard balance/deposit-address
  experience (needs new BE plumbing, Phase 3).
- **DD-3 App chrome:** **rebuild the shared `Site.Master` into the sidebar layout.**
  Every authenticated page already references `Site.Master`, so they inherit the new
  chrome at once; one frame to maintain. (Pre-login pages stay standalone — they have
  no app chrome to share — per PR #35.)
- **DD-4 Design-token base:** **vendor `premium.css` into `Content/css/`** as the
  shared base loaded before `app.css`. `app.css` (the dashboard design) already
  expects premium.css's `:root` tokens, and premium.css is **not in the repo** (it
  lives only in the untracked local `NewDesign/` prototype) — so we capture it now.
- **DD-5 Settings page:** **build `settings.aspx` now, but hide unsupported features.**
  Wire what the backend supports (profile, change password, change email-via-OTP,
  referral); hide what it doesn't (account-deletion "danger zone", notification
  prefs — no endpoints). This also fixes the broken `~/account` link (M1) by giving
  it a real target.
- **DD-6 PR granularity:** **Phase 1 = one PR** (four small, cohesive, design-
  independent fixes, no red-team); **Phase 2 & 3 = one PR per slice.** All PRs branch
  off `main` from a worktree, **never stacked**; where a slice depends on a prior one,
  the prior **merges first** and the next branches off the updated `main`. Merge to
  `main` stays a human gate.
- **DD-7 Card artwork: distinct per card type.** Add a nullable art/image field to the
  card-type data (INT card-type service → API `/v1/card/type` → `CardTypeModel`),
  images under `Content/media/cards/`, with the static brand card as fallback. Non-
  money; light review. Folded into the cards re-skin (Slice 2.3). (NS-4d.)
- **DD-8 v1 scope cuts (deferred, hidden in the UI):** **2FA toggle hidden** — backed
  in INT (`enable2FA`/`get2FA`) but not exposed to the FE, and mandatory-vs-optional is
  unresolved (Plan 4); login already enforces email-OTP, so users aren't unprotected.
  **Recent-activity feed card-scoped** — reuse existing card-scoped transaction data, no
  aggregate endpoint in v1. **Account-deletion "danger zone" + notification prefs
  hidden** — no backend. (NS-4 b/c/e.) **Mobile master + ViewSwitcher removed** in
  favour of the responsive design. GSAP/Lenis stay CDN on marketing pages and are not
  pulled into the app pages.
- **DD-9 Money-flow pages — skin + rewire together in Phase 3.** The deposit/buy/top-up
  pages change behaviour under Plan 07's wallet-only model (R1), so Phase 2 re-skins only
  the **flow-stable** pages; `txdeposit` (→ wallet deposit), `card/carddetail` **Buy**
  (→ pay-from-balance), and `card/mycarddetail` **top-up** (→ pay-from-balance) are
  re-skinned **and** rewired together in Phase 3, so each is touched once.

---

## Current state — reconciliation with PR #35

PR #35 ("New design: landing page + pre-login auth flow", merged 2026-06-26) already
executed the marketing/auth half of Phase 2:

- **Re-skinned, standalone (no master page), to the dark/cyan design:** `Default.aspx`
  (landing), `login`, `register`, `otplogin`, `otpregister`, `forgotpassword`,
  `newpassword`. Code-behind and `.designer.cs` **untouched** — all server controls
  and handlers preserved.
- **Assets landed under project convention:** `Content/css/{landing,kash-auth}.css`
  (self-contained — own `:root` tokens) + `Content/css/app.css` (the **dashboard +
  OTP** design — sidebar layout, balance/card/stat/txn/referral panels) and
  `Content/js/{landing,kash-auth}.js`, media under `Content/media/landing/`.
- **Product change:** register dropped First/Last name (backend doesn't capture them).
- **`NewDesign/` is now untracked + gitignored** — kept on disk locally as a visual
  reference only; it is no longer part of the app.

**What this means for the plan:**

| Item | State |
|---|---|
| Phase 2 · pre-login re-skin (was Slice 2.2) | ✅ **Done by #35** |
| Phase 1 (code-behind fixes F1–F6) | ✅ **Unaffected** by #35 (no code-behind touched) — still valid, ready now |
| M3 (logo → `index.html`) | ✅ **Fixed by #35** (logo now `href="/"`) |
| M4 (dead `href="#"` Terms/Privacy) | ⚠️ **Persists** in the new auth markup (register consent label) |
| M6 (Default.aspx dead email input) | Re-verify against the rewritten `Default.aspx` |
| `app.css` dashboard styles | ⚠️ **Inert** — depend on `premium.css` tokens that are **not in the repo** (DD-4 vendors them in) |
| Logged-in app pages | ❌ Still old top-nav `Site.Master` + `style.bundle.css` (Metronic) — the Phase 2 remainder |

---

## Context

`QryptoCard.Dashboard` is the cardholder site: legacy **ASP.NET Framework 4.x
WebForms** (`.aspx` + code-behind), IIS-hosted. Pages render server controls whose
handlers call `Services/*` (`UserService`, `CardService`, `AuthClient`), which
POST/GET JSON to the user-tier REST API (`QryptoCard.API`, `/v1/*`) via `AuthClient`
(Bearer attach + silent 401-refresh). The REST tier delegates to the **INT (WCF)
money tier** (`QryptoCard.INT`).

**Two layers, treated differently.** The review findings split cleanly:

| Layer | Survives a redesign? | Treatment |
|---|---|---|
| **Code-behind / BE** (logic, URLs, error handling, validation) | Yes — design-independent | Fix now in **Phase 1** |
| **Markup / FE** (links, dead anchors, copy-button postbacks, layout) | No — markup gets rewritten | Fix **as part of** the re-skin in Phase 2, not twice |

**Relationship to Plan 07.** The "deposit into your balance" experience the prototype
dashboard shows is the money path shipped in
[`07-runegate-prepaid-balance.md`](07-runegate-prepaid-balance.md) — deposit to a
Runegate address → reusable prepaid balance → spend on cards. That logic already
exists in the **INT tier** (`QryptoCard.INT/Script/Service/WalletService.cs`,
`CardSpendService.cs`, the Callback tier). What's missing is the **Dashboard-API →
front-end plumbing** to fetch a balance-level deposit address, and the dashboard UI
to show it. **Phase 3 builds on Plan 07; it does not reinvent the money path.**

---

## 1. Phase 1 — Wiring & correctness fixes (design-independent)

Small, surgical, low-risk edits to code-behind and one model. They survive the
redesign, ship first as **one PR**, and give fast verifiable wins. Line numbers
verified against the current tree; PR #35 did not touch any of these.

| # | Severity | What's wrong | File:line | Fix |
|---|---|---|---|---|
| F1 | **Bug** | **Frozen deposit-expiry countdown.** `getCounter()` compares `hfStatus.Value == "creatred"` — a typo for `"created"`, the only value the status is set to. The `asp:Timer` fires every second but the body is skipped: the "Expires in" countdown never decrements *and* the page posts back every second doing nothing. | `txdeposit.aspx.cs:227`, `txcard.aspx.cs:214` | Compare to `"created"`. (Also tidy the always-true bitwise `\|` guard beside it.) |
| F2 | **Risky** | **Card navigation hardcoded to the dev host.** Four nav URLs point at `https://dash-dev.kash.cards/...`, so tile clicks and post-buy redirects leave the current host on any non-dev environment. (`API_URL` above them is correctly config-driven.) | `Models/KeyModel.cs:17-20` (`REFERRAL_URL`, `DETAIL_URL`, `DETAIL_OWN_URL`, `TXCARD_URL`) | Make relative (`ResolveUrl("~/card/...")`) or config-driven. |
| F3 | **Risky** | **Swallowed errors → no feedback.** On a non-success result `txdeposit`/`txcard` do a bare `Response.Redirect("~/dashboard")`; `cardlist` binds null (blank list); `carddetail` leaves "0 USD". | `txdeposit.aspx.cs:185`, `txcard.aspx.cs:171`, `cardlist.aspx.cs:75-80`, `carddetail.aspx.cs:136-141` | Surface `op.Message` in the existing alert modal. |
| F4 | Minor | **Login empty-field guard too weak.** `if (txtEmail.Value == "" && txtPassword.Value == "")` only blocks when **both** are blank. | `login.aspx.cs:48` | `&&` → `\|\|`. |
| F5 | Minor | **Dead referral-grid pager.** `gvReferralList` sets `AllowPaging` but declares no `OnPageIndexChanging` and binds only on `!IsPostBack`. | `dashboard.aspx:258` | **Deferred OUT of Phase 1 to the redesign** — the Phase-2/3 dashboard replaces this grid; keeping it out keeps Phase 1 off `dashboard.aspx` (no two waves touch one file). |
| F6 | Cosmetic | Forgot-password error reads "Email and password cannot be empty" on an email-only page. | `forgotpassword.aspx.cs:45` | Correct the wording. |

**Verification:** build Dashboard + test projects; run unit/integration suites;
manually confirm the countdown ticks (F1) and an induced error now shows a message
(F3). State pass/fail/skip counts in the PR.
**Red-team:** **not necessary** — display/logic/validation only, no security/money/
auth/data-integrity surface.

---

## 2. Phase 2 — NewDesign adoption (logged-in app; one PR per slice)

Pre-login pages are done (PR #35). The remainder re-skins the **authenticated app**
to the prototype, rebuilds the shared chrome as a sidebar (DD-3), and builds the
Settings page (DD-5). Markup-level review findings (§2.6) are fixed here, as the
markup is rewritten. Each slice keeps the existing wired server controls and only
changes markup/styling, unless noted.

### 2.1 Chrome + design-system foundation (merges first)
- **Vendor `premium.css`** (the token/base layer) from the local prototype into
  `Content/css/`, registered in the csproj + `BundleConfig`, loaded before `app.css`
  (DD-4) — this is what makes `app.css`'s dashboard styles functional.
- **Rebuild `Site.Master`** from the old top-nav Metronic shell into the prototype's
  **sidebar** chrome (sidebar nav, balance widget, user block, footer), per `app.css`
  `.dash-*`. Keep all server bindings (`lblBalance`, name/email, repeater `DetailURL`)
  — restyle only. Every authenticated page inherits it at once (DD-3).
- Drop `style.bundle.css`/Metronic JS from the master once the sidebar is in.

### 2.2 Pre-login re-skin — ✅ DONE (PR #35)
Landing + the six auth pages. **Residual cleanup only:** fix the persisting dead
`href="#"` Terms/Privacy links (M4) and re-verify the `Default.aspx` email input (M6)
— folded into 2.1 or a tiny PR.

### 2.3 App pages re-skin — flow-stable pages only (DD-9)
Re-skin only the pages whose **behaviour does not change** under Plan 07's wallet-only
model. The deposit/buy/top-up pages are deferred to Phase 3 (skin + rewire together).
- `card/cardlist.aspx` ← `buy-card.html` (browse). `card/mycardlist.aspx` ← `cards.html`.
- `card/carddetail.aspx` / `card/mycarddetail.aspx` — re-skin the **view** (OTP-gated
  sensitive details, history); the **Buy / top-up actions are deferred to Phase 3**.
- `txcard.aspx` ← `transactions.html` (card tx history — flow-stable).
  **`txdeposit.aspx` is deferred to Phase 3** (becomes the wallet deposit view).
- **Card artwork (DD-7):** add the nullable art field to the card-type data + API +
  `CardTypeModel`; render per-type art with the static brand card as fallback; images
  under `Content/media/cards/`.
- **Recent-activity feed (DD-8):** populate from existing card-scoped transaction data.
- `dashboard.aspx` ← `dashboard.html` — the **shell** (balance/cards/referral layout);
  the **wallet deposit-address panel is reserved here but built in Phase 3** (T4.5).
  Fold the dead-`copyAddress`-JS removal (M7) in here (one PR touches `dashboard.aspx`).

### 2.4 Settings page (build now, hide unsupported — DD-5)
- New `settings.aspx` from the prototype `settings.html`, on the new chrome.
- **Wire (backend exists):** profile name (`updateUserData`), change password
  (`updatePassword`), change email via OTP (`updateEmailOTP` + `updateEmail`), referral
  (`getReferralCode`/`getReferralJoined`).
- **Hide (v1 — DD-8):** account-deletion "danger zone", notification preferences, and
  the **2FA toggle** (backed in INT but not FE-exposed; mandatory-vs-optional unresolved
  from Plan 4 — deferred). Login already enforces email-OTP.
- Repoint the user-menu "My Profile" link here — **resolves M1**.

### 2.5 Dead-code & orphan cleanup
- Delete orphan `card/purchasecard.aspx` + `card/yourcards.aspx` (M8).
- Remove/rebuild the `Site.Mobile.Master` stub (M9) — likely remove (prototype is
  responsive).
- Remove dead `copyAddress(addr)` JS, `dashboard.aspx:386` (M7).

### 2.6 Markup-level findings folded into Phase 2

| # | Severity | What | File:line | Disposition |
|---|---|---|---|---|
| M1 | **Broken** | "My Profile" → `~/account` 404s. | `Site.Master:179` | **Resolved by 2.4** (repoint to `settings.aspx`). |
| M2 | Minor | Footer About/Support `href="#"` — `About.aspx`/`Contact.aspx` exist unlinked. | `Site.Master:290,293` | Link real pages in 2.1. |
| M3 | Minor | Logo → `index.html`. | (auth pages) | ✅ **Fixed by #35.** |
| M4 | Minor | Dead `href="#"` Terms/Privacy. | new auth markup | Fix in 2.2 residual. |
| M5 | Risky | Copy buttons force a full-page postback (`HtmlButton` submit + client `onclick`, no `return false`). | `dashboard.aspx:205,214`; `mycarddetail.aspx` Button1–11 | Resolved by the prototype's `type="button"` copy controls + `dashboard.js` in 2.1/2.3. |
| M6 | Minor | `Default.aspx` `txtEmail` collected but unused. | `Default.aspx:142` (re-verify post-#35) | Fix in 2.2 residual. |
| M7 | Minor | Dead `copyAddress` JS. | `dashboard.aspx:386` | Remove in 2.5. |
| M8 | Minor | Orphan empty pages. | `purchasecard.aspx`, `yourcards.aspx` | Delete in 2.5. |
| M9 | Minor | Mobile-master stub. | `Site.Mobile.Master:14` | 2.5. |

**Verification:** build + run suites after each slice; visually verify against the
prototype; confirm every server control still binds/posts back (a dropped control id
in a markup rewrite is the main regression risk).
**Red-team:** **light internal** per slice — guard against dropped wiring. No external
for pure presentation. (Settings 2.4 touches password/email change — confirm those
flows still validate; no money/auth-token surface.)

---

## 3. Phase 3 — Wallet UI (finishing Plan 07's last mile)

**Not net-new money work.** Plan 07 ("Runegate Prepaid Balance") is **core-shipped and
already externally red-teamed** — the deposit→credit→spend→ledger engine, the atomic
balance helper, the webhook credit branch, and the INT-tier **read methods
(`getDepositAddress`, `getLedger`, balance, pay-from-balance) all exist**
(`QryptoCard.INT/.../WalletService.cs`, `CardSpendService.cs`, `UserV1Service`). What
Plan 07 lists as *remaining* is **T4.5 — the user wallet dashboard UI.** Confirmed:
`QryptoCard.Dashboard` has **zero** wallet surface today, so the FE is **out of sync
with the shipped wallet backend** — users can't yet see or use their balance/address.
**Phase 3 ≡ Plan 07 T4.5** + exposing the shipped read methods through the Dashboard
REST API + `UserService` (which have nothing).

**The three decisions are resolved by Plan 07 (signed off when it merged):**

| # | Resolved |
|---|---|
| D-08-1 Address model | **Standing per-user TRC20/USDT static address**, provisioned at registration (`tblM_User_Crypto_Deposit`); reusable, any amount (Plan 07 R2/R3/R8). |
| D-08-2 Replace vs coexist | **Wallet-only (R1)** — card buy/top-up **pay from balance**; legacy per-card deposit path removed. Drives **DD-9** (the coupled money pages). |
| D-08-3 Fee | **Fee at spend** (existing per-card `RechargeFeeRate`); deposits credit **face value** (R9). Prototype's 3% client-side fee is illustrative — server is authoritative. |

**Work:** (3.2) expose `getDepositAddress`/`getLedger` through the Dashboard REST API +
`UserService`; (3.3) build the wallet UI on the redesigned dashboard (address + QR —
reuse the `txdeposit` QR component — balance, ledger) and **skin + rewire the coupled
money pages** to pay-from-balance (DD-9); (3.4) verify + red-team.

**Red-team:** **internal + targeted external.** The money *engine* is already
red-teamed under Plan 07; Phase 3's surface is the FE + the Dashboard→INT
pay-from-balance/read wiring — review IDOR on the new read endpoints and the
spend-trigger wiring, not the (already-hardened) credit/debit core.

**Still gated to launch/shakeout (Plan 07 leftovers, not this plan's code):** T2.7
cutover (drain open `Created` orders + brief new-order freeze), T1.4 unique indexes,
extending the Plan 3 forensic queries to the ledger.

---

## 4. Opportunistic refactors

Folded into whichever phase already touches the code; behavior-preserving, covered by
the existing suite.
- **Dedup `txdeposit` ↔ `txcard` code-behind** (near-identical; share F1/F3) — with
  Phase 1 or the Phase-2 re-skin.
- **Service-layer boilerplate** — a shared `AuthClient` execute-wrapper removes ~20
  identical try/catch copies in `Services/*`.
- **Dead code / renames** — `copyAddress` (M7), orphan pages (M8), mobile stub (M9),
  forgot-password wording (F6).

---

## 4b. Execution breakdown — phases → slices → tasks (checklist)

Each slice ≈ one reviewable PR. Plain-English intent first, then the task checklist.

### Phase 1 — Wiring & correctness fixes (ONE PR: `worktree-dashboard-wiring-fixes`)

**Slice 1.1 — Fix the frozen deposit-expiry countdown.**
The "Expires in" timer never counts down because the code checks the misspelled
status `"creatred"` instead of `"created"`, so the per-second refresh silently does
nothing while still hitting the server. We fix the typo on both deposit screens, clean
up the always-true bitwise guard, and pull the shared countdown/QR logic into one
place since the two files are near-identical.
- [ ] Fix `"creatred"` → `"created"` in `txdeposit.aspx.cs:227` and `txcard.aspx.cs:214`
- [ ] Correct the always-true bitwise `|` guard beside it
- [ ] Extract the shared countdown/QR/status helper from both files (refactor R1)
- [ ] Confirm the countdown decrements on a live deposit screen

**Slice 1.2 — Make card navigation environment-safe.**
Four navigation URLs are hardcoded to the dev host, so on any other environment
clicking a card tile or finishing a purchase bounces the user to dev. We swap those
four literals for relative/config-driven URLs like the `API_URL` above them.
- [ ] Convert `REFERRAL_URL`, `DETAIL_URL`, `DETAIL_OWN_URL`, `TXCARD_URL` in `KeyModel.cs:17-20`
- [ ] Verify card-tile clicks and the post-purchase redirect stay on-host

**Slice 1.3 — Surface errors instead of swallowing them.**
A backend failure on the deposit/card screens currently redirects silently to the
dashboard, and the card list/detail pages render blank or "0 USD" — the user never
learns anything failed. We route these to the alert modal each page already has.
- [ ] Show the error (not a silent redirect) in `txdeposit.aspx.cs:185` / `txcard.aspx.cs:171`
- [ ] Show a message on load failure in `cardlist.aspx.cs:75-80` and `carddetail.aspx.cs:136-141`

**Slice 1.4 — Validation & wording nits + service dedup.**
Login only blocks when *both* fields are blank (should be either), and forgot-password
shows an error mentioning a password field it doesn't have. We also dedup the
copy-paste try/catch across the service layer while those files are open.
- [ ] `&&` → `||` in `login.aspx.cs:48`
- [ ] Fix the wording in `forgotpassword.aspx.cs:45`
- [ ] Shared `AuthClient` execute-wrapper to remove ~20 copies in `Services/*` (refactor R2)
- [ ] Build + run suites; record pass/fail/skip in the PR

### Phase 2 — NewDesign adoption (one PR per slice; 2.1 merges first)

**Slice 2.1 — Chrome + design-system foundation.** *(merge before 2.3/2.4)*
This is the groundwork everything else needs. We bring the prototype's base
stylesheet (`premium.css`) into the project so the dashboard styles actually work,
then rewrite the one shared frame file (`Site.Master`) from the old top menu-bar into
the new left sidebar. Because every logged-in page already pulls its frame from that
file, they all get the new sidebar look at once. We keep the live data bindings
(balance, name, email) and only change the look.
- [ ] Vendor `premium.css` into `Content/css/` + csproj/`BundleConfig`, loaded before `app.css`
- [ ] Rebuild `Site.Master` as the sidebar chrome; preserve all server bindings
- [ ] Wire footer About/Support to real pages (M2); port `dashboard.js` copy behaviour (M5)
- [ ] Remove old Metronic `style.bundle.css`/JS from the master
- [ ] Visually verify the shell; confirm every master control still binds/posts back

**Slice 2.2 — Pre-login residual cleanup.** *(done by #35 except this)*
The login/signup pages are already re-skinned; this just clears the two leftover
markup nits the review found.
- [ ] Fix dead `href="#"` Terms/Privacy links in the new auth markup (M4)
- [ ] Re-verify / fix the `Default.aspx` email input (M6)

**Slice 2.3 — App pages re-skin (flow-stable pages only, DD-9).**
Re-skin only the pages whose behaviour doesn't change under Plan 07's wallet-only model,
keeping every wired handler — markup/styling only. The deposit/buy/top-up pages and the
dashboard wallet panel are deferred to Phase 3 (skin + rewire together).
- [ ] Re-skin `card/cardlist.aspx` (browse) and `card/mycardlist.aspx` (owned grid)
- [ ] Re-skin the **view** of `card/carddetail.aspx` / `card/mycarddetail.aspx` (Buy/top-up actions → Phase 3)
- [ ] Re-skin `txcard.aspx` (card tx history) — **`txdeposit.aspx` → Phase 3**
- [ ] Re-skin `dashboard.aspx` **shell**, reserving the wallet panel for Phase 3; remove dead `copyAddress` JS (M7) here
- [ ] Add card-art field (INT card-type → API → `CardTypeModel`) + render per-type art with brand fallback; images under `Content/media/cards/` (DD-7)
- [ ] Populate dashboard recent-activity from existing card-scoped data (DD-8)
- [ ] Confirm OTP gate + browse/view controls still work per page

**Slice 2.4 — Settings page (build now, hide what we can't back yet).**
Build the new Settings page from the prototype and wire up only the parts the backend
actually supports — editing your profile, changing your password, changing your email
(with OTP), and your referral info. The parts with no backend yet (deleting your
account, notification preferences) are hidden, not shown-but-broken. This also gives
the previously-dead "My Profile" menu link a real page to point at.
- [ ] Create `settings.aspx` on the new chrome
- [ ] Wire profile (`updateUserData`), password (`updatePassword`), email-OTP (`updateEmailOTP`+`updateEmail`), referral
- [ ] Hide account-deletion danger zone + notification prefs + 2FA toggle (DD-8)
- [ ] Repoint the user-menu "My Profile" link to `settings.aspx` (resolves M1)

**Slice 2.5 — Dead-code & orphan cleanup.**
Housekeeping the redesign makes safe — none of these are reachable in the live flow.
- [ ] Delete orphan `card/purchasecard.aspx` + `card/yourcards.aspx` (M8)
- [ ] Remove/rebuild `Site.Mobile.Master` stub (M9)
- [ ] (M7 dead-JS folded into 2.3's `dashboard.aspx` re-skin — keep one PR per file)

### Phase 3 — Wallet UI = Plan 07 T4.5 (engine already shipped & red-teamed)

**Slice 3.1 — Confirm the live spend/read behaviour (mostly done).**
The reconciliation is largely complete: Plan 07's engine + INT read methods exist,
D-08-1…3 are resolved, and the Dashboard has no wallet surface. The one open
confirmation is whether the live `/v1/card/open` + top-up already debit balance
(wallet-only shipped) so the existing FE actions are silently mismatched.
- [ ] Confirm live `openCard`/top-up debit balance (INT `CardSpendService`); note any FE mismatch
- [ ] Pin the `getDepositAddress`/`getLedger`/balance INT method signatures to wrap

**Slice 3.2 — Expose the shipped read methods through the Dashboard tier.** *(no chrome dep — parallelizable from the start)*
Add authenticated `/v1/user/...` routes + `UserService` methods that call the existing
INT `getDepositAddress`/`getLedger`/balance — with IDOR scoping + integration tests.
- [ ] Add the routes (`UserV1Controller`) delegating to INT; add `UserService` methods + models
- [ ] Integration tests: success, unauthenticated, cross-user IDOR

**Slice 3.3 — Build the wallet UI + rewire the coupled money pages (T4.5 + DD-9).**
Build the dashboard wallet panel (address + QR via the reused `txdeposit` component,
live balance, ledger), and re-skin **and** rewire `txdeposit`→wallet deposit,
`carddetail` Buy→pay-from-balance, `mycarddetail` top-up→pay-from-balance.
- [ ] Wallet panel on the dashboard (address + QR + balance + paginated ledger)
- [ ] `txdeposit.aspx` → wallet deposit view; remove the per-card deposit-address flow
- [ ] Buy + top-up → pay-from-balance ("Insufficient balance" path), server-authoritative fee
- [ ] Confirm end-to-end against a sandbox deposit → balance-credit → spend

**Slice 3.4 — Verification & targeted red-team.**
The credit/debit engine is already Plan-07-red-teamed; this pass covers the FE +
Dashboard→INT wiring. Internal red-team + a targeted external pass on the new read
endpoints (IDOR) and the spend-trigger wiring.
- [ ] Build + run suites incl. integration; record counts
- [ ] Internal red-team (IDOR on read endpoints, spend-trigger wiring, no double-submit)
- [ ] Targeted external red-team on the new FE/Dashboard surface → triage → re-verify
- [ ] Build + run full suite incl. money-path integration; record counts
- [ ] End-to-end sandbox deposit → balance-credit verification
- [ ] Internal red-team (endpoint, address allocation, fee math, auth)
- [ ] External model-diverse red-team (Opus + Sonnet) → triage → re-verify

---

## 5. Sequencing, PR strategy, verification & red-team

Organised into **waves** by dependency. Items within a wave run in **parallel**
(separate worktrees, disjoint files); waves gate on the prior wave's merges.

```
WAVE A — start now, 5 parallel worktrees (disjoint files)
   Phase 1   Wiring & correctness fixes         → 1 PR   (no red-team)
   2.1       Rebuild Site.Master → sidebar       → 1 PR   (MERGE FIRST — gates Wave B)
             + vendor premium.css
   2.2       Auth residual (M4/M6)               → tiny PR
   2.5       Cleanup: delete orphans + mobile     → small PR
   3.2       Expose getDepositAddress/getLedger   → 1 PR   (backend-only, no chrome dep)
             via Dashboard REST + UserService
        │
        ▼  (2.1 merged)
WAVE B — parallel page PRs off updated main
   2.3       Re-skin FLOW-STABLE pages only       → PR per page-group
             (browse, owned grid, tx history, card-detail view, dashboard shell)
   2.4       Settings page (hide 2FA/danger/notif)→ 1 PR
        │
        ▼  (2.1 + dashboard shell + 3.2 merged)
WAVE C — Phase 3 wallet UI (Plan 07 T4.5)
   3.3       Wallet panel + skin+rewire money pages → 1 PR   (DD-9)
   3.4       Verify + targeted red-team             → gates the 3.3 merge

LAUNCH/SHAKEOUT (gated, Plan 07 leftovers): T2.7 cutover, T1.4 indexes, forensics.
```

- **Max parallelism day one = 5 worktrees** (Phase 1, 2.1, 2.2, 2.5, 3.2). They can all
  be *in progress* at once, but **2.1 merges first** to unblock Wave B.
- **PR rules (DD-6):** every PR branches off `main` from a dedicated worktree,
  **never stacked**; a slice that depends on a prior one waits for it to **merge**,
  then branches off the updated `main`. Branch pushes inside a worktree are
  pre-approved; **merge to `main` is a human gate.**
- **No two PRs edit the same file:** fold the `dashboard.aspx` dead-JS removal (M7) into
  the 2.3 dashboard re-skin (not 2.5).
- **Verify before every gate:** build the Dashboard + test projects, run the relevant
  suites, state pass/fail/skip. "Verified" = ran it and saw it pass.
- **Red-team scaling:** none for Phase 1; light internal for Phase 2 (presentation —
  guard against dropped control wiring); **internal + targeted external** for Phase 3
  (the credit/debit *engine* is already Plan-07-red-teamed — Phase 3 reviews the FE +
  Dashboard→INT read/spend wiring, esp. IDOR on the new read endpoints).

## 6. Orchestration map (parallel sessions)

Each row = one session in its own worktree off `main`. Items in the same wave run in
**parallel** (disjoint files); waves gate on the prior wave's **merge**. Every slice
ends in a PR except 3.4 (red-team, which gates the 3.3 PR) and the launch/ops leftovers.

| Session | Slice(s) | Wave | Waits for (merge) | File scope (kept disjoint) | PR | Red-team |
|---|---|---|---|---|---|---|
| **S-A** | Phase 1 — wiring fixes (1.1–1.4) | A | — | code-behind only: `txdeposit/txcard.aspx.cs`, `KeyModel.cs`, `login/forgotpassword/cardlist/carddetail .aspx.cs` (**not** `dashboard.aspx` — F5 deferred) | `wiring-fixes` | none |
| **S-B** | 2.1 — sidebar chrome + vendor `premium.css` | A · **MERGE FIRST** | — | `Site.Master(+.cs/.designer)`, `Content/css/premium.css`, `BundleConfig`, `csproj` | `sidebar-chrome` | light internal |
| **S-B** (after, optional) | 2.2 auth residual + 2.5 cleanup | A | — | auth `.aspx` markup; delete `card/purchasecard.aspx`+`yourcards.aspx`; `Site.Mobile.Master` | `housekeeping` | none |
| **S-C** | 3.2 — expose wallet read methods | A | — | `QryptoCard.API` `UserV1Controller`, Dashboard `UserService` + models, integration tests | `wallet-read-endpoints` | internal (IDOR) |
| **S-D** | 2.3 — flow-stable re-skins (DD-9) | B | 2.1 | `cardlist/mycardlist/carddetail/mycarddetail(view)/txcard .aspx`, `dashboard.aspx` shell (+M7), card-art field | `app-reskin` (or per page-group) | light internal |
| **S-E** | 2.4 — settings page | B | 2.1 | new `settings.aspx`, `Site.Master` menu link | `settings` | light internal |
| **S-F** | 3.3 wallet UI + money-page rewire, then 3.4 RT | C | 2.1 + 2.3 dashboard-shell + 3.2 | `dashboard.aspx` wallet panel, `txdeposit.aspx`, `carddetail` Buy, `mycarddetail` top-up, Dashboard service | `wallet-ui` | internal + targeted external |

**Merge order:** `2.1` first (gates Wave B). `Phase 1` and `3.2` have no dependency —
merge any time. Wave B (`2.3`,`2.4`) branches off `main` **after 2.1 merges**. `3.3`
(Wave C) is last — it needs 2.1 + the 2.3 dashboard shell + 3.2. (If 2.3 ships as
per-page PRs, the **dashboard-shell** PR is the one 3.3 gates on.)

**Day-one parallelism:** S-A, S-B, S-C run at once. **Launch/shakeout** (Plan 07 T2.7
cutover, T1.4 indexes, forensics) is gated and not part of these sessions.

**Kickoff prompt per session** (point each at this doc):
- **S-A:** "In a worktree, execute Phase 1 (Slices 1.1–1.4) from `docs/plans/08-dashboard-wiring-and-redesign.md` — the design-independent wiring fixes; skip F5. Build, run the suites, open one PR. No markup, no other phases."
- **S-B:** "In a worktree, execute Slice 2.1 from `docs/plans/08-…`: vendor `premium.css` and rebuild `Site.Master` into the sidebar chrome, preserving all server bindings. Build, run tests, open a PR. Must merge before Wave B; optionally also do 2.2 + 2.5 as a small `housekeeping` PR."
- **S-C:** "In a worktree, execute Slice 3.2 from `docs/plans/08-…`: expose the INT `getDepositAddress`/`getLedger`/balance read methods through a new authenticated Dashboard REST route + `UserService` method, IDOR-scoped, with integration tests. Open a PR. Backend only."
- **S-D** *(after 2.1 merges):* "In a worktree off updated `main`, execute Slice 2.3 (flow-stable pages only, per DD-9) from `docs/plans/08-…`. Open a PR."
- **S-E** *(after 2.1 merges):* "In a worktree off updated `main`, execute Slice 2.4 (settings page; hide 2FA/danger/notifications) from `docs/plans/08-…`. Open a PR."
- **S-F** *(after 2.1 + 2.3 shell + 3.2 merge):* "In a worktree off updated `main`, execute Slices 3.3 + 3.4 from `docs/plans/08-…`: build the wallet UI (Plan 07 T4.5) and rewire deposit/buy/top-up to pay-from-balance; internal + targeted external red-team; open a PR."

### Coordination protocol (self-coordinating sessions)

Shared state lives in **GitHub, never in a repo file** (a repo file can't be seen across
worktrees until merged, and concurrent edits just conflict). **Claim = branch + draft PR;
dependency gate = PR merge state; dashboard = the tracking Issue
[#36](https://github.com/egunawan85/kash-cards/issues/36).**

Every session, on startup, follows this:
1. **Look before claiming.** Read Issue #36 + `gh pr list --state open`. If your slice
   already has an open branch/PR, it's claimed — stop and tell the user.
2. **Claim it.** Create `worktree-08-<slice>`, open a **draft PR**
   (`[08/<slice>] <name> — WIP`) immediately, and tick your row in #36. The PR is the lock.
3. **Check your gate.** If your row "waits for" X, confirm X's PR is **MERGED**
   (`gh pr view <X-branch> --json state -q .state` → `MERGED`, or it's in
   `git log origin/main`) before doing any work. If not → stop, report "blocked on X."
4. **Finish.** Mark the PR ready for review; set your #36 row to ✅ + link. **Never merge**
   (human gate). Stale claims from a dead session are the user's to reassign.

## Status

**Draft for alignment.** **Wave A is ready to execute now** — Phase 1, Slice 2.1
(chrome + `premium.css`), 2.2, 2.5, and 3.2 (Dashboard-tier exposure of the shipped
Plan 07 read methods) are independent and parallelizable; **2.1 merges first** to unblock
Wave B's page re-skins. Phase 3 is **no longer blocked** — D-08-1…3 are resolved by
Plan 07 and its engine is shipped; Phase 3's UI (3.3) depends on Wave B's dashboard
shell + 3.2.
