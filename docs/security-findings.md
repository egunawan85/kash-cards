# Security findings — open register

A living list of **known, still-open security issues** and the conscious mitigations
in place for each. It exists so nothing falls through the cracks between now and the
dedicated security pass ("opening motion") that will close these out together. Entries
are removed only when the underlying issue is actually fixed and verified — not when a
release ships.

Most open items are not code-fixable in isolation: they need **live database access**,
**provider cooperation** (WasabiCard / Runegate-PGCrypto), or **cloud/Azure access** to
complete. They are staged here so the work is ready the moment that access exists.

## Status legend

- **Mitigated** — a real control is in place; what remains is hardening/defense-in-depth.
- **Open** — no control yet; relies on an environmental assumption (e.g. network isolation).
- **Blocker** — what is required to close it: `code`, `db`, `provider`, or `cloud`.

---

## 1. Runegate / PGCrypto crypto-deposit cross-check — *Mitigated · Blocker: provider*

The crypto-deposit callback (`CallbackV1Service.PGCrypto`) credits a deposit and provisions a
card on the strength of the PGCrypto/Runegate webhook. Forgery and body-tampering are already
closed by the raw-body signature verification at the callback edge, and a replay guard now
rejects a webhook whose `TransactionID` was already applied. The remaining defense-in-depth
step — independently re-fetching the deposit's status from Runegate's REST API and requiring it
to match before crediting — is **not yet wired** because Runegate exposes no "get deposit /
transaction status" endpoint in this codebase and its REST contract is not documented here.

- **To close:** obtain Runegate's deposit-status REST endpoint (path + response shape), add a
  gateway method, and gate `PGCrypto` crediting through `WebhookCrossCheckEvaluator` the same way
  the WasabiCard path now is.
- **Until then:** signature verification + the `TransactionID` replay guard are the controls.

## 2. Durable webhook replay idempotency — *Mitigated · Blocker: db*

Both callback paths are idempotent-by-state and now claim each order with an **atomic conditional
update** (`UPDATE ... SET Status=... WHERE ID=... AND Status=<expected>`) before crediting, so a
concurrent or replayed duplicate finds zero rows affected and bails — no double-refund, no
double-provision. PGCrypto additionally requires a `TransactionID` and rejects one already
applied. What remains for the database migration is the **durable belt-and-suspenders**: a UNIQUE
index on the dedup key (`tblT_Card.PGCryptoID` / `tblT_Card_Deposit.PGCryptoID`, and a
request/reference id for the WasabiCard path) so duplicates cannot commit even across processes or
a context that bypasses the conditional claim.

- **To close:** add the unique constraints during the database migration; keep the in-code atomic
  claim as the fast-path.

## 3. INT-tier WCF surface relies on network isolation — *Open · Blocker: cloud/db*

The internal WCF tier (`QryptoCard.INT*`) carries the money and credential-validation operations.
Inbound calls to the money-callback tier are authenticated with a shared secret, but the broader
INT surface (`SecurityService.validate*`, card/balance operations in the App/Admin services) has
**no per-call caller identity** — it is protected by the assumption that the INT tier is not
reachable from the internet. The dead/dangerous operations that made this worse (the crypto
oracles, `ManualService`, the debug stubs) have been removed, which shrinks the blast radius, but
the dependency remains.

- **To close:** confirm at deploy time that the INT tier is bound to loopback / a private network
  with no public ingress (perimeter work), and treat any change that widens its exposure as a
  go/no-go gate. Consider extending the shared-secret behavior to the rest of the INT surface.

## 4. OTP brute-force lockout — *Mitigated · Blocker: code (hardening)*

Shipped (PR #25). Every OTP verify handler (login / register / email-change / key-OTP, user and
admin) now atomically increments an unmapped `FailureCount` on the OTP-session row and flips
`isVerify = -1` at 5 failures, after which the existing `isVerify == 0` lookup treats the row as
not-found — a brute-forcer is cut off after 5 guesses per session with no oracle. The atomic
`UPDATE … CASE` is race-safe; the columns are unmapped so no EDMX regen was needed (DDL
`create-otp-lockout-columns.sql`, applied before the code deploys).

Companion control — **password brute-force lockout** (also PR #25): 5 wrong passwords lock the
account for 15 minutes via unmapped `FailureCount` + `LockoutEnd` on `tblM_User` / `tblM_Admin`. A
locked account returns the same "password is incorrect" message as an ordinary wrong password, so
the lock is not an enumeration oracle; an expired window auto-resets to prevent a perpetual-lock
DoS; and the failure UPDATE is gated on not-currently-locked so a concurrent burst can't over-count
past the lock. These sit *in front of* the OTP factor (an attacker must clear the password gate to
mint an OTP session at all) and behind the rate limiting in item 5.

- **Remaining (hardening, defense-in-depth):** per-account OTP lockout — the current OTP lockout is
  per-session; a fresh random code per session plus the rate limit already make brute-force
  infeasible, but a per-account counter would harden it further. Soften lock-griefing (anyone who
  knows an email can lock that account for 15 min) by notifying the user on lockout and/or allowing
  an immediate unlock via a successful second factor.

## 5. Rate limiting + trusted-proxy client IP — *Mitigated · Blocker: code (hardening)*

Shipped (PR #25). A per-IP sliding-window rate limiter fronts every `AuthV1Controller` action on
the user and admin API tiers (login / verify / resend, register / verify / resend,
forgot-password, refresh, revoke), returning HTTP 429 + `Retry-After` on breach. The client IP is
resolved by a peer-gated resolver that trusts `CF-Connecting-IP` / `True-Client-IP` /
`X-Forwarded-For` only when the request peer is the cloudflared loopback (including the IPv4-mapped
`::ffff:127.0.0.1` form), so a direct-ingress client cannot spoof its bucket key. Limits are
environment-tunable.

- **Remaining (hardening):** add a `maxTrackedKeys` cap with fail-closed eviction to the in-process
  bucket store (today unbounded — fine at single-instance scale, but a cap guards against a
  direct-ingress IP-spray memory DoS), and emit a metric/alert on a high `"unknown"`-IP rate (a
  broad peer-resolution failure would collapse every such request into one bucket and throttle the
  whole tier). Swap to a shared store (Redis) if the API tiers ever scale horizontally.

## 6. Crypto-at-rest migration (passwords, API secrets, 2FA) — *Open · Blocker: db*

Stored passwords and API secrets use reversible encryption under a key that was exposed; 2FA
secrets use a weak (IV == key) construction. The fix is a one-time data migration at launch:
passwords and API secrets → one-way **bcrypt** (with a forced password reset for all users, since
the old values must be treated as compromised), and 2FA secrets → **AES-256-GCM** under a fresh
key. The old database/app keys are then retired rather than rotated.

- **To close:** run the migration against the canonical database at launch; reset credentials.

## 7. Secret rotation — *Open · Blocker: provider/cloud*

Every key/secret that was historically committed must be rotated at the providers (WasabiCard,
Runegate, SQL, email) and moved into the secret store. Source no longer contains secret material,
but the leaked values are only neutralized once rotated.

- **To close:** rotate at each provider; seed the new values into the deployment secret store.

## 8. Redeploy onto controlled infrastructure + perimeter — *Open · Blocker: cloud*

The application still needs to move off the inherited servers onto controlled infrastructure with
a Cloudflare-style perimeter (no public origin IP), an interim edge IP-lock on the callback route,
and a loopback-bound database. Until then the origin is directly reachable.

- **To close:** provision the new environment and cut over; decommission the old servers.

## 9. Forensics / incident response — *In progress · Blocker: db (analysis) + owner decision (D10)*

The read-only forensic queries were run against the live canonical database (`kashnow` on
`gendb`). Preliminary results:

- **No mass forged-callback theft.** Credited value ($95,874.70) matches confirmed on-chain
  value ($95,824.70) within **$50** — a single, oldest-record deposit (`QRYCRDPST…0001`,
  2025-02-13) funded without a `Txhash`/`PGCryptoID`. Deposit statuses: 54 success / 11 expired
  / 3 paid.
- **Open question (DEFERRED) — duplicate `Txhash` reuse.** Five groups of "success" deposits
  share the same `Txhash` value (uses of 8/5/4/3/2; **~$44k of credited value** sits on reused
  values). The stored values are explorer **URLs** (`https://tronscan.org/…`), not raw on-chain
  hashes, so this may be a benign artifact of how the field is populated **or** genuine
  double-credits. **Deferred by owner decision** — revisit before final cutover. To resolve when
  picked up: pull per-deposit detail for the 5 groups (users, amounts, dates, deposit addresses,
  whether each credit maps to a distinct on-chain transaction); branch to incident response (D10)
  only if double-crediting is confirmed.
- **Suspect fee-GUID account `59da7833…` is a real live account** (1 user row, 9 deposits, 2
  cards), keyed on the `UserID` nvarchar column (not `ID`, which is a bigint surrogate). The
  preferential-fee code is already removed (PR #4); its historical fee advantage is unquantified.
- **Ledger table is buggy/unused, not tampered.** `tblH_User_Balance` has 13 rows, all "Initial
  Deposit", with a `Balance` that does not equal `BalancePrevious + Amount` (inconsistent
  scaling) — a pre-existing promo-path bug, not out-of-band edits.
- **`kashnow` has zero admin rows** (`tblM_Admin` empty) — relevant to seeding the first real
  admin (D17) at migration.

- **To close:** get the owner's read on the duplicate-`Txhash` groups (benign vs. double-credit);
  branch to incident response (D10) only if double-crediting is confirmed; quantify the suspect
  account's historical fee advantage as a follow-up.

**Note:** the forensic script `tmp/forensics.sql` filters the suspect on `tblM_User.ID`, but in
this schema the GUID lives in `UserID` (`ID` is a bigint) — use `UserID` when re-running.

## 10. Auth response enumeration oracle — *Open · Blocker: code*

Login and forgot-password return **distinct messages** for a non-existent email (`"Your email is
not registered"`) versus a valid email with a wrong password, so an unauthenticated caller can
enumerate which emails are registered (and, on login, active/banned status). This is **pre-existing**
and was deliberately left unchanged by the lockout work (item 4): the password lockout uses the same
message for locked and wrong-password so it adds no *new* oracle, but the underlying email oracle
remains. Closing it is a user-facing contract change (the front-end currently shows the specific
messages), which is why it was deferred rather than bundled.

- **To close:** return a uniform "invalid email or password" (and a generic "if that email is
  registered, you'll receive a reset link" for forgot-password) for every failure branch, with
  matched timing; coordinate the front-end copy change.

## 11. 2FA enrolled but never verified — *Open · Blocker: code*

`tblM_User_2FA` stores an (encrypted) TOTP secret and `enable2FA` / `get2FA` let a user enroll, but
there is **no `verify2FA` endpoint and no TOTP check on login** — the authenticator code is never
actually validated. A user who turns on 2FA gets no second-factor protection, while believing they
do (a false sense of security). Surfaced by the verification-surface review during the brute-force
work.

- **To close:** add TOTP verification (validate the time-based code against the stored secret) and
  enforce it as a step in the login flow for users with active 2FA; rate-limit / lockout that step
  like the OTP factor in item 4.

---

## Carried notes (low severity)

- **Refund path dereferences the user balance row without a null check.** In the WasabiCard
  deposit-fail refund (`CallbackV1Service.Wasabi`), the user's balance row is read with
  `FirstOrDefault()` and then dereferenced; a user with no balance row throws and the deposit is
  left `Failed` with no refund credited. This is **pre-existing** (the same finalize-before-credit
  ordering existed before the cross-check work and was confirmed out-of-scope by both red-team
  passes), but it is a real availability gap on a money path. To close: null-guard the balance row
  and bail before claiming the deposit, or wrap the claim + credit in a single transaction.
- **`updatePassword` current-password check is not lockout-guarded.** The authenticated
  change-password path verifies the current password but is not covered by the login password
  lockout (item 4). Brute-force value is low (the caller is already an authenticated session for
  that account), so it was left out of scope; guard it if/when current-password confirmation becomes
  a higher-value target.
- **Stale generated WCF client proxies.** Removing the unused server-side operations
  (`testEmail` / `testAPI` and the `SecurityService` crypto oracles) leaves the auto-generated
  client proxies (`Connected Services/*/Reference.cs`) still declaring those operations. They are
  never invoked, so this is harmless; regenerate the service references on the next provider-ref
  refresh to drop them.
