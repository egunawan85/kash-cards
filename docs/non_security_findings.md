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
