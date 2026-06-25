# QryptoCard.Tests.Smoke

Over-the-wire E2E smoke suite that runs against the **deployed** endpoints (not in-process),
mirroring the sister `PGCrypto.Tests.Smoke`. It authenticates as a real API consumer using the
HTTP Basic credential the seeder produces, so it exercises the full public → INT → DB path.

## How it's driven

`QryptoCard.DevSeed` seeds a user + API key and emits `deploy/secrets/.smoke.env`:

```
SMOKE_BASE_URL=https://public-dev.kash.cards   # the deployed public API base
SMOKE_API_KEY=smoke-...                          # tblM_User_API.APIKey
SMOKE_API_SECRET=...                             # the EncryptAPP wire form (Basic password)
SMOKE_ADMIN_EMAIL=edward@s16.ventures
SMOKE_ADMIN_PASSWORD=...                          # EncryptAPP wire form
```

Load those into the environment, then `dotnet test QryptoCard.Tests.Smoke`. When the env is not
set, every test early-returns (pass-skip), so the project is harmless in CI without a target.

## Tiers (and why prod is different)

- **T0 — env gate.** Skip the suite cleanly when unconfigured.
- **T1 — auth.** No-credentials → 401, valid key not rejected. Read-only; **safe in all
  environments** (these are the always-on prod checks too).
- **T2 — read coverage.** Authenticated reads answer with the expected shape. Read-only; **safe
  everywhere.**
- **T3 — mutating lifecycle.** create card → deposit → signed callback → funded. **Gated by
  `SMOKE_ALLOW_MUTATION=true`; dev/sandbox only**, with a reaper to clean artifacts.

**Production does not run T3 as the dev seed+reap flow.** A standing test API user, free
mutation, and a delete-capable reaper are all unacceptable against live money. In prod the
money-path check becomes a **bounded canary pilot**: one opt-in, smallest-possible real
round-trip against a monitored account that is excluded from financial reconciliation, run only
at release/cutover gates. T0–T2 carry over unchanged; T3 is replaced by the pilot.
