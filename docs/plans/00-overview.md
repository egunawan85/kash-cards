# kash-cards — Takeover Hardening & Redeployment: Overview

## Status checklist (as of 2026-06-26)

Master roll-up; each plan doc has its own detailed checklist. `[x]` = code substantially done · `[ ]` = material gaps remain · ⏳ = on-box/launch. **The "Shipped #1–#13" narrative below is accurate but frozen at PR #13** — the repo has since merged Plan 7 prepaid-balance (#16/#19), Postmark (#21), OTP-lockout/rate-limit (#25), and the reconciliation sweep (#26/#29/#31).

**By plan:**
- [x] **Plan 1 — Secret rotation:** code-side done (env/Key Vault, fail-fast accessor, hardcoded removed); ⏳ the actual provider rotations + history scrub remain → see `01`
- [x] **Plan 4 — Auth tokens & 2FA:** built & merged (#7–#11); residual deviations (2FA mandatory-vs-opt-in) + purge worker → see `04`
- [ ] **Plan 3 — Security hardening:** webhook integrity + IDOR/role + dead-code closed, **but T5.1 crypto migration (bcrypt/AES-GCM) is still unwritten**; forensics tails owner-gated → see `03`
- [ ] **Plan 2 — Redeploy & perimeter:** Stage A automation merged (#14); WAF build-out, BACPAC data-move, and CI pipeline still open; ⏳ prod cutover → see `02`
- [x] **Plan 7 — Prepaid balance:** core + reconciliation sweep shipped (#16/#19/#26/#29/#31); shakeout items remain → see `07`

**Stale "open" notes that are actually DONE (verified in code; corrected in the per-plan docs):**
- [x] OTP-lockout + rate-limiter + IP-resolver (was P3·S5 "top residual")
- [x] `InsertAddress` / open write endpoint removed (P3·S3/S4, commit `b28a2c9`)
- [x] `SecurityService` / `trustConnection` / dead-op cleanup (P3·S4)
- [x] Postmark migration incl. forgot-password + admin-invite (06 "known gaps")
- [x] DevSeed → SQL-seed replacement

**Genuinely still OPEN in-repo:**
- [ ] **CI pipeline** (Plan 2 S6) — no `.github/workflows/` exists despite the test projects landing
- [ ] **T5.1 crypto migration** — bcrypt/AES-GCM code not written (launch-gated)

**On-box / launch gates (⏳):** prod DB move + swap · provider rotations · git-history scrub · the launch crypto/bcrypt **data** migration · WAF/zone hardening · decommission old host.

## Context

kash-cards (a.k.a. QryptoCard) is a crypto→prepaid-card platform: users deposit
stablecoins to a per-user address via the **PGCrypto / Runegate** gateway
(`api.runegate.co`); a webhook credits them and provisions/funds a real prepaid
card via the whitelabel issuer **WasabiCard**. Stack: legacy **ASP.NET Framework
4.x + WCF (INT tier) + EF6**, IIS-hosted. Data in **Azure SQL**. There are real
users with real balances.

The codebase was inherited from a departing developer. Three problems drive this
effort:

1. **Leaked secrets** — every key/secret was hardcoded and committed to a repo
   that was public on GitHub. All must be rotated (history scrub won't un-expose).
2. **Exploitable code / possible insider backdoor** — money-crediting callbacks
   have no signature verification, OTP is hardcoded to `000000`, several
   money/admin endpoints are unauthenticated or IDOR-prone, and a hardcoded user
   GUID gets preferential fees. Needs forensics + hardening.
3. **Redeployment** — move off the current server, move the database, and put
   Cloudflare perimeter defense in front.

This is split into three plans, executed against the sister-project patterns from
**Runegate**, **qrypto-omni**, and **runegate-infra** (same team, same stack,
already taken through post-breach hardening):

Numbered in **implementation order** — rotation → deployment → hardening:

- [`01-secret-rotation.md`](01-secret-rotation.md)
- [`02-redeploy-and-perimeter.md`](02-redeploy-and-perimeter.md)
- [`03-security-hardening.md`](03-security-hardening.md)
- [`04-auth-tokens-2fa.md`](04-auth-tokens-2fa.md) — bearer-token & email-OTP/2FA design (broke out of P3·S3; awaiting sign-off)
- [`05-execution-runbook.md`](05-execution-runbook.md) — **operational plan of record**: dev shakeout → prod cutover (how the code-complete hardening goes live)
- [`07-runegate-prepaid-balance.md`](07-runegate-prepaid-balance.md) — feature design: deposit to a Runegate address → reusable prepaid balance → spend on cards (money-path refactor + security review; 📝 draft, awaiting sign-off)

## Progress (live status)

Legend: ✅ done · 🟡 partial · ⬜ not started · ⏸ deferred (blocked on DB / provider / Azure access)

| Plan / Slice | Status | Where |
|---|---|---|
| **P1·S1** — Secret-management foundation (SecretsConfig, secrets out of source, deploy scaffold, guard, tests) | ✅ | **PR #1 (merged)** |
| **P1·S2** — Emergency provider rotation (Wasabi/Runegate/SQL keys, email→Postmark) | ⏸ | provider actions + at launch |
| **P1·S3** — Stored creds & internal keys (admin swap; crypto keys → P3·S5) | ⏸ | admin swap needs DB |
| **P1·S4** — Repo hygiene & history scrub | 🟡 | gitignore + secret guard done (PR #1); secret-scanning/pre-commit + history scrub pending rotation |
| **P2** — Redeploy & Cloudflare perimeter (VM/KV/tunnel, DB move, deploy scripts) | ⬜ | needs Azure/Cloudflare access; scripts not yet authored |
| **P2·S6 (added)** — CI pipeline (run tests + secret guard on every push) | 🟡 | xUnit test projects landed (**PR #6**); CI wiring → with deploy prep |
| **P3·S1** — Forensics & incident response | ⏸ | you run `tmp/forensics.sql` (needs DB) |
| **P3·S2** — Callback integrity (signature verifiers + verify-before-forward) | ✅ | **PR #2** — T2.4 cross-check & T2.5 Runegate wire-up deferred |
| **P3·(added)** — INT money-tier hardening (WCF shared-secret auth, metadata lockdown, error logging) | ✅ | **PR #3** — emerged from the PR #2 red-team |
| **P3·S3** — Auth/authz (real OTP+2FA, IDOR, admin roles, open write endpoint) | 🟡 | real OTP+bearer+2FA shipped → **Plan 4 ✅ (#7–#11)**; IDOR **comprehensively closed — 14 endpoints + admin role-gates (PR #12)**, superseding the partial #4; admin roles (#5). **⬜ T3.4: `AutomationV1Controller.InsertAddress` is still unauthenticated — the one forgotten code-doable item.** |
| **P3·S4** — Backdoor & money-path cleanup (fee GUID, commission gate, trustConnection, debug endpoints) | 🟡 | fee-GUID removed (#4); inverted-`isBanned` guards fixed (#7/#12). **⬜ remaining: `ManualService` (`cancelCard`/`generateAPI`) + `SecurityService` crypto-oracle (`decryptdb`/`getwb`/`signRSA`/`decryptRSA`) — audit-confirmed dangerous, currently protected ONLY by INT network isolation; `trustConnection()` TLS bypass (RT-flagged 3×, now carries bearer tokens); `testEmail`/`testAPI` debug endpoints.** |
| **P3·S5** — Crypto migration & defense-in-depth (AES-GCM, bcrypt, rate-limit, IP resolver) | 🟡 | error logging done (PR #3); AES-GCM/bcrypt migration ⏸ (DB, at launch); **rate-limit/IP-resolver ⬜ = the OTP brute-force lockout, flagged 4× by external RTs — top residual (needs an `Attempts` column + handler).** |
| **P3·S6** — Verification & red-team | ✅ | escalated to **full external red-team** — separate headless `claude` instances (Opus + Sonnet) in detached worktrees, verdicts posted verbatim — run across **every** PR (#2–#12), incl. post-merge assurance on the already-merged #2/#4. xUnit projects (#6). |
| **P4** — Auth tokens & 2FA (opaque bearer tokens, SubjectType, email-OTP → 2FA) | ✅ | **DONE — #7–#11 merged** (real OTP #7, token subsystem #8, bearer wiring #9, dashboard Basic→Bearer + Postmark #10, consolidated via #11). Runegate-wholesale auth subsystem, LocalDB integration-tested. Residual: OTP lockout (DB-gated). |

**Shipped:** PR #1–#12 — secrets foundation (#1), callback signature verification (#2), INT money-tier WCF auth (#3), IDOR + fee-backdoor + `enable2FA` (#4), admin role guards (#5), xUnit test projects (#6), then all of **Plan 4** — real OTP (#7), opaque bearer-token subsystem + LocalDB harness (#8), bearer wiring (#9), dashboard Basic→Bearer + Postmark (#10), consolidated into `main` (#11) — and a **comprehensive IDOR remediation (#12)** that closed a whole *class* of cross-user holes the original #4 missed (14 endpoints + admin role-gates), and a **security loose-ends close-out (#13)** that removed the unauthenticated write endpoint and the dead crypto/debug WCF operations, killed the accept-any-TLS-certificate bypass and pinned TLS 1.2, and added the callback replay-guard / provider cross-check / atomic-claim defense-in-depth. **#1–#13 all merged — every code-only item is now closed.** Every money/auth change went through a **full external red-team** (separate headless Opus + Sonnet instances); the reviews caught and we fixed before merge: a broken WCF role-guard (critical), a refresh/ban-bypass TOCTOU + a missing `AuthV1Service.svc` host (medium/runtime), old-OTP-on-regenerate, two inverted `isBanned` checks, a `login.aspx` revoke gap — and surfaced the entire #12 IDOR class on already-merged code.

**Code-doable loose ends — CLOSED by #13.** The items that were once the only things
executable without DB/provider/Azure access are now done: the unauthenticated
`AutomationV1Controller.InsertAddress` write endpoint (T3.4) and the dead
`ManualService` / `SecurityService` crypto-oracle + `testEmail`/`testAPI` operations
(T4.2/T4.4) were **removed**; the `trustConnection()` TLS-validation bypass (T4.3) was
**deleted** and TLS 1.2 pinned; the callback gained the post-verify cross-check (T2.4),
an atomic-claim guard, and a `TransactionID` replay guard. What remains is **operational
only** (rotation, forensics, redeploy, the launch crypto/bcrypt migration with the
durable UNIQUE index, OTP lockout, rate-limiter) — tracked in
[`../security-findings.md`](../security-findings.md) and executed per
[`05-execution-runbook.md`](05-execution-runbook.md).

**Why things are deferred:** anything needing **DB access** (forensics, the crypto/bcrypt data migration, replay idempotency with a unique index) or **provider/Azure access** (rotation, deploy) is staged but not executable in Band A (code-only) — those run with your access, mostly at launch.

## Sister-project patterns we inherit (the house style)

| Concern | House pattern (source) |
|---|---|
| Hosting | Azure VM, Windows Server 2022, **bare IIS** (no Docker), build-on-box (`runegate-infra`) |
| Perimeter | **Cloudflare Tunnel** (`cloudflared` Windows service, outbound-only); NSG denies all inbound 443 — no public origin IP (`runegate-infra/scripts/perimeter`) |
| Secrets | **Azure Key Vault + VM managed identity**; per-pool env vars injected into `applicationHost.config`; connection strings assembled on-box, never stored whole (`*/inject-secrets.ps1`, `SecretsConfig.cs`) |
| Secret accessor | `SecretsConfig.Require/Preload` — env-only, fail-fast at `Application_Start`, no fallback defaults |
| DB | On-VM **SQL Server Express**, loopback-bound, NSG-dark; least-privilege per-app login (`runegate-infra/vm-bootstrap.ps1`) |
| DB move | **BACPAC pipeline**: import side-by-side → smoke → backup → atomic swap with sub-second rollback (`runegate-infra/import-database.sh`, `vm-*-db.ps1`) |
| Webhook verify | Raw-body HMAC-SHA256 / RSA, **verify-before-parse, fail-closed, constant-time, 401-on-forgery** + **post-verify REST cross-check** (`runegate`/`qrypto-omni` `*SignatureVerifier.cs`, `CallbackV1Controller.cs`) |
| Crypto-at-rest | **AES-256-GCM** (BouncyCastle), random IV, auth tag (`qrypto-omni/AesUtility.cs`) — replaces broken Rijndael IV==key |
| Env separation | `ENVIRONMENT ∈ {prod, staging}` enforced at startup; dev branches deleted from source |
| Repo hygiene | grep-guard pre-commit hooks + secret scanning; `.env`/`.vault`/`*.pem`/`*.key` gitignored |

## Cross-cutting decisions (need your sign-off before we execute)

Recommendations are defaults chosen to match the sister projects. ✅ = I'll
proceed with the recommendation unless you change it; ❓ = I need your input.

| # | Decision | Recommendation | Alternative | Status |
|---|---|---|---|---|
| D1 | Database host | On-VM **SQL Server Express** (sister parity, loopback-locked, free) | Keep/move to managed **Azure SQL** (easier managed backups, but exposes a network surface) | ✔ **decided: on-VM SQL Express** |
| D2 | Perimeter mode | **Cloudflare Tunnel** (no public origin IP) | Proxied DNS + origin firewall to CF IPs | ✅ confirm |
| D3 | Secret store | **Azure Key Vault** + managed identity | Git-ignored `Web.config` transforms | ✅ confirm |
| D4 | Environments | **Production-only steady-state**, preceded by a **disposable dev shakeout** box to validate the pipeline (torn down after) | Permanent staging/dev env | ✔ **decided: prod-only + throwaway dev shakeout** |
| D11 | Dev shakeout data/keys | **Synthetic data**; dev/sandbox provider keys if available, else real keys with mutating-call guards | Restored prod data | ✔ **decided: synthetic data; sandbox keys preferred** |
| D5 | Primary domain | **`kash.cards`** | `kash.now` / `qrypto.trade` (both legacy) | ✔ **decided: `kash.cards`** (subdomain→site map still TBD) |
| D6 | Rotation timing | **Rotate provider creds now** (kills leaked values); externalize to KV during redeploy | Full externalize on the old server first | ✅ confirm |
| D7 | Canonical prod DB | **Consolidate to ONE.** | — | ✔ **RESOLVED by live data: canonical = `kashnow` on `gendb.database.windows.net`.** Only two kash-cards DBs actually exist (`gendb/kashnow`, `gendb-dev/qrypto-card`); the committed catalog names `qrypto-card-kashnow`/`qrypto-card-dev` are stale (no such DB). `kashnow` holds the live data (4,356 users, callback traffic today); `qrypto-card` is a near-empty dev/test remnant (2 users). **Identical schema (38 tables), no money-split → single-DB move, no keyed merge.** Committed connection strings are unreliable (prod runs an overridden config); the data is decisive. |
| D8 | Sequencing | **Rotation → Deployment → Hardening** (deploy adds an interim Cloudflare IP-lock on the callback route to cover the pre-hardening window) | Harden before deploy | ✔ **decided: rotate → deploy → harden** |
| D9 | Old server / DB | Treat as compromised; **decommission after cutover** | Keep as warm fallback (still-compromised) | ✅ confirm |
| D10 | Forensics → IR | If theft is confirmed, branch to incident response (freeze suspect acct, notify WasabiCard, assess user impact) | — | ❓ depends on forensic results |
| D12 | Deploy/infra location | **In-repo `deploy/` folder, self-contained**; provisioning scripts adapted from `runegate-infra`, provisioning **isolated** resources (own VM/KV/Cloudflare/RGs — NOT co-tenant with sister apps) | Reuse runegate-infra for shared provisioning; or separate kash-cards-infra repo | ✔ **decided: in-repo, isolated** |
| D13 | Secret home | Gitignored **`deploy/.vault`** (secrets) + **`.env`** (non-secret) → seeded to **Key Vault** for the server; app reads from env | Single root `.env` | ✔ **decided: `.vault`/`.env` in `deploy/`** |
| D14 | Email provider | **Postmark + `kash.cards`** (replaces the live Gmail/`qrypto.trade` sender; dead spacemail code deleted). No Postmark in repo today — build it. | Keep SMTP | ✔ **decided: Postmark** |
| D15 | Password storage | **bcrypt one-way hash** + **forced password reset** for all users (DBKey was public and passwords were reversibly encrypted → treat all as compromised). Mirror sister `PasswordHasher`. | Keep reversible encryption | ✔ **decided: bcrypt + forced reset** |
| D16 | Crypto migration | Rotating `DBKey`/`APPKey` is a **data migration** (5 live columns). Bundle it with the Plan 3 crypto upgrade, executed **at launch**: passwords→bcrypt, API secrets→bcrypt, 2FA secrets→AES-GCM under a new key. Old `DBKey`/`APPKey` mostly retired, not rotated. | Standalone key rotation now | ✔ **decided: bundle into Plan 3 at launch** |
| D17 | Admin account | Real admins are **DB rows** (`tblM_Admin`), not config. Seed **`edward@s16.ventures`** as first admin, **disable the dev's** `syapril@qrypto.trade`; delete the dead `qwerty`/`12345678` KeyModel defaults. | — | ✔ **decided** |

## Sequencing

```
Plan 1 — Secret rotation (NOW)                                          [you + me]
   (a) Foundation FIRST: create the in-repo deploy/ folder + gitignored
       .vault/.env + the SecretsConfig env-loader (gives rotated keys a home)
   (b) In parallel: provider revocation/reissue (kills leaked values)
   (c) Store the new values in deploy/.vault  +  run forensics
              │
              ▼
Plan 2 — Redeploy & perimeter                                           [me + you]
   FIRST validate the whole pipeline on a disposable DEV box (synthetic
   data, sandbox keys), then provision PROD with the proven scripts:
   VM + Key Vault + Cloudflare, move DB (BACPAC), cut over, decommission
   old.  INTERIM: lock the callback route to provider IPs at the
   Cloudflare edge (mitigates the forge-deposit hole until Plan 3)
              │
              ▼
Plan 3 — Security hardening                                             [me, gated PR]
   code fixes in a worktree (callback signature verify first), then
   build + test + internal & external red-team → PR for your review
```

**Sequencing rationale (D8 = rotation → deployment → hardening):** rotation is
most urgent (live leaked creds). Deploying onto controlled infrastructure with
Cloudflare next gets us off the departing dev's servers and adds perimeter defense
early. Hardening — the deep code surgery — comes last. The one gap this opens is
that the forge-a-deposit callback hole stays exploitable between deploy and
hardening; Plan 2 therefore includes an **interim edge mitigation** (Cloudflare
IP-lock on the callback route) so it isn't reachable from the public internet in
that window.

**No permanent staging env (D4):** steady-state is production-only, but a
**disposable dev shakeout box** runs first to prove the net-new deploy scripts, the
DB-move pipeline, and the tunnel/Key-Vault wiring against synthetic data — so the
prod run executes already-proven automation. The dev box is torn down after
validation. The prod DB move additionally runs **dry-run per phase** (import
side-by-side → smoke-check → backup) with the live DB untouched until the final
atomic swap (sub-second rename-aside rollback).

## Status

**These are drafts for alignment.** Nothing is executed until the decisions above
are signed off. Each plan has its own phased steps, sister-file references,
verification, and rollback.
