# Execution Runbook — Dev Shakeout → Prod Cutover

## Why this doc exists

Plans 01–04 are **design** documents (what to change and why). This is the **operational
plan of record** for taking the now code-complete hardening live: the concrete order of
operations, what is locked, and what is still pending a decision or an access grant.

The code work is finished — every code-only item is closed (callback signature
verification, internal money-tier auth, the full IDOR + admin-role remediation, real
OTP + bearer tokens + 2FA, removal of the unauthenticated write endpoint and the dead
crypto/debug operations, the TLS-bypass removal, and the callback replay-guard /
cross-check / atomic-claim defense-in-depth). What remains is **operational** and needs
live database, provider, and cloud access. Those open items are tracked in
[`../security-findings.md`](../security-findings.md); this doc is how we execute them.

## Approach: prove it on a disposable box first, then cut over

Rather than leading with provider key rotation, we **stand up the app on infrastructure
we control first** — a disposable dev box with minimal seeded tables and dev
environment variables — and prove the full flow runs. Only once that is proven do we
redeploy to production with the real database migration and the real (rotated) secrets.

This means:

- **Provider rotation folds into the production cutover**, not the old (compromised)
  box. The compiled-in secrets (WasabiCard keys, the email sender) only break the
  running site when swapped, so they swap at cutover with the new server. The leaked
  values are neutralized at the providers as part of that coordinated step. (The DB
  login is the one credential that can be neutralized sooner by firewall-locking the
  server to the app host, without breaking the live app.)
- **Forensics is independent and read-only.** The forensic queries against the live
  database (to determine whether funds were already taken) can run the moment database
  access is authenticated; they do not gate, and are not gated by, the deploy work.

## Decisions locked for this execution

- **Runegate / PGCrypto wiring is deferred.** The dev and first prod passes keep
  outbound provisioning stubbed (`QRYPTO_ENVIRONMENT=dev` → fake deposit addresses).
  No real address provisioning and no inbound webhook signing for now; the callback
  signature verification and replay guard already shipped stay in place for the day it
  is wired up.
- **The dev shakeout is a full-topology rehearsal.** The disposable dev box provisions
  the entire production topology — Azure VM + on-box SQL Server Express + Azure Key
  Vault + Cloudflare Tunnel — not a stripped-down subset, so the prod run executes
  already-proven automation end to end. The box is torn down after validation.
- **Dev email uses a real Postmark token.** The dev box sends real email through
  Postmark (verified sender on `kash.cards`), so OTP / password-reset flows are
  exercised for real rather than stubbed.
- **Dev data is synthetic.** Minimal seeded tables only — no production data is copied
  to the dev box.

## Decisions still pending

- **Decommissioning the old host + old database** after a confirmed-stable cutover.

## Decisions resolved during execution

- **Schema source = Option A (read-only, schema-only export).** A schema-only DACPAC was
  extracted from the live canonical database (no data) and is the source of truth for
  the dev seed and the prod migration reference.
- **Canonical production database = `kashnow`** on `gendb.database.windows.net`. The only
  two kash-cards databases that exist are `gendb/kashnow` (live: 4,356 users, callback
  traffic ongoing) and `gendb-dev/qrypto-card` (near-empty dev/test remnant: 2 users).
  The committed catalog names `qrypto-card-kashnow` / `qrypto-card-dev` do not exist as
  databases — production runs an overridden config, so the committed connection strings
  are not reliable; the live data is decisive. Identical schema across both (38 tables),
  no money-split → a single-database move, not a keyed merge.
- **Azure context:** the live database is under the **Qrypto** subscription; the
  disposable dev box deploys to **Subscription qrypto**; region **Southeast Asia
  (Singapore-adjacent)**.
- **Dev email = real Postmark token.** **Dev shakeout = full-topology rehearsal.**
  **Runegate wiring deferred** (fake addresses).
- **WasabiCard sandbox credentials validated.** A read-only `account/info` call against the
  sandbox merchant API succeeds (`success:true`, a funded WALLET account), confirming the API
  key + RSA-SHA256 signing scheme work end to end. The dev box exercises the real (sandbox)
  card-issue leg — no stub needed.

## Staged execution

### Stage A — Author (no cloud spend, in a worktree) — ✅ DONE

**Merged to `main` via PR #14.** The deployment automation, the `QryptoCard.DevSeed` seeder,
and the `QryptoCard.Tests.Smoke` harness are in `deploy/` + the solution. Built (12 projects),
tested (Unit 74 / Integration 27 +4 skip / Smoke 5), seeder validated against the real schema in
SQL LocalDB, and taken through internal + model-diverse external red-team (Opus + Sonnet) with all
findings fixed. **Next live step is Stage B (gated).**

Scope as authored:

- Provisioning script (resource groups, VM + locked-down NSG, Key Vault, on-box SQL
  Express, server bootstrap).
- `deploy-iis.ps1` (create the IIS sites/app-pools, build-on-box, rewrite connection
  strings from environment) and `inject-secrets.ps1` (Key Vault → per-app-pool env).
- Dev schema + seed SQL (per the pending schema-source decision) and the token tables
  (`deploy/sql/create-token-tables.sql`).
- Dev `deploy/.env` + `deploy/.vault` with dev/sandbox values and the real dev Postmark
  token.
- Build `QryptoCard.sln`; run the test suites; report pass/fail/skip.

### Stage B — Dev shakeout (gated; provisions Azure) — NEXT

Run the Stage A automation against a disposable `rg-kash-dev`, prove the flow, tear down:
`ENV=dev ./deploy/provision-and-bootstrap.sh --with-deploy`.

**Operator prerequisites before the run:**
- Fill `deploy/config/.env.provision.dev` (subscription id + admin source IP) and the remaining
  `deploy/secrets/.vault` blanks.
- **Cloudflare:** set up the `kash.cards` zone, or run with the wired quick-tunnel fallback
  (`CLOUDFLARE_QUICK_TUNNEL=true`) for dev.
- Confirm the **cloudflared version pin** and fill the **callback edge IP-lock allowlist**
  (WasabiCard / Runegate source CIDRs) before relying on the edge lock.
- On-box: confirm the `applicationHost.config` ACL (Administrators+SYSTEM) doesn't block worker
  startup.

Steps:
- Provision the full topology; seed the minimal tables; deploy all sites.
- Prove the path end to end: deposit (fake address) → callback (signed, INT shared
  secret) → card funded; admin login (bearer); OTP email delivered via Postmark.
- Perimeter checks: direct-to-origin fails, only the Cloudflare tunnel reaches the app;
  the INT/WCF tier binds to loopback only; the callback site rejects an unsigned request.
- Tear the dev box down once validated.

### Stage C — Production cutover (gated)

Reuse the proven automation for production.

- Real database move (BACPAC: import side-by-side → smoke → backup → atomic
  rename-aside swap, live DB untouched until the final swap).
- Launch data migration: passwords + API secrets → bcrypt with a forced password reset
  for all users; 2FA secrets re-encrypted under a fresh AES-256-GCM key; the UNIQUE
  index on the callback dedup key that makes the replay guard durable.
- Provider rotation at cutover: swap in the rotated WasabiCard / email credentials,
  re-register webhooks, confirm the old credentials are rejected.
- Interim Cloudflare IP-lock on the callback route until Runegate signing is wired.
- Cutover, monitor, then decommission the old host + old database after a retention
  window.

## What "minimum required seed" means

Enough schema and data for the app to start and to demonstrate the money/auth/card flow:

- **Schema:** the money/auth/card tables the app reads (users, balances, cards,
  deposits, balance history, admins/roles, partner-webhook log, fee/commission/price
  config) plus the bearer-token tables.
- **Seed (synthetic):** one admin (`edward@s16.ventures` as the first admin row), one or
  two synthetic users with balance rows, and the fee / commission / price configuration
  rows the app reads at startup. Token and 2FA tables start empty.
