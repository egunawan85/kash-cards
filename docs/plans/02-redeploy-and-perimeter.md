# Plan 2 — Redeployment & Cloudflare Perimeter

## Objective

Stand up kash-cards on a clean new server we control, move the database, and put
Cloudflare in front — getting off the departing dev's (compromised) infrastructure
and adding perimeter defense **before** the deep code hardening of Plan 3. Mirror
the `runegate-infra` topology, which is built for exactly this stack and is fully
scripted.

This plan runs **after rotation (Plan 1)** and **before hardening (Plan 3)**.
Because the forge-a-deposit callback hole stays open until Plan 3, this plan
includes an **interim Cloudflare IP-lock** on the callback route so it isn't
reachable from the public internet in the meantime.

## Target topology (from `runegate-infra`)

- **Azure VM**, Windows Server 2022, **bare IIS** (no Docker), build-on-box.
- **Cloudflare Tunnel** (`cloudflared` Windows service, outbound-only). NSG denies
  all inbound 443 — no public origin IP; origin lock-down is structural.
- **Azure Key Vault + VM managed identity** for secrets; per-pool env injection.
- **SQL Server Express on the VM**, loopback-bound (127.0.0.1:1433), NSG-dark,
  least-privilege per-app login (decision D1).
- **Two resource groups:** `rg-kash-shared` (Key Vault + Log Analytics, permanent)
  and `rg-kash-prd` (disposable compute; redeploy = delete + re-provision).
- Naming `<kind>-kash-prd` (D4 = **production only**, no staging).

## Decisions

- **D1 DB host:** ✔ on-VM SQL Express. **D4 environments:** ✔ production-only
  steady-state, preceded by a disposable dev shakeout (see Execution model).
  **D11:** ✔ dev uses synthetic data + sandbox keys (real keys only with mutating-
  call guards). **D2 perimeter:** Cloudflare Tunnel. **D3 secrets:** Azure Key Vault.
- **D12 deploy/infra:** ✔ **in-repo `deploy/` folder, self-contained**; scripts
  adapted from `runegate-infra` (provenance-tagged) but provisioning **isolated**
  resources — kash-cards gets its **own** VM, Key Vault, Cloudflare config, and RGs,
  **not** co-tenant with sister apps (blast-radius isolation during the takeover).
  The `deploy/` folder + `.vault`/`.env` convention (**D13**) is created in Plan 1
  Slice 1; Plan 2 builds the Azure/Cloudflare provisioning on top of it.
- **D5 domain (❓ needed):** the zone + subdomain→site map for the 5 IIS sites
  (api / admin / callback / public / dashboard).
- **D7 canonical DB (❓ needed):** which of `qrypto-card`, `kashnow`,
  `qrypto-card-kashnow`, `qrypto-card-dev` is the live production DB.
- **D9:** decommission the old host + old Azure SQL after a successful cutover;
  treat both as compromised.

## Organized into slices

- **Slice 1 — Azure foundation & VM provisioning**
- **Slice 2 — App deployment automation**
- **Slice 3 — Database migration (BACPAC)**
- **Slice 4 — Cloudflare perimeter** (incl. interim callback IP-lock)
- **Slice 5 — Cutover & decommission**
- **Slice 6 — CI pipeline** (build + tests + secret guard on every push)

## Execution model — dev shakeout, then prod (D4 / D11)

The deploy scripts (`deploy-iis.ps1`, `inject-secrets.ps1`) are net-new for
kash-cards and the tunnel/Key-Vault/DB-move wiring is fiddly first-time, so the
pipeline is run **twice**:

1. **Dev shakeout (disposable):** provision a transient `rg-kash-dev` env and run
   **Slices 1–4** end-to-end against **synthetic data** and **dev/sandbox provider
   keys** (we already hold WasabiCard sandbox + dev PGCrypto keys). Validate the
   full deposit→callback→card flow, then **tear the dev box down** (`az group delete
   rg-kash-dev`). This is also where Plan 3's callback fix is developed against a
   running app.
   - **Key-safety fallback:** if functional dev/sandbox keys aren't available and we
     must use **real** provider keys on dev, guard against triggering real
     money/card operations — restrict to non-mutating/read flows, or knowingly
     accept and bound the risk. Synthetic data is used regardless.
2. **Prod:** provision `rg-kash-prd` and run the now-proven Slices 1–4, then the
   prod-only **Slice 5** (cutover + decommission). The prod DB move additionally
   runs dry-run-per-phase with the live DB untouched until the final swap.

Net steady-state remains production-only; the dev env is transient.

---

## Slice 1 — Azure foundation & VM provisioning

- **T1.1 — Resource groups + Key Vault.** Create the permanent shared RG (Key
  Vault in RBAC mode with soft-delete + purge-protection, Log Analytics) and the
  disposable `rg-kash-prd` compute RG. This is the two-RG model that lets us
  redeploy compute without re-seeding secrets.
- **T1.2 — VM + network lock-down.** Provision the Windows Server VM and its NSG
  with **inbound 443 denied** (the tunnel is outbound), RDP denied, and SSH
  hardened to pubkey-only. Reference `runegate-infra/scripts/provision/azure-vm-provision.sh`.
- **T1.3 — Server bootstrap.** Install the IIS + ASP.NET 4.x feature set, WCF HTTP
  activation, URL Rewrite, and the VS Build Tools to compile on-box; install SQL
  Server Express bound to loopback with a least-privilege `kash_app` login.
  Reference `runegate-infra/scripts/provision/vm-bootstrap.ps1`.
- **T1.4 — Seed Key Vault.** Upload the **rotated** secrets from Plan 1 into Key
  Vault via the `seed-kv-secrets.sh` pattern; the VM's managed identity is granted
  read-only access. No secret crosses the wire in plaintext.

## Slice 2 — App deployment automation

The app-side scripts the sisters have and kash-cards must add — the main authoring
work of this plan.

- **T2.1 — `deploy-iis.ps1`.** Create the 5 IIS sites/app-pools on localhost ports,
  pull the source, MSBuild-publish, and rewrite each `Web.config` connection string
  from env/Key Vault using a proper connection-string builder. One command brings
  the whole stack up.
- **T2.2 — `inject-secrets.ps1`.** Pull secrets from Key Vault via managed identity
  and write them as per-app-pool environment variables into `applicationHost.config`
  (ACL'd to Administrators/SYSTEM). This is how the running app gets its secrets
  without anything being stored in source.
- **T2.3 — Go-live verification script.** Assert the INT/WCF tier binds to
  loopback only (not reachable off-box) and that the callback site rejects an
  unsigned request. A scripted check so a misconfigured deploy is caught
  immediately.

## Slice 3 — Database migration (BACPAC)

Each phase is independently dry-runnable; the live DB is untouched until the final
swap. Reference `runegate-infra/import-database.sh` + `vm-*-db.ps1`.

- **T3.1 — Identify & export the canonical DB.** Resolve which of the stray DBs is
  actually serving production (D7), retire the rest, and export it to a `.bacpac`
  (doing any schema/data shaping off-box first). Getting this right is the
  foundation of the whole move.
- **T3.2 — Import side-by-side + smoke (dry run).** Copy the `.bacpac` to the VM
  and import it into a timestamped staging DB alongside the live one, then
  smoke-check a sentinel table and row counts. Nothing live is touched, so this can
  be rehearsed freely.
- **T3.3 — Backup + atomic swap.** Take a verified backup, then under a mutex stop
  IIS, rename the live DB aside, rename staging forward, repair contained users,
  and restart. The rename-aside gives sub-second rollback if anything looks wrong.

## Slice 4 — Cloudflare perimeter

Driven by the idempotent `cloudflare-setup.sh` pattern.

- **T4.1 — Tunnel + connector.** Create the `kash-prd` tunnel and install
  `cloudflared` as a pinned Windows service; its token lives in Key Vault and is
  pulled via managed identity. This is the outbound connection that replaces any
  open inbound port.
- **T4.2 — Routes + DNS.** Map each public hostname to its localhost IIS port and
  create the proxied CNAMEs to the tunnel, with a catch-all 404. This is where the
  D5 subdomain→site decisions get applied.
- **T4.3 — Zone hardening.** Turn on Always-HTTPS, min TLS 1.2 + TLS 1.3, SSL Full
  (strict), and HSTS — with Browser Integrity Check left **off** for the callback
  host (it breaks provider webhook callers). Declarative zone settings reconciled
  by the setup script.
- **T4.4 — WAF rules.** Enable bot protection with carve-outs for the API/callback
  hosts, block common scanner paths (`.env`/`.git`/`.php`...), block `/v1/admin/*`
  at the edge, and add a per-host lockdown that only allows the callback path on
  the callback host. Edge defense before requests ever reach the origin.
- **T4.5 — Interim callback IP-lock + re-register webhooks.** Restrict the callback
  route to the known WasabiCard/Runegate source IPs at the edge — this is the
  stop-gap that neutralizes the forge-deposit hole from the public internet until
  Plan 3 adds real signature verification. Then point both providers at the new
  callback URLs and confirm their webhooks arrive.

## Slice 5 — Cutover & decommission

- **T5.1 — Production validation.** Run the full flow on the new box: crypto
  deposit → callback → card funded, admin login, perimeter checks (direct-to-origin
  fails, only Cloudflare reaches the app). Done on prod since there's no staging,
  using the dry-run DB phases first.
- **T5.2 — Cutover.** Schedule the switch, flip DNS/tunnel to the new server, and
  monitor deposits/callbacks closely through the transition. Coordinate timing with
  the webhook re-registration so no deposit is missed.
- **T5.3 — Decommission.** After a retention window, tear down the old host and old
  Azure SQL, and confirm the old credentials are fully dead. Closes out the
  compromised infrastructure for good.

---

## Slice 6 — CI pipeline

The xUnit test projects (`QryptoCard.Tests` — Unit/Integration/Fixtures, PR #6) and the
`deploy/scripts/check-no-secrets.sh` guard already exist; this slice runs them automatically.

- **T6.1 — Build + test on push.** GitHub Actions on a Windows runner: restore + build
  `QryptoCard.sln` (MSBuild) and run `dotnet test QryptoCard.Tests.sln`; fail on any test
  failure. The DB-gated Integration tests stay skipped in CI (no `KC_TEST_DB`) and run in
  the dev shakeout.
- **T6.2 — Secret + hygiene guard.** Run `check-no-secrets.sh` plus a secret-scanner (e.g.
  gitleaks) on every push/PR and block on a hit — completing the Plan 1·S4 repo hygiene
  (grep-guard + scanning) that was deferred pending rotation.
- **T6.3 — Branch protection.** Require the build/test/guard checks before a PR can merge to
  `main`, matching the sister "clean CI before launch" go-live gate.

---

## Verification

- NSG shows no inbound 443; `cloudflared` healthy; each route resolves only through
  the tunnel; direct-to-origin attempts fail.
- Callback route reachable only from provider IPs (interim lock) and only via
  Cloudflare.
- DB swap smoke test green; rollback rehearsed.
- Full deposit/card flow passes on the new box before cutover.

## Risks

- **Webhook re-registration:** providers must be pointed at the new callback URLs;
  coordinate timing with cutover to avoid missed deposits.
- **DB shaping:** the BACPAC swap assumes schema parity; transforms happen off-box.
- **Build-on-box drift:** pin Build Tools / targeting-pack versions.
- **No staging:** validation happens on prod — mitigated by dry-run-per-phase DB
  moves and rename-aside rollback.
- Legacy ASP.NET Framework requires Windows/IIS — no Linux/container path.
