# Plan 4 — Authentication Tokens & Two-Factor (design proposal)

> **Status: DESIGN PROPOSAL — not started, awaiting sign-off.** This broke out of
> Plan 3 Slice 3 (the auth/authz slice): the IDOR fixes, fee-backdoor removal, and
> admin role guards shipped in PR #4/#5, but the *authentication model* itself —
> real OTP, two-factor, and replacing per-request Basic credentials with revocable
> bearer tokens — is a distinct architectural change and is planned here.
>
> Per our workflow, the **Design decisions** below are a hard gate: nothing is
> implemented until you sign them off.

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
