# Plan 1 — Secret Rotation

## Objective

Rotate every credential that was committed to the (formerly public) repository,
and move secrets out of source for good. **Premise: every value is already
compromised.** Rotating at the provider is mandatory; git-history scrub is a
later, cosmetic step that does not un-expose anything.

Full per-secret detail (file/line, truncated values, provider action, tracker
table) lives in [`../../tmp/rotation-runbook.md`](../../tmp/rotation-runbook.md).

## Organized into slices

- **Slice 1 — Secret-management foundation** (give rotated keys a home)
- **Slice 2 — Emergency provider rotation** (Wasabi keys, Runegate, SQL, email→Postmark)
- **Slice 3 — Stored credentials & internal keys** (admin swap; crypto keys → Plan 3)
- **Slice 4 — Repo hygiene & history scrub**

## Execution order (foundation first)

Rotation has two halves with different dependencies:
- **Provider revocation/reissue** (kills the leaked value) needs no destination —
  it starts **immediately, in parallel**, from the moment Slice 2 begins.
- **Storing the new value where the app reads it** needs a home — so **Slice 1
  runs first** to establish the gitignored `deploy/.vault` + the env-loader. Then
  Slice 2 writes each rotated value into `.vault` (never back into source).

The `deploy/` folder created in Slice 1 is the same one Plan 2 builds on
(self-contained, in-repo — decision D12), and `.vault`/`.env` is the secret home
(decision D13).

---

## Slice 1 — Secret-management foundation

- **T1.1 — Create the `deploy/` folder + secret convention.** Scaffold the in-repo
  `deploy/` folder (scripts adapted from `runegate-infra`, provenance-tagged), with
  a **gitignored `deploy/.vault`** (secrets) and **`deploy/.env`** (non-secret
  config), plus committed `.example` templates. This is the home rotated keys get
  written to and the base Plan 2 builds on.
- **T1.2 — Add the fail-fast secret accessor.** Introduce `SecretsConfig.Require` /
  `Preload` (env-only, cached, throws on missing — no fallback defaults), and call
  `Preload(...)` at `Application_Start` so a misconfigured pool fails to start with
  the complete missing-secret list. Reference:
  `runegate/PGCrypto.INT.Callback/Config/SecretsConfig.cs`.
- **T1.3 — Replace hardcoded secrets with env-backed loading.** Empty out the
  `KeyModel.cs` literals and the `Web.config` connection-string values, and load
  them via `SecretsConfig.Require(...)` / on-box connection-string assembly. After
  this, no secret remains in source — and rotated values flow in via `.vault`/env.
- **T1.4 — Secret minimization + guard test.** Provide each secret only to the pool
  that consumes it (e.g. WasabiCard private key → callback/INT only), and add a
  grep-pin build test that fails if an INT-only secret name reappears in a
  public-facing project.

## Slice 2 — Emergency provider rotation

Values go into `deploy/.vault`. **Timing (D6):** provider revocation/reissue is
*prepared* now, but the secrets that are **compiled into the app** (Wasabi keys,
Runegate key, the Gmail sender) only break the running site when swapped — so those
**execute at launch** as a coordinated cutover with the new server, not on the old
box. DB credentials (T2.3) are runtime config and *can* be hot-patched sooner.

- **T2.1 — WasabiCard credentials.** The RSA signing keypair is **ours** — we
  **generate a fresh 2048-bit keypair locally and register the new public key with
  WasabiCard** (no issuance needed); the **API key** is the one piece WasabiCard must
  re-issue. Coordinated cutover: WasabiCard must accept the new public key before we
  sign with the new private key. Only `WASABICARD_PRIVATE_KEY_XML` is actually used —
  drop the redundant key encodings. Also **request WasabiCard's merchant audit log**
  (feeds Plan 3 forensics). 🧑 API key + register pubkey · 🤖 generate keypair, `.vault`.
- **T2.2 — Runegate / PGCrypto key.** Runegate is **your own gateway** (the sister
  repo *is* PGCrypto), and kash-cards' outbound calls are currently **stubbed off**
  (`QRYPTO_ENVIRONMENT="dev"` → fake addresses), so rotating the key has **no live
  runtime impact today**. Rotate the merchant key in your Runegate admin; confirm it
  isn't shared with qrypto-omni. The real work — *wiring Runegate up properly +
  enabling webhook signing* — lands in **Plan 3**, not here. 🧑 + 🤖.
- **T2.3 — Azure SQL login passwords.** Reset the **`gendb-dev`** and **`qrypto`**
  logins (the live ones; connection strings are runtime config, so hot-patchable
  without a rebuild). **Verify against the *deployed* server first** — committed
  configs disagree with each other — and fold into the **DB consolidation** (D7):
  run `tmp/db-consolidation-checks.sql` to pick the canonical DB and detect any
  data split before rotating. 🧑 Azure · 🤖 `.env` (server/user) + `.vault` (`DB-PASSWORD`).
- **T2.4 — Email → Postmark.** Not a spacemail rotation — that SMTP code is **dead**.
  The *live* sender is Gmail/`no-reply@qrypto.trade` (admin + alert mail only; user
  OTP/reset mail is currently commented out). **Build Postmark + `kash.cards`** to
  replace it, **re-enable the disabled user OTP/reset emails** (needed for the Plan 3
  OTP fix), retire the Gmail creds, and delete the dead SMTP classes. 🧑 Postmark
  keys → `.vault` · 🤖 integrate.

## Slice 3 — Stored credentials & internal keys

- **T3.1 — App encryption keys (`DBKey`/`APPKey`) → MOVED to Plan 3 (D16).** Rotating
  these is a **data migration**, not a config swap: `DBKey` protects 5 live DB columns
  (user/admin passwords, API secret keys, 2FA secrets). It's bundled into the Plan 3
  crypto upgrade at launch — passwords→bcrypt (+forced reset), API secrets→bcrypt,
  2FA→AES-GCM under a new key — so the old keys are mostly **retired, not rotated**.
- **T3.2 — Admin account swap (`tblM_Admin`).** Real admins are **database rows**, not
  config. **Seed `edward@s16.ventures`** as the first admin and **disable the dev's**
  `syapril@qrypto.trade` row. Delete the dead `KeyModel` defaults (`USER_PASSWORD`/
  `ADMIN_PASSWORD` = `qwerty`/`12345678`) — they're vestigial, not real logins.
- **T3.3 — Strip dev secrets/paths (sister approach).** Remove the dev SMTP Gmail
  app-password, the WasabiCard sandbox key+RSA, and the dev's **personal** Gmail
  app-password from source (he revokes it on his side). The dev's broader access
  offboarding is owned by you, tracked separately.

## Slice 4 — Repo hygiene & history scrub

- **T4.1 — Harden the repo against future leaks.** Add explicit `.gitignore` entries
  (`.env`, `.vault`, `*.pem`, `*.pfx`, `*.p12`, `*.key`, `*.local.config`, `*.bak`,
  `*.orig`), enable GitHub secret scanning, and add a `gitleaks`/grep-guard
  pre-commit hook (sister `scripts/guards/G-*.rule` pattern).
- **T4.2 — Scrub git history.** Only after every secret is confirmed rotated, squash
  to a clean root commit (secrets removed) and force-push, or run `git filter-repo`.
  Cosmetic — not a substitute for rotation.

---

## Verification

- Each provider's old credential is confirmed **rejected** (an API call with the old
  key → 401/failure).
- App builds and runs reading secrets from env/`.vault`; a deposit/card smoke test
  passes.
- `grep` of the tree for the known leaked values (and `akbarmc:akbarmc`) returns
  nothing in source.
- `SecretsConfig.Preload` faults the pool when a secret is removed (negative test).

## Risks

- **Compiled-in secrets break the live app when swapped.** Wasabi keys, the Runegate
  key, and the Gmail sender are compiled into `KeyModel.cs`, so they're swapped at the
  Plan 2 cutover, not on the old box. Only DB credentials (config) are hot-patchable.
- **Leaked `DBKey` ⇒ all stored passwords were decryptable** (reversibly encrypted, not
  hashed) → forced password reset is mandatory (D15), handled in the Plan 3 migration.
- Rotating the Runegate key could affect a sister deployment — confirm it isn't shared
  with qrypto-omni before rotating (T2.2).
- Until the new server (Plan 2) reads from Key Vault, rotated values live in the local
  gitignored `.vault`; the old server is already compromised and will be decommissioned,
  so the marginal exposure is acceptable.
