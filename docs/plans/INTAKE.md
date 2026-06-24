# Human-in-the-Loop Intake — everything I need to run Plans 1–3

Goal: front-load every human input so automation runs as long as possible without
stopping. Fill section 1 (access) + section 2 (decisions) once; section 3 are
third-party actions to start early (lead time); section 4 is the small set of
irreversible gates we keep.

---

## 0. Autonomy model — what runs without you vs. with you

| Band | Work | Needs |
|---|---|---|
| **A — autonomous NOW, zero access** | All Plan 1 code (deploy/ scaffold, `SecretsConfig`, env-loading), all Plan 3 code (callback verifier, OTP, IDOR, role checks, AES-GCM, cleanup), authoring all Plan 2 deploy scripts; build + unit tests + internal red-team; open PRs (never merge) | nothing — start immediately in worktrees |
| **B — autonomous WITH one-time access** | Provision Azure (VM/KV/network), dev shakeout, Cloudflare perimeter, SQL rotation, forensics/consolidation queries, the crypto/data migration on a copy | the grants in §1 |
| **C — hard human gates (kept minimal)** | Production cutover/DNS flip, forced password reset on real users, prod DB swap, old-server decommission, git history force-push, rotating the LIVE WasabiCard key | a final go at the moment (§4) |
| **D — only you can do (third party)** | WasabiCard portal/support actions; dev offboarding; telling providers the new callback URLs | §3 |

The point: **Band A starts today with nothing from you.** §1–§3 unblock Band B/D in
parallel. Band C is a short, enumerated list of final yeses.

---

## 1. Access to grant me (so I can act, not just instruct)

Scope each as tightly as possible (noted). If you'd rather run a step yourself, I'll
hand you exact commands instead.

- [ ] **Azure** — a **service principal** (tenant/client id/secret) scoped to a
  dedicated subscription or RG, with: Contributor (provision VM/KV/network), Key Vault
  admin, and SQL admin (reset logins). *Tight scope: one subscription or the two
  `rg-kash-*` groups, not tenant-wide.*
- [ ] **Cloudflare** — add `kash.cards` to Cloudflare, then a **scoped API token**
  (Zone:Read/Edit, DNS:Edit, Cloudflare Tunnel:Edit) for that zone only.
- [ ] **Database** — a **SQL login** with read (forensics/consolidation) and, later,
  write (migration) on the live DB(s); *or* you run my SQL scripts and paste results.
- [ ] **Postmark** — the server API token for `kash.cards` → into `deploy/.vault`.
- [ ] **Runegate** (your gateway) — access (repo/admin/DB) to issue the kash-cards
  merchant key, set its `CallbackSigningKey`, and enable outbound webhook signing; *or*
  provide those values.
- [ ] **Live OLD server** — RDP/SSH, *or* export the real deployed `Web.config` files,
  so I can confirm which DB/env is actually live (committed configs disagree).
- [x] **GitHub** — already have it (for CI + secret scanning).

## 2. Decisions to lock now (one pass)

- [ ] **Subdomain → site map** for `kash.cards` (5 sites): e.g. `api.`, `admin.`,
  `callback.`, `public.`/partner, `app.`/dashboard — your preferred names.
- [ ] **Azure region + VM size** (default: match sister — `Standard_D4s_v5`, region
  closest to users). Confirm or override.
- [ ] **Canonical DB** — either you run `tmp/db-consolidation-checks.sql` and tell me,
  or grant DB access (§1) and I determine + propose it.
- [ ] **Dev-shakeout keys** — do working **WasabiCard sandbox** creds exist? (If yes,
  no real-money risk on dev. If no, I guard against mutating calls.)
- [ ] **Forced password reset** — confirm in-scope, and the **email copy/sender** for
  the reset + the re-enabled OTP mails (default sender `no-reply@kash.cards`).
- [ ] **Cutover window** — a target maintenance window for the prod switch.

## 3. Third-party actions to start early (lead time — only you can)

- [ ] **WasabiCard support:** (a) reissue the production **API key**; (b) register our
  **new public key** (I'll generate the keypair); (c) send their **current public key**
  (for callback verification — the committed one is stale); (d) send the **merchant
  audit log** (forensics).
- [ ] Confirm the **Runegate merchant credential isn't shared** with qrypto-omni.
- [ ] **Dev access offboarding** (GitHub/Azure/server/portals) — you own this track.
- [ ] At cutover: give **WasabiCard + Runegate the new callback URLs**.

## 4. Pre-authorizations & the gates we keep

**Blanket go-ahead I'm asking for (reversible / build work):** create worktrees, write
code, build, run tests, run internal + external red-team, author deploy scripts, open
PRs. (Never merge; never push to main.)

**Irreversible / money- or user-facing — I will pause for one explicit "go" each (or you
pre-authorize specific ones now):**
- [ ] Production **cutover / DNS flip**
- [ ] **Forced password reset** on real users
- [ ] **Prod DB swap** (the migration's final step)
- [ ] **Decommission** old server + old DB
- [ ] **git history** force-push (scrub)
- [ ] Rotating the **LIVE WasabiCard key** (coordinated with their pubkey registration)

I recommend keeping these six as checkpoints even under "full automation" — they're
irreversible and touch money/users — but I'll bundle them into as few moments as
possible (ideally one "go for cutover").

## 5. How we capture secrets

I'll generate `deploy/.env.example` + `deploy/.vault.example` listing every name to
fill. You populate `deploy/.vault` (gitignored) once; I reference names, never values.
