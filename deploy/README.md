# deploy/

Self-contained deployment for kash-cards. Provisioning scripts are adapted from the
sister `runegate-infra` repo (provenance is tagged in each script header), but
kash-cards provisions **isolated** resources — its own Azure VM, Key Vault, Cloudflare
config, and resource groups — not co-tenant with the sister apps.

## Layout

```
deploy/
  secrets/            # operator-authored, GITIGNORED (templates committed as *.example)
    .env.example      #   non-secret config (DB host/name/user, URLs, env)
    .vault.example    #   secrets (DB password, provider keys) — NEVER committed
  scripts/
    load-env.ps1      # local dev: load .env + .vault into the current process env
    provision/        # (Plan 2) Azure VM + KV + network
    deploy/           # (Plan 2) deploy-iis.ps1, inject-secrets.ps1
    perimeter/        # (Plan 2) Cloudflare tunnel + WAF
    secrets/          # (Plan 2) seed-kv-secrets.ps1
```

## How secrets reach the app

The app reads secrets from **process environment variables only** (see
`QryptoCard.Sec/SecretsConfig.cs`). It never reads files or Key Vault directly.

- **Local / dev:** `deploy/scripts/load-env.ps1` loads `secrets/.env` + `secrets/.vault`
  into the current process before you run the app.
- **Server:** `inject-secrets.ps1` pulls from Azure Key Vault (via the VM managed
  identity) and writes per-app-pool env vars into `applicationHost.config`.

So the same code works everywhere; only the source of the env vars differs.
