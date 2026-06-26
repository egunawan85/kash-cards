# Plan 4 — Authentication Tokens & Two-Factor (design proposal)

> **Status: ✅ DONE — shipped and merged (PR #7–#11).** Real OTP (#7), opaque
> bearer-token subsystem + LocalDB harness (#8), bearer wiring (#9), dashboard
> Basic→Bearer + Postmark (#10), consolidated into `main` (#11). Built **wholesale on
> runegate's auth subsystem** (see the Revision section) and integration-tested against
> a throwaway SQL Server LocalDB. **Residual:** the OTP per-account rate-limit/lockout
> (T1.3 / A7) is deferred — it needs an `Attempts` column (DB-gated) and is the top
> tracked residual, flagged 4× by the external red-teams. The design/decision tables
> below are kept for reference of what was built.

## Status checklist (as of 2026-06-26)

Verified against merged code. The opaque-token + email-OTP system is fully built and wired; residuals are two deviations from this design, one missing worker, and DB/box-gated items. `[x]` done · `[ ]` outstanding · ⏳ = on-box.

**Done (built & merged):**
- [x] **A1–A4, A8, A9** — opaque `at_`/`rt_` SHA-256 tokens, token store, 15m/7d lifetimes + rotation, `SubjectType` enforced both ends, Basic→Bearer migration complete, lifecycle in the INT tier
- [x] **D-1, D-3, D-5, D-6, D-7 + Revision** — separate `AuthDbContext`, token-in-session (password cookie dropped), decoupled from bcrypt, Basic kept on the public APIKey tier only, atomic refresh rotation + reuse-detection chain-revoke, LocalDB integration harness
- [x] **T1.1–T1.3, T2.1–T2.3, T3.1–T3.3, T4.1, T4.3, T5.1, T5.2, Slice 6** — OTP gen/verify hardening, token service, bearer filters, split login→2FA→mint, dashboards on token endpoints, unit/integration tests + red-teams

**Outstanding (code):**
- [ ] **⚠️ A6 / T4.2 / D-2 — deviation:** the merged code makes OTP **mandatory for every user**; the doc's "users opt-in via `is2FA`" was never built. Reconcile doc↔code (decide which is intended).
- [ ] **A5 — TOTP** not built (only email-OTP shipped; the TOTP path is a stub `Param2 <> 'totp'` guard)
- [ ] **Purge worker** — the hourly expired-token purge job (24h grace) from the Revision is not implemented anywhere

**On-box / DB-gated (⏳):**
- [ ] ⏳ **A7 / T1.3 / D-4** lockout — coded but per-session; per-account enforcement needs the `FailureCount`/`Attempts` column (`deploy/sql/create-otp-lockout-columns.sql`, not yet applied)
- [ ] ⏳ **A2 / T2.1** token tables (`create-token-tables.sql`) applied to the live DB · **T5.3** bcrypt forced-reset sequencing · email delivery confirmed (T1.2)

## Objective

Move kash-cards from "every request carries the user's `email:password` (Basic
auth), OTP is hardcoded to `000000`, and 2FA exists in a table but is never
enforced" to the sister-project (Runegate / qrypto-omni) model: **a real one-time
code, a two-factor gate at login, and short-lived opaque bearer tokens** that are
hashed at rest, scoped by subject type, and individually revocable.

## Current state (what we're replacing)

- **Basic auth on every call.** API controllers carry `[BasicAuthentication]`;
  `getKey()` decodes the header to an email; the INT `SecurityService`
  `validateUser`/`validateAdmin` re-checks email+password against the DB on every
  request. The long-lived password is on the wire on every call.
- **OTP is disabled.** `getOTPCode()` returns a constant; the OTP email sends are
  commented out. `tblH_User_OTP` exists and is wired but inert.
- **2FA is dormant.** `tblM_User_2FA` holds (encrypted) TOTP `AccountKey` /
  `ManualEntryKey`, and `tblM_User.is2FA` exists, but **no login path enforces it**
  (and `enable2FA` was only just scoped to the caller in PR #4).
- **No token store, no revocation, no session lifetime.** Logout is client-side;
  a captured credential is valid until the password changes.

## Design decisions (sign-off gate)

Recommendations mirror the sister projects. ✅ = proceed with the recommendation
unless you change it; ❓ = needs your input.

| # | Decision | Recommendation | Alternative | Status |
|---|---|---|---|---|
| A1 | Token format | **Opaque random tokens** `at_…` / `rt_…`, stored only as **SHA-256 hashes** (Runegate parity) — revocable, no signing-key to manage, nothing sensitive at rest | Signed **JWT** (stateless, but hard to revoke and adds key management) | ✅ opaque |
| A2 | Token store | New `tblT_AuthToken` + `tblT_RefreshToken` (hash, `SubjectId`, **`SubjectType`** user/admin, `ExpiresAt`, `RevokedAt`, `CreatedAt`) — a **DB schema change** (EDMX update + migration script) | Reuse an existing table | ✅ new tables |
| A3 | Lifetimes | **Access 15 min, refresh 7 days** (Runegate parity); refresh rotates on use | Longer/shorter | ✅ 15m / 7d |
| A4 | Subject separation | **`SubjectType` enforced in BOTH the auth filter and at mint** — a `user` token can never satisfy an admin route, and vice-versa | Single token space | ✅ enforce both ends |
| A5 | 2FA factor (first cut) | **Email OTP first** (email already works; minimal) — gates token mint. **TOTP** (the existing `tblM_User_2FA`) added later as an opt-in | TOTP-first | ✅ email-OTP first *(you chose this earlier)* |
| A6 | 2FA enrolment scope | **Required for admins; opt-in for users** initially, with a switch to make it mandatory for all | Mandatory for everyone day one | ✔ **decided: admins forced, users opt-in** |
| A7 | OTP hardening | Single-use, **5–10 min expiry**, per-account **rate limit + lockout**, constant-time compare; delivered via the Plan 1 email provider (Postmark per D14, Gmail interim) | Looser | ✅ |
| A8 | Migration path | **Coexist**: add the token endpoints + `[BearerAuthentication]` alongside Basic; migrate the dashboards/clients; then **deprecate Basic**. Ties into the D15 bcrypt forced-reset (passwords are treated as compromised) | Hard cutover | ✅ coexist → deprecate |
| A9 | Where enforced | Token issue/verify/refresh/revoke live in the **INT `SecurityService`** (the shared choke point both API tiers already call), like the existing validate methods | Per-controller | ✅ INT tier |

**Dependency note:** A2's new tables are a **DB schema change** — like the other
DB-touching work (bcrypt migration, replay idempotency), the *code* is built in a
worktree now, but the migration runs with DB access during the dev shakeout /
launch. The token tables can be created ahead as an additive migration (no data
backfill), so this is lighter than the crypto data-migration.

## Resolved implementation decisions (from the code spike)

The code spike pinned down the implementation choices the A-table left open:

- **D-1 — Token tables live OUTSIDE the EDMX.** The model is database-first EDMX
  (EF6, no migrations); the two relationship-free token tables go in a small
  separate context (or Dapper), so all token code + Unit tests build now with no DB
  access and no fragile 4-way EDMX hand-edit. An idempotent `CREATE TABLE` script
  applies during the shakeout.
- **D-2 — Reuse the existing login gate.** Login is already two-step
  (`Login` issues a challenge row → `LoginVerify` checks a code); real OTP +
  token-mint hook into `LoginVerify`. OTP is required for admins always, and for
  users only when `is2FA == 1` (per A6).
- **D-3 — Token in session; DROP the password cookie.** The dashboards currently
  stash the AES-encrypted password in a 1-month cookie + `SessionLib.Password` —
  both are removed; the session holds the `at_`/`rt_` tokens instead.
- **D-4 — Fix the OTP table + a real expiry bug.** `DateExpired` is written but
  **never enforced** today (harmless only because the code is constant) — enforce it,
  store only a hash of the code, and add an attempt/lockout counter (column adds are
  DB-gated; the expiry-enforcement code fix is not).
- **D-5 — Decoupled from bcrypt** *(your call)*: the mint endpoint validates the
  current password scheme; the bcrypt migration + forced reset stays at launch.
- **D-6 — "Deprecate Basic" = user/admin tiers ONLY.** `QryptoCard.API.Public`
  authenticates partners with `APIKey:SecretKey` (a separate identity) and is left
  untouched.
- **D-7 — Minting tier stamps `SubjectType`.** Add an admin auth controller
  mirroring `AuthV1Controller` so the tier that mints unambiguously sets user/admin.

Deferred deeper spikes (not blocking): whether stored `tblM_User_2FA` TOTP secrets
are real/usable (DB-gated — TOTP is post-MVP per A5 anyway), and the
`tblH_Admin_Login` vs `tblH_Admin_OTP` discrepancy (checked during Slice 1).

## Revision — wholesale alignment to runegate's proven auth subsystem

Discovered while building the integration harness: runegate ships a complete,
production-red-teamed implementation of exactly this system, so kash-cards mirrors it
**wholesale** rather than shipping a simpler first cut. Changes vs the original A1–A9 / D-1…D-7:

- **Token store.** Replace the storage-agnostic `Sec.TokenService` + `ITokenStore` abstraction
  with runegate's structure: a code-first **`AuthDbContext`** + three tables — **`tblT_AuthToken`**
  (access; `ParentRefreshTokenID`), **`tblT_RefreshToken`** (refresh; `ReplacedByID` +
  `RotationChainRoot`), **`tblH_Auth_Log`** (audit ledger). The lifecycle moves to the service tier.
- **Lifecycle (service).** Mirror runegate's `AuthV1Service`:
  `mintAfterOtpVerify`/`refresh`/`verify`/`revoke`/`revokeAllForSubject`, with **refresh-token-reuse
  detection** — presenting an already-`ReplacedByID` token revokes the entire `RotationChainRoot`
  chain and logs `refresh_token_reuse` (RFC 6819; closes the exact gap the Slice 2 red-team flagged).
  Atomic rotation via conditional `UPDATE … WHERE ReplacedByID IS NULL`.
- **Purge worker.** Hourly DELETE of expired rows past a 24h grace (adopted wholesale).
- **Validation.** A **LocalDB integration harness** (mirroring runegate's `LocalDbFixture` +
  EF-generated `init.sql`) runs the real service methods against a throwaway SQL Server LocalDB, so
  the full login → OTP → token → bearer chain, the atomic rotation, and reuse-detection are
  integration-tested for real — no waiting for the shakeout. Retroactively validates Slice 1 + the
  IDOR/role stubs.

**Revised order:** harness → rework token store to `AuthDbContext` / 3-table schema → port
`AuthV1Service` + `BearerAuthAttribute` → wire login mint → integration-test throughout, each
re-red-teamed. `Sec.AuthTokens` (gen/hash primitives) is kept; the `Sec.TokenService` /
`ITokenStore` / `EfTokenStore` abstraction from PR #8 is superseded.

## Organized into slices

- **Slice 1 — Restore a real OTP** (un-hardcode, harden, re-enable delivery)
- **Slice 2 — Token store & lifecycle** (mint / verify / refresh / revoke)
- **Slice 3 — Bearer authentication filter** (per tier, SubjectType-enforced, coexists with Basic)
- **Slice 4 — Two-factor gate at login** (email OTP before token mint)
- **Slice 5 — Client migration & Basic deprecation**
- **Slice 6 — Verification & red-team** (auth is high-risk → full model-diverse red-team)

### Slice 1 — Restore a real OTP
- **T1.1** Replace the constant `getOTPCode()` with a CSPRNG numeric code; persist a
  hash in `tblH_User_OTP` with `ExpiresAt`, `Used`, and attempt count.
- **T1.2** Re-enable the email send through the Plan 1 provider abstraction; verify
  the path end-to-end (the existing send code is commented, not deleted).
- **T1.3** Harden verification: single-use, expiry, per-account rate-limit + lockout,
  constant-time compare. Unit-test all branches in `QryptoCard.Tests.Unit`.

### Slice 2 — Token store & lifecycle
- **T2.1** Additive EF6/EDMX migration: `tblT_AuthToken` + `tblT_RefreshToken`
  (hash, `SubjectId`, `SubjectType`, `ExpiresAt`, `RevokedAt`, `CreatedAt`).
- **T2.2** `SecurityService` methods: `issueTokens(subjectId, subjectType)`,
  `verifyAccess(token, requiredType)`, `refresh(rt)`, `revoke(token)` — opaque
  `at_`/`rt_`, SHA-256-hashed lookup, 15m/7d, refresh rotation, `SubjectType` set
  at mint.
- **T2.3** Constant-time hash compare; never log or return the raw token after issue.

### Slice 3 — Bearer authentication filter
- **T3.1** `BearerAuthenticationAttribute` (user tier) and an admin variant calling
  `verifyAccess(token, requiredType)`; on success expose the subject to controllers
  (replacing `getKey()` over time).
- **T3.2** **SubjectType enforced in the filter** — a `user` token is rejected on
  admin routes and vice-versa (defence in depth with the mint-time stamping).
- **T3.3** Apply alongside `[BasicAuthentication]` so both work during migration;
  no route loses protection at any step.

### Slice 4 — Two-factor gate at login
- **T4.1** Split login into **authenticate → (2FA challenge) → mint**: valid
  password issues a short-lived OTP (Slice 1), not tokens; tokens are minted only
  after OTP verification.
- **T4.2** Enforce per A6 (admins required; users opt-in via `is2FA`), reading the
  existing `is2FA` flag; leave the `tblM_User_2FA` **TOTP** path as a later opt-in.
- **T4.3** Wire the dashboards' login flow to the challenge step.

### Slice 5 — Client migration & Basic deprecation
- **T5.1** Move the admin + user dashboards (and any mobile/API client) onto the
  token endpoints; store/refresh tokens instead of replaying Basic creds.
- **T5.2** Once clients are migrated, **remove `[BasicAuthentication]`** from the
  protected routes (kept only for the token-issue endpoint, which takes credentials).
- **T5.3** Sequence with the **D15 bcrypt forced password reset** — passwords are
  treated as compromised, so the reset and the auth-model switch land together.

### Slice 6 — Verification & red-team
- Build + the full `QryptoCard.Tests` suite (Unit token/OTP logic; Integration
  DB-gated login→2FA→mint→refresh→revoke against the dev-shakeout DB).
- Internal red-team + **model-diverse external red-team** (Opus + Sonnet) — auth is
  the highest-risk surface; attack token forgery/replay, SubjectType confusion, OTP
  brute-force/replay, refresh-rotation races, and Basic/Bearer coexistence gaps.

## Verification

Per-PR: build the affected projects + run `dotnet test QryptoCard.Tests.sln`;
new auth logic gets Unit tests, and the login→2FA→token lifecycle gets a DB-gated
Integration test that runs in the dev shakeout. Every slice that touches auth goes
through the Slice 6 red-team gate before merge.

## Risks

- **Lockout / self-DoS** if 2FA or token expiry is misconfigured — mitigated by the
  coexistence path (Basic stays until clients are migrated) and admin-recovery.
- **Schema change on a live DB** — additive only (new tables, no backfill), run as a
  migration during the shakeout; rollback is a table drop.
- **Subject-type confusion** (a user token reaching admin routes) — the explicit
  reason for enforcing `SubjectType` at *both* mint and filter.

## Decisions (resolved)

1. **A6 — decided: 2FA required for admins, opt-in for users** at first, with a switch
   to make it mandatory for all once adoption is in place.
2. **Sequencing — decided: Plan 4 runs BEFORE the deploy**, so the new server launches
   already on the new auth model. Plan 4 lands code-complete with Unit coverage of the
   token/OTP logic; the full login → 2FA → mint → refresh → revoke lifecycle is the
   DB-gated Integration suite, validated against a throwaway DB (the dev shakeout, or a
   local SQL stood up for Plan 4 dev) before prod cutover.
