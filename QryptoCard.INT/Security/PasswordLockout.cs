using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace QryptoCard.INT.Security
{
    // Password brute-force lockout (login). Companion to OtpLockout (which guards the OTP second
    // factor); this guards the first factor — the password gate on Login.
    //
    // Each wrong-password login attempt atomically increments an unmapped FailureCount column on the
    // identity row (tblM_User / tblM_Admin); at the threshold LockoutEnd is set to now + LockoutMinutes.
    // While LockoutEnd is in the future the login handler rejects with the SAME "Your password is
    // incorrect" message it returns for an ordinary wrong password (Option B), so the lockout state is
    // indistinguishable from a normal failure and never becomes an account-enumeration oracle.
    //
    // Raw atomic SQL (not an EF read-modify-write) so concurrent wrong-password guesses cannot race the
    // increment: a single UPDATE with CASE lets SQL Server serialize per-row. FailureCount and
    // LockoutEnd are intentionally unmapped in the EDMX — the only consumers are the UPDATE here and the
    // raw SELECT in IsLockedOut — so keeping them out of the entity model avoids a multi-project model
    // regen. EF generates column-explicit SELECT/INSERT, so unmapped columns are ignored on reads and
    // default to NULL on inserts.
    //
    // The table and primary-key column come exclusively from the compile-time Targets allowlist (never
    // request input) and are validated against it; the identity id is always passed as a parameter. The
    // companion DDL (deploy/sql/create-login-lockout-columns.sql) must be applied to the database BEFORE
    // this code deploys, else the SQL fails with "Invalid column name 'FailureCount'/'LockoutEnd'".
    public static class PasswordLockout
    {
        // Threshold / window are single-sourced in QryptoCard.Sec.LockoutPolicy (whose pure
        // ComputeNextState is the unit-tested mirror of the RecordFailure SQL below).
        public const int Threshold = QryptoCard.Sec.LockoutPolicy.Threshold;
        public const int LockoutMinutes = QryptoCard.Sec.LockoutPolicy.LockoutMinutes;

        // Allowed (table, primary-key-column) targets — the two identity tables. Keyed "table|pk" so the
        // runtime SQL can only ever touch a known table/column.
        private static readonly HashSet<string> Targets = new HashSet<string>(StringComparer.Ordinal)
        {
            "tblM_User|UserID",
            "tblM_Admin|AdminID",
        };

        private static void Validate(string table, string pkColumn)
        {
            if (!Targets.Contains(table + "|" + pkColumn))
                throw new ArgumentException("Unrecognised password lockout target: " + table + "." + pkColumn);
        }

        // True if the identity row is currently locked (LockoutEnd in the future). LockoutEnd is
        // unmapped, so this is a raw SELECT, not EF. Absent row / NULL LockoutEnd → not locked.
        public static bool IsLockedOut(Database database, string table, string pkColumn, object pkValue, DateTime now)
        {
            if (database == null || pkValue == null) return false;
            Validate(table, pkColumn);

            string sql = "SELECT LockoutEnd FROM " + table + " WHERE " + pkColumn + " = @p0";
            DateTime? end = database.SqlQuery<DateTime?>(sql, new SqlParameter("@p0", pkValue)).FirstOrDefault();
            return end.HasValue && end.Value > now;
        }

        // Atomically count one failed password attempt and lock at the threshold. No-op on an unknown id
        // (zero rows affected). The transition MUST stay in lockstep with
        // QryptoCard.Sec.LockoutPolicy.ComputeNextState (the unit-tested pure mirror):
        //   - expired lockout (LockoutEnd set but in the past) → fresh window: FailureCount = 1,
        //     LockoutEnd = NULL. (Critical anti-perpetual-DoS corrective: without it, one wrong attempt
        //     right after expiry would immediately re-lock — lock/expire/one-guess/re-lock forever.)
        //   - otherwise → FailureCount += 1, and LockoutEnd = lockUntil iff the increment reaches the
        //     threshold (else LockoutEnd unchanged).
        public static void RecordFailure(Database database, string table, string pkColumn, object pkValue, DateTime now)
        {
            if (database == null || pkValue == null) return;
            Validate(table, pkColumn);

            DateTime lockUntil = now.AddMinutes(LockoutMinutes);

            // Both SET right-hand sides read the pre-update row values (SQL Server evaluates them against
            // the old row), so the two CASE expressions see a consistent prior FailureCount/LockoutEnd.
            //
            // The WHERE additionally gates on the row NOT being actively locked
            // (LockoutEnd IS NULL OR LockoutEnd <= @now), so the DB row is the single source of truth for
            // the lock: a concurrent wrong-password burst that all passed the earlier IsLockedOut SELECT
            // can no longer keep incrementing (or pushing out LockoutEnd) once one of them has set the
            // lock — the stragglers update zero rows. Rows that are NULL or already-expired still match,
            // so the normal-increment and expired-window-reset paths are unaffected.
            string sql =
                "UPDATE " + table + " SET " +
                "FailureCount = CASE WHEN LockoutEnd IS NOT NULL AND LockoutEnd <= @now THEN 1 " +
                "ELSE ISNULL(FailureCount, 0) + 1 END, " +
                "LockoutEnd = CASE WHEN LockoutEnd IS NOT NULL AND LockoutEnd <= @now THEN NULL " +
                "WHEN ISNULL(FailureCount, 0) + 1 >= " + Threshold + " THEN @lockUntil " +
                "ELSE LockoutEnd END " +
                "WHERE " + pkColumn + " = @p0 AND (LockoutEnd IS NULL OR LockoutEnd <= @now)";

            database.ExecuteSqlCommand(sql,
                new SqlParameter("@p0", pkValue),
                new SqlParameter("@now", now),
                new SqlParameter("@lockUntil", lockUntil));
        }

        // Clear the counter and any lock after a correct password.
        public static void RecordSuccess(Database database, string table, string pkColumn, object pkValue)
        {
            if (database == null || pkValue == null) return;
            Validate(table, pkColumn);

            string sql = "UPDATE " + table + " SET FailureCount = 0, LockoutEnd = NULL WHERE " + pkColumn + " = @p0";
            database.ExecuteSqlCommand(sql, new SqlParameter("@p0", pkValue));
        }
    }
}
