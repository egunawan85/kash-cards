# Legacy KashNow → kash-cards data migration

Plan for migrating the **live legacy KashNow** production database into the **new
kash-cards** platform as part of the production cutover. Read-only on the legacy
source; the import/migration work all happens on throwaway scratch copies until
the real cutover.

---

## 0. Audit results (2026-06-30) — dry-run GREEN

The S0–S3 audit ran end-to-end on a LocalDB copy of the production export
(`kashnow-20260630-040137.bacpac`; 38 tables, 4,358 users, 72 cards). Outcome:

- **S0 import:** bacpac imported into LocalDB clean — **no Azure SQL → Express
  incompatibility** (the top compat risk, cleared).
- **S1 schema parity:** legacy vs baseline diff is near-empty — only a harmless
  legacy-extra `tblM_Card_Type.NotSupport` column + cosmetic `vw_Card`/`vw_Card_Deposit`
  view/diagram metadata. No object our baseline needs is missing → 2-step adopt confirmed.
- **S2 dry-run:** pre-flight all green (Address max len 34 ≤ 128; `TXID`/`UserReferenceID`
  empty; **all duplicate probes 0**); all **10 migrations applied clean**; post-flight
  schema diff vs a freshly-built current DB matched (same harmless residual).
- **S3 policy:** **keep everything** (DB ~5 MB; faithful copy, lowest risk); the only
  transform is the forced credential reset; legacy settings coexist by name with the
  seeded ones. Full chain (import → migrate → scrub → post-flight) rehearsed: scrub reset
  4,358 users, post-flight hard-checks all 0, money totals unchanged, no untrusted
  constraints.

Deliverables: `deploy/sql/legacy-import/preflight-checks.sql`,
`deploy/sql/legacy-import/postflight-checks.sql`,
`docs/runbooks/legacy-import-cutover.md`. Remaining: S5 (consistent export + target
replace-vs-merge check + cutover), gated on operator scheduling.

---

## 1. The two systems are different at the engine level

The most important fact to internalise: **source and target run different database
engines.** This is not a like-for-like restore.

| | Legacy KashNow (SOURCE) | New kash-cards (TARGET) |
|---|---|---|
| Engine | **Azure SQL Database** (managed PaaS) | **SQL Server Express on the VM** |
| Host | `gendb.database.windows.net` | `localhost\SQLEXPRESS` on `vm-kash-prd` |
| Subscription | `Qrypto` (`8f9931d8-…`) | "Subscription qrypto" (`7135879b-…`) |
| Auth | SQL login (`gendb`) | Windows integrated; app uses a least-priv SQL login |
| Schema mgmt | (legacy app / EDMX) | Frozen dacpac **baseline** + ordered run-once `migrations/NNNN-*.sql`, tracked in `dbo.SchemaMigrations` (`deploy/scripts/deploy/vm-migrate.ps1`) |

"Cloud" is true for both only in that the target is an Azure **VM** — the target DB
engine is on-box SQL Express, not a managed Azure SQL Database. The export artifact
crossing between them is a **`.bacpac`** (portable schema+data export).

### Terminology (for reference)
- **BACPAC** — portable logical export (schema + data). Importing one *creates* a DB;
  you cannot pour a bacpac's data into a pre-existing, differently-shaped schema.
- **DDL** — Data Definition Language (`CREATE`/`ALTER`/`DROP`; structure).
  **DML** — Data Manipulation Language (`INSERT`/`UPDATE`; data). Our migration set is
  mostly DDL, with two DML seed migrations (0008, 0009).
- **Idempotent** — safe to re-run, same result. Note: idempotent ≠ drift-proof.
- We guard against **logical data-integrity loss** (truncation, dropped rows,
  constraint violations, orphans) — not physical *corruption* (page damage), which is
  a different thing.

---

## 2. Source export artifact (done 2026-06-30)

- Method: Azure SQL **server-side** export (`az sql db export`) — runs inside Azure,
  read-only on the source, never touches GENVM. Chosen because GENVM has no
  SqlPackage/azcopy/sqlcmd.
- DB exported: **`kashnow`** on `gendb.database.windows.net` (38 tables, ~0.04 GB).
  Confirmed production (live `api.kash.now`). Credentials live in the legacy app's
  `…\Production\KashNow\QryptoCard.INT*\web.config` (`DBEntities`) — not repeated here.
- Artifact: `kashnow-20260630-040137.bacpac` (~4.9 MB), in temp storage account
  `kashnowexport5412` / container `exports` (Qrypto sub, RG `Gen`). A read-only SAS is
  generated on demand; local working copy under `tmp/`.
- **Consistency caveat:** a direct export of a live DB is **not** transactionally
  consistent. Fine for analysis/dry-runs. For the REAL cutover, take a consistent
  source first (`az sql db copy` then export the copy), or quiesce the legacy app and
  do a final delta. Delete the temp storage account after the import.

---

## 3. Strategy: B (import-then-migrate) — a clean 2-step

Chosen: **Strategy B** — treat the legacy DB as the starting point and bring it
forward to the current kash-cards schema, mirroring how `vm-migrate.ps1` already
deploys (baseline → ordered migrations → ledger). Dry-runs on **LocalDB**
(`(localdb)\MSSQLLocalDB`) — same engine family as the SQL Express target, free,
disposable.

**Schema lineage (verified 2026-06-30):** our dacpac **baseline IS the `kashnow`
schema, 1:1.** The baseline EDMX (`QryptoCard.INT/DB.edmx` storage model) and legacy
`kashnow` have the **identical 38 tables** — same names, no extras on either side.
Our schema was taken straight from kashnow (the dev DB is even named
`qrypto-card-kashnow`); the other DBs on the server (`Qelola`, `QelolaMitra`,
`QelolaPartner`, `FraudManagement`) contributed **nothing** — no schemas were
combined. (Code/patterns were borrowed from sister apps `runegate`/`qrypto-omni`, but
not tables.)

Because legacy already *is* the baseline, `vm-migrate.ps1`'s **ADOPT path** applies
directly: it sees `dbo.tblM_User`, skips the dacpac, and just replays the migrations.
So Strategy B is **two steps**:

1. **Import** legacy `.bacpac` → scratch LocalDB (legacy schema + data).
2. **Migrate** — run `migrations/0001..0010` in order (the `vm-migrate` engine).
   Everything our current schema has beyond the baseline is **created by the
   migrations themselves** (see §4), so no separate catch-up/dacpac step is needed.
   The post-flight schema diff then proves the result equals a freshly-built current DB.

The gap migrations must close (all migration-created, none pre-existing in legacy):
new tables `tblT_AuthToken`/`tblT_RefreshToken`/`tblH_Auth_Log` (0001),
`tblH_WasabiCard_Refill` (0009), the `dbo.SchemaMigrations` ledger; new columns
`FailureCount`/`LockoutEnd` (0005/0006); column narrowings, dedup indexes, and seeded
settings (0002–0010). All ten migrations' dependency tables exist in legacy.

**Fallback — Strategy A (build-then-load):** only if Phase-1 surfaces unexpected
column-level drift — build a pristine current DB (dacpac + all migrations) and
bulk-load the legacy *data* table-by-table in FK order. More control, more work.

---

## 4. Migration inventory & predicted impact on legacy data

All ten migrations are `IF [NOT] EXISTS`-guarded (re-runnable) and the risky ones
carry their own pre-flight duplicate/length probes that `RAISERROR` actionably rather
than failing on a cryptic index error. "Depends on" = the table must already exist
(from legacy import or the §3-step-2 catch-up) or the migration errors.

| # | File | Kind | Depends on | Impact on legacy data / risk |
|---|------|------|-----------|------------------------------|
| 0001 | token-tables | DDL, new tables | — | Creates `tblT_AuthToken`, `tblT_RefreshToken`, `tblH_Auth_Log`. Pure-additive, no legacy dependency. Safe. |
| 0002 | wallet-indexes | DDL, narrow + unique idx | `tblM_User_Crypto_Deposit`, `tblM_User_Balance` | Both tables **exist in legacy**. Narrows `Address` → `nvarchar(128)` with **no length pre-check** (fails if a legacy Address > 128). 3 filtered unique indexes w/ dup probes: one active addr per (User,Network); one active balance per (User,Currency); one active Address globally. |
| 0003 | webhook-dedup-index | DDL, narrow + unique idx | `tblH_Partner_Webhook_ID` | Table **exists in legacy**. Narrows `TXID` → `nvarchar(200)` (fails if legacy TXID > 200). Unique idx on TXID where Type='PGCrypto' w/ dup probe. |
| 0004 | referral-commission-dedup-index | DDL, unique idx | `tblH_Partner_Webhook_ID` | Filtered unique idx on TXID where Type='ReferralCommission' w/ dup probe. |
| 0005 | otp-lockout-columns | DDL, add columns | `tblH_User_OTP/_Register/_Login`, `tblH_Admin_OTP/_Login` | Adds nullable `FailureCount`. Additive, safe — but those 5 tables must exist. |
| 0006 | login-lockout-columns | DDL, add columns | `tblM_User`, `tblM_Admin` | Adds nullable `FailureCount`, `LockoutEnd`. Additive, safe (legacy has these tables). |
| 0007 | card-idempotency-index | DDL, narrow + unique idx | `tblT_Card` | Narrows `UserReferenceID` → `nvarchar(100)` **with** a length pre-check (RAISERROR if any > 100). Filtered unique idx on (UserID, UserReferenceID). Legacy rows likely NULL/'' here → excluded by the filter. |
| 0008 | card-global-pricing-settings | DML, seed | `tblM_Setting` | Inserts `CardPrice`=0, `CardDepositFeeRate`=3 if missing. Never overwrites existing. Safe. |
| 0009 | wasabicard-autofund | DDL new table + DML seed | `tblM_Setting` | Creates `tblH_WasabiCard_Refill` (new) + seeds autofund settings; **kill-switch `WasabiCardAutoFundEnabled` defaults 0/OFF**. Safe. |
| 0010 | card-refund-dedup-indexes | DDL, unique idx | `tblH_Partner_Webhook_ID` | Two filtered unique indexes (CardRefund, ReferralCommissionReversal) on TXID w/ dup probes. |

**The dominant risks, in order** (note: "missing baseline tables" is NOT a risk —
all ten migrations' dependency tables exist in legacy; §3):
1. **Column-narrowing failures** — legacy `Address` > 128 (0002), `TXID` > 200
   (0003/0004/0010), `UserReferenceID` > 100 (0007). Pre-flight length probes below.
2. **Unique-index duplicate violations** — real legacy dups in any of the dedup keys
   would make a migration RAISERROR. Pre-flight dup probes below; resolve (deactivate
   stale rows) before the real run.
3. **Azure SQL → SQL Express bacpac compatibility** — Azure-specific objects/settings
   in the bacpac can choke a LocalDB/Express import. Surfaces at Phase 0.
4. **Column-level drift** — table names match 1:1, but a column added/altered on the
   live legacy DB after the baseline snapshot (and not folded into our baseline) would
   show up here. Phase-1 schema diff confirms; not expected, but verify don't assume.

---

## 5. The phased plan

**Phase 0 — Stand up the scratch copy.** Import the `.bacpac` into a throwaway
LocalDB (`SqlPackage /Action:Import`, or the dotnet `microsoft.sqlpackage` tool).
Confirms Azure→Express compatibility. Read-only on the live source.

**Phase 1 — Schema diff (column-level).** Table names already match 1:1 (§3); this
pass confirms column-level parity between scratch-legacy and the dacpac baseline and
flags any post-snapshot drift on the live legacy DB that would break the dry-run.

**Phase 2 — Data audit.** Per-table classify **keep** (real customer money/data:
users, cards, deposits, balances), **drop** (logs, sessions, temp/staging,
integration-test rows, dead audit churn), **transform** (anything whose shape
changed). Capture row counts + money aggregates as the pre-flight baseline.

**Phase 3 — Predict migration impact.** Already mostly done (§4). Finalise the
pre-flight probe queries against the actual legacy data.

**Phase 4 — Dry-run the migration.** On a copy: import → catch-up → run
`migrations/0001..0010` (the `vm-migrate` engine). Either it lands clean (prove it
with a post-flight schema diff vs a freshly-built current DB) or it fails — record
where, adjust, repeat until clean.

**Phase 5 — Wire pre/post-flight checks** (§6).

**Phase 6 — Cutover rehearsal & the real thing.** Use a *consistent* final export
(§2 caveat). Out of scope for this analysis pass.

---

## 6. Pre-flight & post-flight checks

**Pre-flight (before applying migrations / before cutover):**
- Row counts per table (baseline).
- Money/state aggregates: `SUM` of balances, `SUM`/count of deposits, card counts by
  status — the "must not change" baseline.
- **Column-length probes** (drive the narrowing migrations): max `LEN` of
  `tblM_User_Crypto_Deposit.Address` (≤128), `tblH_Partner_Webhook_ID.TXID` (≤200),
  `tblT_Card.UserReferenceID` (≤100).
- **Duplicate probes** for every unique index introduced (the exact `GROUP BY … HAVING
  COUNT(*) > 1` queries embedded in 0002/0003/0004/0007/0010). Zero rows = safe.
- Orphan / referential-integrity scan on the keep-set.

**Post-flight (after migrations applied):**
- Re-run row counts; reconcile vs pre-flight with an *expected delta* per table
  (explain every intentional change).
- Re-run money aggregates → must equal pre-flight exactly (no value drift) unless a
  migration intentionally transformed them.
- **Schema diff: migrated DB vs a freshly-built current kash-cards DB → must be
  identical.** This is the proof migrations brought legacy to exactly current schema.
- `dbo.SchemaMigrations` shows `0000-baseline-dacpac` + `0001..0010` all recorded.
- All FKs / CHECK constraints are **trusted** (not left `WITH NOCHECK`) — a classic
  silent post-migration trap.
- App smoke test against the migrated DB in a staging slot.

---

## 7. Work breakdown — slices & tasks

Sliced **risk-first**, so the technical and product design issues surface as early as
possible. S0 is the foundation; S1/S2/S3 then run in parallel off the same scratch DB;
S4 codifies checks alongside; S5 is the cutover. Each slice ends in a concrete artifact
and a named decision it forces.

### S0 — Scratch-import harness *(foundation)*
- **0a** Resolve SqlPackage locally (dotnet tool `microsoft.sqlpackage`).
- **0b** Import `kashnow-*.bacpac` → `(localdb)\MSSQLLocalDB` DB `kashnow_scratch`.
- **0c** Idempotent drop+reimport script (rebuild-from-bacpac on demand).
- *Surfaces:* **Azure SQL → LocalDB/Express compatibility** — unsupported features in
  the bacpac fail here, immediately. Artifact: a queryable local legacy copy.

### S1 — Schema parity
- **1a** Build a pristine baseline DB from the dacpac (also the post-flight reference).
- **1b** Column-level schema diff: scratch-legacy vs baseline.
- **1c** Triage drift (expected: only the known `nvarchar(max)` widths the migrations narrow).
- *Surfaces:* **post-snapshot legacy drift** — confirms the 2-step adopt path is valid
  (table names already match 1:1, §3).

### S2 — Migration dry-run *(core technical de-risk)*
- **2a** Pre-flight probes on real legacy data: column lengths (`Address`≤128,
  `TXID`≤200, `UserReferenceID`≤100) + the dup probes for every unique index.
- **2b** Run the `vm-migrate` ADOPT path against scratch
  (`-DbServer (localdb)\MSSQLLocalDB -DbName kashnow_scratch`).
- **2c** Capture failures; if dups/oversize exist, design the remediation (deactivate
  stale rows / data fix) — these are real money-integrity findings.
- **2d** Post-flight schema diff: migrated scratch vs a fresh baseline+migrations build
  → must be identical.
- *Surfaces:* **duplicate deposits/addresses, oversized values** — whether the app's
  required money-safety indexes can even exist on the legacy data.

### S3 — Data keep/drop/transform policy *(core design decisions — needs sign-off)*
- **3a** Row counts + money aggregates per table (the pre-flight baseline numbers).
- **3b** Draft keep/drop/transform classification (logs / sessions / test rows → drop).
- **3c** **Credential-at-rest reconciliation — DECIDED: forced reset, reuse the
  existing one-off.** Legacy `kashnow` passwords/secrets are under the same old
  reversible Rijndael cipher the crypto-at-rest work already retired. The existing
  `deploy/sql/oneoff/crypto-at-rest-scrub.sql` (operator-run once, per
  `docs/runbooks/crypto-at-rest-cutover.md`) does exactly what we need on the imported
  data: overwrites every `tblM_User`/`tblM_Admin` password with a non-bcrypt sentinel
  (`!RESET-REQUIRED-CRYPTO-MIGRATION!`) → forces forgot-password; deletes `tblM_User_2FA`
  + clears the 2FA flag; neutralises `tblM_User_API` secrets + deactivates the keys. So
  the legacy-import credential reset is **run that scrub as a cutover step** (S5), not a
  new mechanism. Hard prerequisite (per the runbook): a working password-reset link on
  the live host before cutover.
- **3d** **ID-preservation.** Preserve legacy PKs / identity values (recommended —
  keeps the FK graph and external references like WasabiCard holder IDs and referral
  chains intact) vs remap on load.
- **3e** Review with operator → settled policy.
- *Surfaces:* the biggest product decisions — credentials and identity continuity.

### S4 — Reconciliation harness
- **4a** Pre-flight script (counts, aggregates, length/dup probes, orphan scan).
- **4b** Post-flight script (re-counts vs expected deltas, aggregate equality, schema
  diff, ledger check `0000`+`0001..0010`, trusted-constraint check).
- **4c** Reconciliation report format.
- *Surfaces:* which per-table deltas are "expected" — forces explicit accounting.

### S5 — Consistent export + cutover rehearsal & runbook
- **5a** **Verify current kash-cards PRD DB state** (empty vs has real rows since the
  2026-06-28 launch — referral test, autofund refills) → confirms **replace vs merge**.
- **5b** Consistent source export (`az sql db copy` → export, or quiesce legacy app +
  final delta) — replaces today's non-consistent export.
- **5c** End-to-end rehearsal on a prd-like target (import → migrate → reconcile → app
  smoke).
- **5d** Cutover runbook + rollback (`vm-migrate` auto-backs-up pre-migrate; verify the
  restore path).
- *Surfaces:* downtime window, rollback, and app/DNS cutover coordination.

## 8. Decisions needed (surfaced by the slicing)
1. ~~**Credentials (3c):**~~ **DECIDED** — forced reset via the existing one-off
   `deploy/sql/oneoff/crypto-at-rest-scrub.sql`, run as a cutover step (not a migration).
2. ~~**Identity (3d):**~~ **DECIDED** — preserve legacy IDs (faithful copy, FK graph +
   external refs intact), conditional on a clean/empty target (5a `IDENTITY_INSERT` load).
3. **Target state (5a):** is kash-cards PRD effectively empty → clean **replace**, or
   does it hold real post-launch rows → **merge**? Verify before any import design.
4. **Cutover consistency (5b):** copy-then-export vs quiesce+delta — decide before S5.
5. **Strategy A fallback:** only if S1 shows unexpected column drift.
6. **Execution shape:** run S0–S3 in one audit worktree and PR the findings
   *(recommended)*, vs a worktree per slice.
