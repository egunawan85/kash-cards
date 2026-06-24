# deploy/scripts/

| Path | Purpose | Status |
|---|---|---|
| `load-env.ps1` | Local dev: load `secrets/.env` + `.vault` into the current process env | ready |
| `provision/` | Azure VM + Key Vault + network (NSG default-deny, no inbound 443) | Plan 2 |
| `deploy/` | `deploy-iis.ps1` (build + IIS sites) + `inject-secrets.ps1` (KV → app-pool env) | Plan 2 |
| `perimeter/` | Cloudflare tunnel install + `cloudflare-setup` (routes, WAF, callback IP-lock) | Plan 2 |
| `secrets/` | `seed-kv-secrets.ps1` (upload `.vault`/`.env` → Key Vault) | Plan 2 |

Scripts under `provision/`, `deploy/`, `perimeter/`, `secrets/` are adapted from the
sister `runegate-infra` repo and will be authored in Plan 2. Each will carry a header
noting the `runegate-infra` file it was derived from, so security fixes can be re-synced.
