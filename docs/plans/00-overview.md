# kash-cards — Takeover Hardening & Redeployment: Overview

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
| D7 | Canonical prod DB | **Consolidate to ONE.** Spike confirmed all DBs share one schema (no drift) → feasible. Likely-live = `qrypto/qrypto-card-kashnow`. **Risk found:** API reads `qrypto-card` while money-callback writes `qrypto-card-kashnow` (possible split). | — | ❓ **pending live-DB check** (`tmp/db-consolidation-checks.sql`) to pick canonical + detect data split |
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
