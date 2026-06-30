# Legacy KashNow → kash-cards import cutover runbook

How to migrate the live legacy **KashNow** database into a kash-cards environment:
import the legacy data, bring it to the current schema with the ordered migrations,
force the credential reset, and verify integrity. Planning context and the schema
analysis live in `docs/plans/legacy-data-migration.md`.

## What this is

A **cross-engine** migration carried by a `.bacpac`:

| | Legacy KashNow (source) | kash-cards (target) |
|---|---|---|
| Engine | Azure SQL Database (`gendb.database.windows.net`, DB `kashnow`) | SQL Server Express on the VM (`localhost\SQLEXPRESS`) |
| Subscription | `Qrypto` | "Subscription qrypto" |

Strategy **B (import-then-migrate)**: the legacy schema *is* our dacpac baseline (verified
1:1, 38 tables), so `vm-migrate.ps1` takes its **ADOPT** path — it sees `dbo.tblM_User`,
skips the dacpac, and just replays migrations `0001..0010`. Importing the bacpac
**preserves all legacy IDs/PKs natively** (faithful copy — no identity remap), which keeps
the FK graph and external references (WasabiCard holder IDs, referral chains, order
numbers) intact.

## Data policy

- **Keep everything.** The whole DB is ~5 MB; importing the bacpac as-is is the faithful,
  lowest-risk option. Operational-log tables (`tblH_API_Log`, `tblH_User_Register`,
  `tblH_User_Login`, `tblH_User_OTP`, `tblH_Partner_Webhook`, `tblT_User_ForgotPassword`)
  are harmless to carry and may be purged later if desired — not required for cutover.
- **One transform: forced credential reset** (step 7). No row values are otherwise changed.
- **Settings coexist** by distinct name: legacy's `Cardholder Topup Commission` /
  `Default User's Commission` / `Max Cards Purchase` are preserved; the migrations seed
  `CardPrice` / `CardDepositFeeRate` / `WasabiCard*` only if missing (no overwrite).

## Prerequisites

1. **A consistent source export.** Today's `az sql db export` of a live DB is NOT
   transactionally consistent. For the real cutover either (a) `az sql db copy` the
   source then export the copy, or (b) quiesce the legacy app and export. (See
   `docs/plans/legacy-data-migration.md` §2.)
2. **A working password-reset link on the live host** — the credential scrub forces every
   user to reset, so verify a reset round-trips on the target host *before* cutover
   (`docs/runbooks/crypto-at-rest-cutover.md`).
3. **Confirm target state — replace vs merge.** This runbook assumes a **clean replace**
   (target effectively empty). Verify the target prd DB has no real post-launch rows
   first; if it does, do NOT drop-and-import — escalate (merge is out of scope here).
4. `KASH_DATA_KEY` provisioned and the bcrypt code already deployed on the target
   (`docs/runbooks/crypto-at-rest-cutover.md` steps 1–2).

## Steps

> Run SQL on the box via `az vm run-command` (the box has `sqlcmd` at `C:\Program Files\SqlCmd`
> and `sqlpackage` per `vm-install-sqlpackage.ps1`). Paths below are on the box.

### 1. Stage the bacpac on the target box
Upload the consistent `.bacpac` to the box (e.g. blob + download, as in the export step).

### 2. (Replace) Drop the existing target DB
Only after prerequisite 3 confirms a clean replace:
```
ALTER DATABASE [<db>] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [<db>];
```

### 3. Import the bacpac (preserves all IDs)
```
sqlpackage /Action:Import /SourceFile:<file>.bacpac /TargetServerName:localhost\SQLEXPRESS /TargetDatabaseName:<db> /TargetTrustServerCertificate:True
```

### 4. Pre-flight verification
```
sqlcmd -S localhost\SQLEXPRESS -d <db> -E -b -i deploy/sql/legacy-import/preflight-checks.sql
```
All length probes must fit; all duplicate `ViolatingGroups` must be 0. **Save this output**
— it is the baseline the post-flight is diffed against. If any probe fails, resolve the
reported rows before migrating (the migrations would otherwise RAISERROR on the same).

### 5. Migrate (ADOPT path → applies 0001..0010)
```
az vm run-command invoke ... --scripts @deploy/scripts/deploy/vm-migrate.ps1 -Env prd
```
`vm-migrate` auto-backs-up the DB before changing anything (`<db>-premigrate-*.bak` in the
SQL default backup dir) — that backup is the rollback point.

### 6. Apply pending app deploy if needed
Ensure the running build matches the migrated schema (`ENV=<env> ./deploy/deploy.sh update`).

### 7. Forced credential reset (the one-off scrub)
```
sqlcmd -S localhost\SQLEXPRESS -d <db> -E -b -i deploy/sql/oneoff/crypto-at-rest-scrub.sql
```
Resets every user password to a non-bcrypt sentinel (forces forgot-password), clears 2FA,
neutralises API secrets. On legacy data this resets the ~4.3k users; admins/2FA/API are
empty so those branches are no-ops.

### 8. Restore bootstrap admin
Legacy has **no admin rows**, so seed/restore the bootstrap admin with a fresh bcrypt hash
(`docs/runbooks/crypto-at-rest-cutover.md` step 4).

### 9. Post-flight verification
```
sqlcmd -S localhost\SQLEXPRESS -d <db> -E -b -i deploy/sql/legacy-import/postflight-checks.sql
```
- User-data row counts + money baseline **identical to step 4** (expected deltas only:
  `SchemaMigrations`, the 4 new empty tables, `tblM_Setting` +13 seeds).
- Ledger holds `0000-baseline-dacpac` + `0001..0010`.
- All migration-created objects present; no untrusted constraints; credential hard-checks 0.

Then confirm **schema parity** against a freshly-built current DB:
```
# build reference: publish dacpac to a scratch DB, run vm-migrate against it, then:
sqlpackage /Action:Extract /SourceDatabaseName:<reference> /TargetFile:current.dacpac ...
sqlpackage /Action:DeployReport /SourceFile:current.dacpac /TargetDatabaseName:<db> /OutputPath:parity.xml ...
```
Expected residual ONLY: the legacy-extra `tblM_Card_Type.NotSupport` column (harmless,
unused) and cosmetic `vw_Card` / `vw_Card_Deposit` view/diagram metadata.

### 10. App smoke + cutover
App smoke test against the migrated DB; a real user password-reset round-trip on the live
host; then the DNS/app cutover. Notify users their passwords were reset; re-issue any API
keys (keep re-issued secrets ≤ 72 bytes — bcrypt ignores input past that).

## Rollback
- Pre-migrate: restore the `vm-migrate` auto-backup (`RESTORE DATABASE`), or re-import the
  bacpac (step 3) for a clean reset.
- The migrations are guarded/idempotent; re-running `vm-migrate` replays guarded no-ops.

## Verified dry-run (2026-06-30)

Full chain rehearsed on a LocalDB copy of the production export
(`kashnow-20260630-040137.bacpac`, 38 tables, 4,358 users, 72 cards):
- Bacpac imported into LocalDB clean — **no Azure→Express incompatibility**.
- Schema parity vs baseline: only the `NotSupport` column + cosmetic views differ.
- Pre-flight: all length probes fit (Address max 34 ≤ 128), all duplicate probes 0.
- All 10 migrations applied clean; post-flight schema diff vs a freshly-built current DB
  matched (same harmless residual).
- Scrub reset 4,358 users; post-flight hard-checks all 0; money totals unchanged.
