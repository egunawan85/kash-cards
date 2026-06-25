# Stage B — Dev Shakeout: Setup & Run Guide

Stand up the disposable dev environment, prove the app runs end to end, then tear it
down. This is the gated Azure run of the Stage A automation (merged in PR #14). Nothing
here touches production. Expect ~30–45 min the first time (most of it the VM bootstrap).

> Run everything from a shell where the Azure CLI is logged in. The repo's
> `deploy/provision-and-bootstrap.sh` orchestrates the whole thing; the steps below are
> the **operator setup** it depends on.

---

## 0. One-time prerequisites (your side)

### 0a. Azure permissions — the one that bites
Provisioning grants the VM's managed identity a **Key Vault Secrets User** role, which
requires **Owner** or **User Access Administrator** on the subscription (or at least the
`rg-kash-dev` resource group). **Contributor is NOT enough** — it can create resources
but cannot assign RBAC roles, and the provision step will fail at the role grant.

Verify (in the portal: Subscription qrypto → Access control (IAM) → "View my access"),
or grant yourself/owner-confirm before the run.

### 0b. Tools
The box you run from needs: `az` (logged in), `openssl`, `jq`, `base64`, `git`. The
current working box already has these. Confirm the CLI points at the right subscription:

```bash
az account set --subscription "Subscription qrypto"
az account show --query "{sub:name, id:id}" -o json   # id should be 7135879b-...-c35fe457
```

### 0c. Key Vault name must be globally unique
`KEYVAULT_NAME=kv-kash-dev` in `deploy/config/.env.provision.dev`. KV names are global.
Check availability and, if taken, append a short suffix (e.g. `kv-kash-dev-7a3`):

```bash
az keyvault list-deleted --query "[?name=='kv-kash-dev'].name" -o tsv   # also check soft-deleted
```

If you change it, change it only in `.env.provision.dev` — every script reads it from there.

---

## 1. Configuration — already prepared, just review

`deploy/config/.env.provision.dev` is **pre-filled** (gitignored). Review:
- `AZURE_SUBSCRIPTION_ID` = `7135879b-92ac-47ee-8e1e-bb6c3da63c00` (Subscription qrypto) ✓
- `AZURE_LOCATION=southeastasia`, `VM_SIZE=Standard_D2alds_v6` (matches legacy `genvm`) ✓
- `RDP_SOURCE=` blank + `RDP_DEFAULT_ACTION=Deny` — fine; the deploy uses `az vm
  run-command`, not RDP. Only set `RDP_SOURCE` to your IP if you want emergency RDP.
- `CLOUDFLARE_QUICK_TUNNEL=true` — see step 2.

`deploy/secrets/.vault` and `deploy/secrets/.env` are **already filled** (Postmark token,
WasabiCard sandbox keys, generated DB/app/internal secrets). Nothing to do unless you want
to rotate the dev values.

---

## 2. Cloudflare — named tunnel + named ROUTES

The shakeout ran in **named mode** (`CLOUDFLARE_QUICK_TUNNEL=false`) against the **`s16.xyz`**
zone — a full prod-shaped perimeter rehearsal. One-time setup:
1. Add the zone in Cloudflare; create an API token scoped to **Account → Cloudflare
   Tunnel:Edit** and **Zone → DNS:Edit**. (Zone hardening + WAF additionally need **Zone
   Settings:Edit + Zone WAF:Edit** — see the note below.)
2. Put the token + IDs in `deploy/config/.env.cloudflare.dev` (gitignored):
   `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `CLOUDFLARE_ZONE_ID`.
3. Set `CLOUDFLARE_QUICK_TUNNEL=false` and `CLOUDFLARE_ZONE=<zone>` in `.env.provision.dev`.

**Which sites get a public URL is an explicit `ROUTES` array** in `.env.cloudflare.<env>`
(the runegate-infra pattern — you *name* each hostname and map it to a loopback service;
hostname = `<prefix>.<zone>`). The dev set:

```
ROUTES=(
  "app-dev:http://127.0.0.1:8087"        # Dashboard         (customer/merchant UI)
  "admin-dev:http://127.0.0.1:8088"      # Dashboard.Admin   (admin UI, login-gated)
  "api-dev:http://127.0.0.1:8082"        # API.Public        (programmatic, API-key)
  "callback-dev:http://127.0.0.1:8084"   # API.Callback      (provider webhooks)
  "docs-dev:http://127.0.0.1:8086"       # APIDocs
)
```

Sites not listed get **no** public hostname (deploy-iis still builds all 12 loopback-only).
Rename a prefix to rename the URL, then re-run `cloudflare-setup.sh` (idempotent). See
[§6 Exposure model](#6-exposure-model-login--what-the-shakeout-proved) for why those 5.
A quick tunnel (`=true`) remains available for a zero-domain test of the tunnel mechanic.

> **Zone hardening + WAF are gated on the broader token.** With only Tunnel+DNS scope, the
> setup creates the tunnel/ingress/DNS (what makes the app reachable) and **skips**
> Always-HTTPS / min-TLS / HSTS / WAF with a clear note. Add Zone Settings:Edit +
> Zone WAF:Edit and re-run to apply them — required for the prod cutover.

---

## 3. Run it

```bash
cd /c/src/kash-cards
az account set --subscription "Subscription qrypto"
ENV=dev ./deploy/provision-and-bootstrap.sh --with-deploy
```

What happens, in order (all idempotent — safe to re-run):
1. **Provision** — `rg-kash-dev`, VNet/subnet, NSG (inbound 443 denied, RDP default-deny),
   public IP/NIC, the Windows VM (managed identity), Key Vault, Log Analytics; grants the
   VM identity KV read.
2. **Seed Key Vault** — uploads `.vault` + `.env` values (names `_`→`-`).
3. **Bootstrap VM** — IIS + ASP.NET 4.x + WCF activation + build tools + loopback SQL
   Express + the `kash_app` login. *(Longest phase, first run.)*
4. **Deploy** — fetch source, push the infra config, install sqlpackage, publish the 38
   tables, create the 12 IIS sites (4 INT tiers loopback-only), inject per-pool secrets,
   seed the minimal data + the smoke user, install cloudflared.
5. **Perimeter** — the tunnel (quick or named).
6. **Verify** — `vm-verify.ps1` prints `VERIFY_RESULT: PASS (n/n)`.

---

## 4. Validate

**Smoke test** — targets the programmatic API tier (`QryptoCard.API.Public`), API-key Basic
auth, read-only:
```bash
# The seeder writes smoke credentials here during the run:
cat deploy/secrets/.smoke.env          # SMOKE_API_KEY / SMOKE_API_SECRET (wire form)
SMOKE_BASE_URL=https://api-dev.s16.xyz dotnet test QryptoCard.Tests.Smoke
```
It exercises only the API-key tier (`/v1/card`, `/v1/master`, `/v1/transaction`); the
Bearer/dashboard backends are deliberately excluded. The mutating lifecycle tier runs only
with `SMOKE_ALLOW_MUTATION=true`.

**Admin login** — browse `https://admin-dev.s16.xyz` and sign in as the seeded admin
`edward@s16.ventures` / `KashAdmin!dev1`. An **OTP is emailed via Postmark** (confirm it
arrives); enter it to complete login. The dashboards reach their backends over loopback —
`KeyModel.API_URL` is config-driven (default `http://127.0.0.1:8081` / `:8083`).

**Expect:** `vm-verify` PASS; the 5 public URLs answer (dashboard/docs `200`, api/admin/
callback return their auth/signature challenges — not `5xx`); the internal surfaces
(`apiadmin`, `scheduler`, the INT tiers, Scrapper) have **no public hostname**.

---

## 6. Exposure model, login & what the shakeout proved

### Exposure model
12 sites are built; **5 are public**, 7 are internal (loopback-only — reachable only on the
box, or via the tunnel-fronted public tiers):

| Public (`*-dev.s16.xyz`) | Project | Why public |
|---|---|---|
| `app-dev` | Dashboard | customer/merchant UI |
| `admin-dev` | Dashboard.Admin | admin UI — login + OTP gated |
| `api-dev` | API.Public | the programmatic, API-key customer API |
| `callback-dev` | API.Callback | inbound provider webhooks (signature-verified) |
| `docs-dev` | APIDocs | API documentation |

**Internal (no public URL):** `QryptoCard.API` + `API.Admin` (the dashboards' Bearer-token
backends — called server-side over loopback, never from the browser), `API.Scheduler`
(internal job trigger), `INT` / `INT.Callback` / `INT.Scheduler` (the WCF money tier), and
`Scrapper` (a worker that web-scrapes US billing addresses for card issuance — outbound
only). This mirrors runegate/qrypto-omni: admin *API* and scheduler stay dark; the admin
*dashboard* is public but login-gated.

### Login & email
The seeder creates one admin (`edward@s16.ventures` / `KashAdmin!dev1`) in `tblM_Admin` plus
one smoke API user. Login is OTP-based: password → OTP emailed → verify → bearer token. The
**login/OTP email path is wired to Postmark** (MailKit → `smtp.postmarkapp.com:587`,
`POSTMARK_SERVER_TOKEN`, from `no-reply@kash.cards`). Known gaps (not on the login path):
user *forgot-password* email is commented out, and admin-invitation / callback-failure
emails still use a legacy Gmail path — migrate both to Postmark before prod.

### What the shakeout proved (and the fixes it took)
The full pipeline runs end-to-end on a fresh disposable VM. Non-obvious things that had to be
right, now baked into the scripts:
- **SQL 2025 Express** needs the static TCP port on **`IPAll`** (a per-IP loopback config
  yields error 26058 "no TCP listening ports"); SQL Browser enabled; clients connect by
  instance name, not `tcp:127.0.0.1,1433`.
- **On-box build:** restore the **whole solution** before building (a shared dependency
  declares its package in another project's `packages.config`); copy the published tree from
  `obj\Release\Package\PackageTmp` to the site root (these projects ignore `publishUrl`);
  **start the app pools** after publish (a stopped pool answers 503).
- **Tunnel origin must be `127.0.0.1`**, not `localhost` (Windows resolves `localhost` to
  `::1` first, but IIS binds IPv4 only → HTTP.sys 400 "Invalid Hostname").
- **`vm-install-cloudflared` must be passed `KvName`** — a missing mandatory parameter makes
  PowerShell prompt for input under `run-command` and hang forever.
- **Windows/git-bash:** `MSYS_NO_PATHCONV=1` for `az` resource-id args; CRLF stripping;
  cygpath for file / `@file` / native-`jq` path args.

### Deferred to the prod cutover
- Zone hardening + WAF (needs the broader CF token: Zone Settings + Zone WAF Edit).
- Callback-host **IP-allowlist** at the Cloudflare WAF (defense-in-depth; the HMAC webhook
  signature is the actual auth — see Plan 7 / security-findings).
- **Replace DevSeed** with a lighter seeding method (under review).
- Migrate the legacy-Gmail / commented-out email paths to Postmark.
- Real secret rotation + Runegate wiring (deposits) — currently fake addresses, dev token.

---

## 5. Tear down (when done)

```bash
az group delete --name rg-kash-dev --yes --no-wait
```

The single disposable RG holds everything, so one delete removes the whole dev env. Key
Vault has soft-delete; purge it too if you'll reuse the name:
`az keyvault purge --name kv-kash-dev`.

---

## Troubleshooting

- **`AuthorizationFailed` on the role grant** → you lack Owner/User Access Administrator
  (step 0a).
- **`VaultAlreadyExists` / name taken** → change `KEYVAULT_NAME` (step 0c).
- **A `run-command` phase hangs** → re-run the orchestrator; every phase is idempotent and
  resumes cleanly.
- **App pool faults on a missing secret** → re-run the deploy; `inject-secrets` recycles
  pools so workers pick up the env.
