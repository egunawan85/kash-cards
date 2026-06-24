# Plan 3 — Security Hardening & Forensics

## Objective

Determine whether funds were **already** stolen, then close the money-theft and
auth-bypass surfaces in the code. Fixes mirror the post-breach hardening already
done in the sister projects (`runegate`, `qrypto-omni`), which share kash-cards'
exact stack and anti-patterns.

This is the last plan in the sequence (rotation → deployment → hardening). The code
work is done **in a worktree behind a gated PR**, with internal + external red-team
before merge. **Forensics (Slice 1) is front-loaded** — it should be run as early
as Plan 1 (the moment there is DB access), since it is time-sensitive and
independent of the code changes.

## Organized into slices

- **Slice 1 — Forensics & incident response** (run first / early)
- **Slice 2 — Callback integrity** (the critical money fix)
- **Slice 3 — Authentication & authorization**
- **Slice 4 — Backdoor & money-path cleanup**
- **Slice 5 — Crypto migration & defense-in-depth**
- **Slice 6 — Verification & red-team**

---

## Slice 1 — Forensics & incident response

Queries are in [`../../tmp/forensics.sql`](../../tmp/forensics.sql) (read-only),
run against the live production DB (D7).

- **T1.1 — Run the forensic queries.** Profile the hardcoded `59da7833…` account,
  find deposits/cards marked paid with no on-chain `Txhash`/`PGCryptoID`
  (forged-callback fingerprint), detect ledger-tamper rows
  (`BalancePrevious + Amount ≠ Balance`), and reconcile credited value against
  confirmed on-chain deposits. This tells us *if* and *how much* was taken.
- **T1.2 — Correlate external evidence.** Cross-check findings against the
  WasabiCard merchant audit log (requested in Plan 1) and the IIS/Cloudflare access
  logs for callback source IPs (the webhook table has no IP column). Confirms
  whether suspicious credits came from forged calls vs. legitimate provider traffic.
- **T1.3 — Incident response (conditional).** If theft is confirmed: freeze the
  suspect account, notify WasabiCard, quantify user impact, preserve evidence, and
  decide on user notification. Runs in parallel with the code fixes; scope depends
  on what Slice 1 finds.

## Slice 2 — Callback integrity (the critical money fix)

Closes the forge-a-deposit hole (`C1`). Today the callback controller has no auth,
reads `X-WSB-SIGNATURE` but never verifies it, takes a raw body, disables TLS
validation, and swallows all exceptions.

- **T2.1 — Verify-first pipeline + raw-body capture.** Restructure the callback to
  read the **raw body bytes** and verify **before** any parse or DB write: invalid
  signature → 401 and no DB touch; bad shape → 400; our error → 500 (so the provider
  retries). Remove the empty `catch` and `trustConnection()`. **Gotcha (from spike):**
  the current callback receives a parsed SOAP/`[FromBody]` object, which does **not**
  expose the raw bytes HMAC must run over — so this task includes changing the
  callback to ingest the raw request body first.
- **T2.2 — WasabiCard RSA verification.** Verify the `X-WSB-SIGNATURE` header by RSA
  over the raw body. **Note:** the committed `WASABICARD_WSBPUBLIC_KEY` is a dead/likely-
  stale constant — get WasabiCard's **current** public key, don't trust the repo copy.
  Reject weak keys (modulus <2048 / tiny exponent). Port `runegate/.../FireblocksSignatureVerifier.cs`.
- **T2.3 — Runegate HMAC verification (exact scheme from Runegate source).** Header
  **`X-Runegate-Signature`**, value `t=<unix>,v1=<hex(HMAC-SHA256(key, "<ts>.<raw-body>"))>`;
  the key is the **per-merchant `CallbackSigningKey`** (not the API secret); enforce a
  **~5-minute freshness window** against `t`; constant-time compare; reject weak secret.
  Since Runegate is your own gateway, this pairs with **turning on outbound signing
  there** (T2.5). Port `runegate/.../QryptoOmniSignatureVerifier.cs` style.
- **T2.4 — Post-verify REST cross-check.** Before crediting, independently re-fetch
  the deposit/payment status from the provider's REST API and require it to match
  the webhook's claimed status/amount/txhash. This is the strongest control — it
  holds even if a webhook is unsigned. Port `qrypto-omni/.../WebhookCrossCheckEvaluator.cs`.
- **T2.5 — Actually wire up Runegate (currently stubbed).** Outbound provisioning is
  switched off today (`QRYPTO_ENVIRONMENT="dev"` → fake addresses), so real crypto
  deposits don't flow. Enable real address provisioning, set a per-merchant
  `CallbackSigningKey`, and **turn on outbound webhook signing** on the Runegate side
  (it's your gateway) so T2.3's verifier has a signature to check. Without this the
  cross-check (T2.4) is the only line of defense.

## Slice 3 — Authentication & authorization

> **Status:** T3.2 (IDOR) and T3.3 (admin roles) **shipped** — PR #4 and PR #5. T3.1
> (real OTP + enforced 2FA + email delivery) and the bearer-token model are broken out
> to **[Plan 4](04-auth-tokens-2fa.md)**. T3.4 (open write endpoint) still ⬜.

- **T3.1 — Real OTP + enforced 2FA + email delivery.** *(→ [Plan 4](04-auth-tokens-2fa.md).)* Replace the hardcoded
  `getOTPCode()` → `"000000"` with real generation, and **enforce verified-OTP/session
  state on every protected call**, not just at login; add login lockout. **Note (from
  spike):** the user-facing OTP/reset emails are currently **commented out**, so OTP
  can't actually reach users — this task includes **re-enabling them via Postmark**
  (D14). Reference `runegate/.../TotpService.cs` + `LoginLockout.cs`.
- **T3.2 — Fix the IDORs.** ✅ **(PR #4.)** Every by-ID balance/card/transaction read and write must
  filter by the authenticated user's ID (currently computed but unused), so one user
  can't read or touch another's data via guessable sequential IDs.
- **T3.3 — Admin role enforcement.** ✅ **(PR #5.)** Add real role checks to the admin
  fee/commission/price/invite endpoints, and fix `addAdmin` repurposing the password
  field as the role string. Reference the sister bearer-token tier separation.
- **T3.4 — Secure the open write endpoint.** Add authentication (or remove) the
  unauthenticated `AutomationV1Controller` address-insert endpoint, plus input
  validation.

## Slice 4 — Backdoor & money-path cleanup

- **T4.1 — Remove the hardcoded fee GUID.** Strip the `59da7833…` preferential-fee
  special-case from all 4 code sites and replace with a normal data-driven fee tier.
- **T4.2 — Gate commission crediting.** The `ManualService` methods that credit
  platform balances have no auth and keyed off the (previously forgeable) success
  status — put them behind auth and the now-verified callback.
- **T4.3 — Remove TLS-validation bypass.** Delete `trustConnection()` everywhere and
  pin `ServicePointManager.SecurityProtocol = Tls12` at startup, so outbound calls to
  providers can't be silently MITM'd.
- **T4.4 — Remove debug/leftover code.** Delete the test/debug endpoints
  (`testEmail`, `testAPI`, `generateAPI`, `signRSA`/`decryptRSA`/`getwb`), the
  `akbarmc:akbarmc` commented credential, and fix the inverted `isBanned` guards.

## Slice 5 — Crypto migration & defense-in-depth

- **T5.1 — Crypto migration (the bundled key event, D16).** This replaces Plan 1's
  old "rotate DBKey/APPKey" — it's a one-time data migration on the canonical DB,
  run at launch, mirroring the sister patterns:
  - **Passwords (user + admin):** stop reversibly encrypting; switch to **bcrypt**
    one-way hashing, and **force a password reset for all users** (D15) — the leaked
    `DBKey` means every stored password was decryptable. Reference `qrypto-omni`/`runegate`
    `PasswordHasher.VerifyWithUniformTiming`. With the forced reset, old password values
    are simply invalidated — no need to decrypt-and-migrate them.
  - **API secret keys (`tblM_User_API.SecretKey`):** bcrypt-hash (verify, don't decrypt).
  - **2FA secrets (`tblM_User_2FA.*`, must stay reversible):** re-encrypt under a **new
    key using AES-256-GCM** (random IV, auth tag) — replacing the broken `Secure.cs`
    Rijndael/IV==key construction (`qrypto-omni/.../AesUtility.cs`).
  - Net: the old `DBKey`/`APPKey` are **retired**, not rotated; only the small 2FA set
    rides a new key.
- **T5.2 — Rate limiting + trusted-proxy IP.** Add a rate-limiting handler ahead of
  auth, and a resolver that trusts Cloudflare's `CF-Connecting-IP` only from trusted
  proxy CIDRs (so the real client IP can't be spoofed). Reference
  `qrypto-omni/.../{RateLimitingHandler,ClientIpResolver}.cs`.
- **T5.3 — Safe error logging.** On auth/webhook paths, log the exception type name
  (never the message) and a SHA-256 body-hash prefix (never the body), so secrets and
  PII don't leak into traces while forgery campaigns stay correlatable.

## Slice 6 — Verification & red-team

> **Status:** ✅ ongoing — every money/auth PR (#2–#5) went through build + tests +
> model-diverse external red-team, which caught and fixed a real critical and a medium
> before merge. The ad-hoc PowerShell checks are now **formal xUnit projects**
> (Unit/Integration/Fixtures, PR #6): `dotnet test QryptoCard.Tests.sln`.

- **T6.1 — Build + test.** Build the solution and test projects, run the existing
  suite (report pass/fail/skip), and author new tests for the settled fixes:
  signature verifiers (tampered body/sig, malformed input, null key → reject), OTP
  enforcement, IDOR ownership filters, role checks. Verifiers are pure functions →
  unit-testable with `(sig, body, secret)` triples.
- **T6.2 — Internal red-team.** Adversarially attack the new callback verifier and
  auth inside the worktree — forge attempts, downgrade attempts, replay — and fix
  what's found before opening the PR.
- **T6.3 — External red-team → PR.** Run a model-diverse automated review (Opus +
  Sonnet), post each verdict verbatim on the PR, fix/re-verify, then present for your
  review. No merge without explicit go-ahead.

---

## Decisions

- **D10:** the forensics outcome (Slice 1) determines whether the incident-response
  branch (T1.3) opens and how wide.
- **PGCrypto webhook secret:** T2.3 needs a shared `PGCRYPTO_WEBHOOK_SECRET` from
  Runegate. If they don't sign outbound to us today, the post-verify cross-check
  (T2.4) is the primary control until they do.

## Risk note

This touches money, auth, and crypto — the highest-risk surface in the codebase.
Hence the full Slice 6 gate (internal + external red-team) before any merge, and the
worktree isolation throughout.
