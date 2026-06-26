# Non-security findings — open register

UX / functional / operational issues found while running the deployed stack that are **not**
security findings (security issues live in [`security-findings.md`](security-findings.md)).
Same format as the security register.

## Status legend
- **Open** — confirmed, not yet fixed.
- **Deferred** — acknowledged, parked for later by owner decision.
- **Fixed** — resolved (links the commit / PR).

---

## NS-1. Admin login page — cannot type into the input fields — *Deferred*

**Symptom.** On the admin dashboard login page (`https://admin-dev.s16.xyz/login`) the email
and password inputs cannot be typed into. The customer **registration** page works fine
(typing + OTP validated); the customer *login* page is unconfirmed.

**Ruled out (server-side) — the page is sound from the server's side:**
- All assets load (`HTTP 200`): `Content/plugins/global/plugins.bundle.{css,js}`,
  `Content/js/scripts.bundle.js`, `Content/css/style.bundle.css`.
- The inputs are plain `<input>` (`txtEmail` / `txtPassword`) — not `disabled`/`readonly`.
- The admin login page is **structurally identical** to the customer login page (same Web
  Forms template, same inputs, same JS bundles — only the logo and a "Sign up" link differ).
- The only inline scripts are harmless: anti-clickjacking frame-busting, a Bootstrap
  theme-mode setter, and a `hostUrl` variable. None of them touch the inputs.

**Likely cause (client-side, unconfirmed).** A runtime JS error in `scripts.bundle.js` /
`plugins.bundle.js` (they load but may throw), a Metronic overlay element covering the form,
a cached broken page state, or a browser extension. This needs browser-console (F12)
diagnosis, which can't be done from the server side.

**Next steps when picked up:**
1. Capture DevTools → Console errors on the login page (a thrown error there is the smoking gun).
2. Hard-refresh (Ctrl+Shift+R) / incognito / a different browser to rule out cache + extensions.
3. Confirm whether the customer login (`app-dev/login`) is *also* affected — that scopes it to
   the shared login template vs. an admin-only problem.

**Found:** testing the dev deployment (admin login), 2026-06-25.

---

## NS-2. OTP login verify collapses ~6 failure modes into one message — *Open (fix recommended)*

**Symptom.** `AuthV1Service.mintAfterOtpVerify` returns the identical `"Invalid OTP code or
session"` for at least six distinct conditions: empty input, wrong code, expired code, unknown
session, user-not-found, **and any internal exception** (e.g. a config / SQL error) caught by the
method's outer `catch`. The generic message is partly deliberate — the catch hides `ex.Message`
so a `SqlException` can't leak schema to the wire — but it also disguises **internal errors as
user errors**.

**Impact.** A deployment/config bug (a missing `AuthDbEntities` connection string — see PR #27)
presented to the operator *identically* to a user fat-fingering their OTP. Finding the real cause
required live instrumentation of the running WCF service, because the message actively
misattributed an infrastructure failure to a bad code.

**Recommended fix.** Keep the catch from surfacing `ex.Message`, but give the **internal-exception**
path a DISTINCT, non-leaky status (e.g. `Status="error"`, `Message="Authentication temporarily
unavailable"`) separate from the genuine bad-OTP returns — so config/infra failures are visible
instead of disguised. Optionally split expired vs incorrect vs unknown-session for the user-facing
paths (low sensitivity, since the caller already holds a valid session id). Touches money-tier auth
code, so it warrants its own red-team.

**Found:** diagnosing the OTP-login failure (PR #27), 2026-06-26.

---

## NS-3. Deploy is all-or-nothing (no app-only / per-tier) + leaves pools Stopped — *To do (approved, deferred to a later session)*

**What.** kash has exactly one deploy mode — the full `provision-and-bootstrap.sh --with-deploy`
pipeline (~30-45 min, rebuilds all 12 sites). There is no app-only redeploy and no per-tier
build/restart. Worse, the pipeline **leaves several app pools Stopped** at the end: after the
merged-main redeploy, `kash-int`, `kash-dashboard`, `kash-int-scheduler`, `kash-scrapper`, and
`kash-api-callback` were all Stopped and had to be started by hand (they started cleanly — no
faults, just never started by the deploy). A deploy that ends with the WCF money tier down is a
correctness gap.

**Why it matters.** Every code-only change forces the whole 30-45 min pipeline; much of the
multi-hour OTP debug was hand-rolling per-tier rebuilds the tooling should provide.

**Fix — model on the sibling apps.** runegate (PGCrypto) and qrypto-omni already ship this:
their `deploy-iis.ps1` is a multi-command tool (`setup`/`deploy`/`update`/`build [svc]`/`restart
[svc]`/`status`/`logs`). kash is the outlier (bundled into one orchestrator). Port that surface
as a laptop-side `deploy/deploy.sh <cmd> [svc]` (the box is NSG-dark, so it wraps `az
run-command`):
- `update` — app-only: fetch main -> clean-build all -> inject-secrets -> recycle (~5-10 min)
- `build [svc]` / `restart [svc]` — per-tier (`int`, `int-callback`, `api`, `dashboard`, ... aliases from sites.json)
- `status` / `logs [svc]`
- **a `start` step that guarantees every pool ends Started** (closes the gap above)
- **Open decision:** should `update` also re-publish schema, or stay strictly app-only?

**Also fold in:** strengthen the `.vault.example` comment so `SCHEDULER_SHARED_SECRET` is flagged
as a HARD startup dependency (API.Callback faults at startup if it's unset).

**Raised:** 2026-06-26, after the merged-main redeploy validated OTP login. Owner approved; not
done in that session.
