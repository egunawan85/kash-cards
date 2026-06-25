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

## 2. Cloudflare — pick one

**Option A — Quick tunnel (default; recommended for dev).** No Cloudflare account or
domain needed. `CLOUDFLARE_QUICK_TUNNEL=true` exposes the dashboard on a throwaway
`*.trycloudflare.com` URL printed during the run; the other public sites stay loopback.
This proves the tunnel mechanic without touching `kash.cards`. **No setup required.**

**Option B — Real `kash.cards` zone (do this for prod anyway).** Per-site dev subdomains
(`api-dev.kash.cards`, `dashboard-dev.kash.cards`, …). Requires, one-time:
1. Add `kash.cards` as a zone in your Cloudflare account.
2. At your domain registrar, change the nameservers to the two Cloudflare assigns.
3. Create a Cloudflare API token (Zone:DNS:Edit + Account:Cloudflare Tunnel:Edit) and a
   scoped account; put them in `deploy/config/.env.cloudflare.dev` (gitignored):
   `CLOUDFLARE_API_TOKEN=...`, `CLOUDFLARE_ACCOUNT_ID=...`, `CLOUDFLARE_ZONE_ID=...`.
4. Set `CLOUDFLARE_QUICK_TUNNEL=false` in `.env.provision.dev`.
5. Confirm the **cloudflared version pin** in `vm-install-cloudflared.ps1` is current.
6. Fill the **callback edge IP-lock allowlist** (`CALLBACK_ALLOW_IPS` in
   `cloudflare-setup.sh`) with WasabiCard / Runegate source CIDRs, or leave empty to skip
   the IP-lock (the rule is skipped, not applied, when empty).

> Recommendation: **Option A for the dev shakeout** (fastest, zero domain risk); set up
> the real zone (Option B) when you do the production cutover.

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

```bash
# The seeder wrote smoke credentials here during the run:
cat deploy/secrets/.smoke.env          # contains SMOKE_API_KEY / SMOKE_API_SECRET
# set SMOKE_BASE_URL to the tunnel URL the run printed, then:
dotnet test QryptoCard.Tests.Smoke
```

Expect: `vm-verify` PASS (NSG deny, INT tiers loopback-only, callback rejects unsigned,
public sites up), and the smoke suite green (auth + read tiers; the mutating lifecycle
tier runs only with `SMOKE_ALLOW_MUTATION=true`).

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
