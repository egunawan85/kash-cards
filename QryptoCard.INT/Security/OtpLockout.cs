using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;

namespace QryptoCard.INT.Security
{
    // OTP brute-force lockout.
    //
    // Each failed OTP guess atomically increments an unmapped FailureCount column on the
    // OTP-session row; at the threshold the row's isVerify flips to -1, which the verify
    // handlers' "isVerify == 0" lookup then treats as not-found — the attacker sees the
    // unified "session is ended / OTP is wrong" message with no oracle distinguishing a
    // locked session from a missing one.
    //
    // Raw atomic SQL (not an EF read-modify-write) so concurrent wrong-code guesses cannot
    // race the increment: a single UPDATE with a CASE expression lets SQL Server serialize
    // per-row. FailureCount is intentionally unmapped in the EDMX — the only consumer is this
    // UPDATE, so keeping it out of the entity model avoids a multi-project EDMX regen; EF
    // generates column-explicit SELECT/INSERT, so an unmapped column is silently ignored on
    // reads and defaults to NULL on inserts.
    //
    // The table and primary-key column come exclusively from the compile-time constants in
    // Targets below (never request input) and are validated against that allowlist; the
    // guessed session id is always passed as a parameter. The companion DDL
    // (deploy/sql/create-otp-lockout-columns.sql) must be applied to the database BEFORE this
    // code deploys, else the UPDATE fails with "Invalid column name 'FailureCount'".
    public static class OtpLockout
    {
        public const int Threshold = 5;

        // Allowed (table, primary-key-column) targets — the five OTP/login/register session
        // tables. Keyed "table|pk" so the runtime UPDATE can only ever touch a known table.
        private static readonly HashSet<string> Targets = new HashSet<string>(StringComparer.Ordinal)
        {
            "tblH_User_OTP|OTPID",
            "tblH_User_Register|ID",
            "tblH_User_Login|ID",
            "tblH_Admin_OTP|OTPID",
            "tblH_Admin_Login|ID",
        };

        // Atomically count one failed guess against the session row and lock it at the
        // threshold. No-op (zero rows affected) if the row is already consumed/locked
        // (isVerify != 0) or the id doesn't match — harmless on a stale/absent session.
        public static void RecordFailure(Database database, string table, string pkColumn, object pkValue)
        {
            if (database == null || pkValue == null) return;
            if (!Targets.Contains(table + "|" + pkColumn))
                throw new ArgumentException("Unrecognised OTP lockout target: " + table + "." + pkColumn);

            string sql =
                "UPDATE " + table + " SET FailureCount = ISNULL(FailureCount, 0) + 1, " +
                "isVerify = CASE WHEN ISNULL(FailureCount, 0) + 1 >= " + Threshold + " THEN -1 ELSE isVerify END " +
                "WHERE " + pkColumn + " = @p0 AND isVerify = 0";

            database.ExecuteSqlCommand(sql, new SqlParameter("@p0", pkValue));
        }
    }
}
