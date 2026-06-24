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

Both callback paths are idempotent-by-state (they only act on rows in the expected pre-state),
and the PGCrypto path additionally rejects an already-applied `TransactionID`. That dedup check
is a best-effort SELECT, not race-proof. The durable form is a **UNIQUE index** on the dedup key
(`tblT_Card.PGCryptoID` / `tblT_Card_Deposit.PGCryptoID`, and a request/reference id for the
WasabiCard path) so concurrent duplicates cannot both commit.

- **To close:** add the unique constraints during the database migration; keep the in-code guard
  as the fast-path.

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

## 4. OTP brute-force lockout — *Open · Blocker: db*

Real OTP generation, bearer tokens, and 2FA shipped, but there is **no attempt-count lockout** on
OTP verification, so codes can be brute-forced at request rate. Closing it needs an `Attempts`
(and lock-until) column plus a verification handler that enforces a threshold and backoff.

- **To close:** add the attempts column + lockout logic; pair with the rate-limiting handler in
  item 5.

## 5. Rate limiting + trusted-proxy client IP — *Open · Blocker: code/cloud*

There is no rate-limiting ahead of the auth endpoints, and no resolver that trusts the real client
IP only from the perimeter proxy's CIDRs (so a client IP can currently be spoofed in any header).
Both are implementable in code but are paired with the perimeter rollout (the trusted CIDRs come
from the deployed proxy).

- **To close:** add a rate-limiting handler and a `CF-Connecting-IP`-style resolver gated on
  trusted proxy CIDRs.

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

## 9. Forensics / incident response — *Open · Blocker: db*

The forensic queries (forged-callback fingerprints, ledger-tamper detection, reconciliation of
credited value vs. confirmed deposits, profiling of the previously hardcoded preferential-fee
account) need to be run against the live database to determine whether funds were already taken,
and to trigger incident response if so.

- **To close:** run the forensic queries once database access exists; branch to IR on a positive.

---

## Carried notes (low severity)

- **Stale generated WCF client proxies.** Removing the unused server-side operations
  (`testEmail` / `testAPI` and the `SecurityService` crypto oracles) leaves the auto-generated
  client proxies (`Connected Services/*/Reference.cs`) still declaring those operations. They are
  never invoked, so this is harmless; regenerate the service references on the next provider-ref
  refresh to drop them.
