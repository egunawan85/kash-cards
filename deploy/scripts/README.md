# deploy/scripts/

Provisioning, deployment, and verification scripts for the VM / IIS / SQL /
Cloudflare stack. See [`../README.md`](../README.md) for how these fit together
and how to run a redeploy.

| Path | Purpose |
|---|---|
| `load-env.ps1` | Local dev: load `secrets/.env` + `.vault` into the current process env |
| `check-no-secrets.sh` | Guard that fails if secrets would be committed |
| `scheduler-trigger.ps1` | Invoke a scheduled job on demand |
| `provision/` | `azure-vm-provision.sh` (VM + network, NSG default-deny, no inbound 443) + `vm-bootstrap.ps1` (first-boot setup) |
| `deploy/` | Build + ship to IIS: `deploy-iis.ps1`, `inject-secrets.ps1` (KV → app-pool env), `vm-fetch-source.ps1`, `vm-iis-ops.ps1`, `vm-write-config.ps1`, `vm-sync-content.ps1`, plus DB ops (`vm-install-sqlpackage.ps1`, `vm-migrate.ps1`, `vm-seed.ps1`) |
| `dev-seed/` | `generate-dev-seed.ps1` — build the dev seed dataset |
| `perimeter/` | `vm-install-cloudflared.ps1` + `cloudflare-setup.sh` (tunnel, routes, WAF, callback IP-lock) |
| `secrets/` | `seed-kv-secrets.sh` (upload `.vault` / `.env` → Key Vault) |
| `verify/` | `vm-verify.ps1` + `vm-verify-walletpath.ps1` — post-deploy smoke checks |

The `provision/`, `deploy/`, `perimeter/`, and `secrets/` scripts are adapted
from the sister `runegate-infra` repo; each carries a header noting the
`runegate-infra` file it derived from, so security fixes can be re-synced.
