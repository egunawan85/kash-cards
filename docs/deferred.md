# Deferred work

Open, not-yet-done work that isn't yet captured by code, tests, or the
deployment runbook. Security- and UX/operational-flavoured items live in the
two registers below; this file holds the remaining engineering and
launch/operational tasks. Remove an item when it ships.

Related living registers:

- [`security-findings.md`](security-findings.md) — security issues (open / mitigated / blocker), with closure criteria.
- [`non_security_findings.md`](non_security_findings.md) — UX / functional / operational issues.

The deploy architecture and redeploy mechanics are documented in
[`../deploy/README.md`](../deploy/README.md).

---

## Secret hygiene & key management

- **Drop redundant WasabiCard key encodings.** Keep only
  `WASABICARD_PRIVATE_KEY_XML`; remove the three unused encodings.
- **Disable the dev admin row.** `syapril@qrypto.trade` is seeded insert-only
  with no code path to disable it — disable it at the DB.
- **Cross-project secret name-guard test.** A grep-pin build test that fails if
  an INT-only secret name leaks into a public-facing project.
- **Pre-commit secret scanning.** Wire a pre-commit hook + gitleaks + GitHub
  secret scanning. Today there's only `.gitignore` plus the standalone
  `deploy/scripts/check-no-secrets.sh` guard.
- **Git history scrub.** Squash or `git filter-repo` to remove historically
  committed secrets. Cosmetic; do it only *after* provider credential rotation
  (security register) completes, so scrubbing doesn't race live credentials.

## Database move & swap

The schema-only DACPAC baseline + migrations model is in place (see deploy
README). The data-bearing production move is not yet scripted:

- **Canonical live export.** Script the production BACPAC export (data-bearing,
  not just schema).
- **Side-by-side import + dry run.** Import the BACPAC to a staging DB alongside
  live, smoke-test row counts, and rehearse rollback.
- **Atomic swap.** Back up live, rename live aside, rename staging forward,
  repair contained users, restart IIS.

## Provider integration

- **Wire up Runegate crypto deposits.** Real address provisioning is currently
  stubbed (`QRYPTO_ENVIRONMENT=dev`). Enable real provisioning and turn on
  outbound webhook signing on the Runegate side. (The verification cross-check
  for this path is tracked as a security-register item.)

## Card catalog & pricing

The Buy-a-Card catalog is sourced live from WasabiCard (online card types only) with
a global price/fee overlay; the cardholder is auto-generated for KYC cards. Accepted
follow-ups from the external red-team (low severity, no money impact):

- **Cardholder created before the wallet debit.** `openCard` registers a WasabiCard
  holder before `CardSpendService` debits, so a doomed buy (insufficient balance, or
  two concurrent first-time buys of the same KYC card) can leave an orphan/duplicate
  holder. No double-charge — idempotency is in `CardSpendService` and the
  `pass_audit`-gated reuse absorbs duplicates on the next attempt. Close it with a
  unique filtered index on `tblM_Cardholder(UserID, CardTypeId) WHERE isActive=1`
  plus moving the deposit-validity check ahead of holder creation.
- **Admin pricing role gate is a deny-list on the dashboard.** The INT
  `isDeniedFinanceMutation` (allow-list: Owner/Admin) is authoritative; the dashboard
  `btnUpdatePricing_Click` uses a deny-list matching the rest of that page. Align the
  dashboard to the allow-list for defence-in-depth.
- **Synthetic cardholder identity.** Auto-generated name/DOB/address are sent to
  WasabiCard for KYC cards — consistent with prior behaviour, but revisit if the
  issuer ever enforces real identity verification.

## Safe error logging

- **Auth service exception logging.** The auth service logs the full exception,
  which can leak secrets (the callback path already logs type-only). Switch to
  type-only plus a SHA-256 body-hash prefix so forged requests can still be
  correlated without storing sensitive bodies.

## CI/CD

- **GitHub Actions workflows.** Build + test on push; run
  `check-no-secrets.sh` + a secret scanner; enable branch protection on `main`.
  None exist yet (`.github/workflows/` is absent).

## Launch / operational sequence

These are gated operator actions, in order:

1. **Dev shakeout.** Provision a throwaway dev environment, run the deploy
   automation end to end, prove the deposit → callback → card flow plus OTP
   email and perimeter checks, then tear it down.
2. **Production cutover.** Execute the DB move (above), run the one-time crypto
   migration (security register), rotate provider credentials (security
   register), populate the callback IP allow-list, re-register webhooks, and
   monitor.
3. **Decommission.** After the cutover is confirmed stable, tear down the old
   host and old Azure SQL, and confirm the old credentials are fully dead.
