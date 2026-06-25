using System;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Idempotent, decoupled post-registration provisioning. OTP-verify only flips the
    /// verify/active gate; everything that used to run inline in that transaction — the
    /// wallet row, the static deposit address, the referral code, the commission row —
    /// is created here, create-if-missing, on a short-lived context per piece.
    ///
    /// This exists because the old inline chain ran un-null-checked gateway calls
    /// mid-transaction: a gateway timeout NPE'd, the catch swallowed it, and the account
    /// was left verified-and-active but address-less while the verify response reported
    /// "error" — and a retry hit "session ended" (isVerify already 1). Decoupling makes
    /// verify always succeed once the code is valid, and lets provisioning be retried
    /// safely on any later access (registration, first card action, first wallet view)
    /// or by a shakeout backfill pass.
    ///
    /// Every step is independent and best-effort: a failure in one (e.g. the gateway is
    /// down so the address can't be minted) does not block the others and is reported,
    /// not thrown, so the caller can decide whether to surface or simply repair later.
    /// </summary>
    public static class UserProvisioningService
    {
        public class ProvisionResult
        {
            public bool WalletOk { get; set; }
            public bool AddressOk { get; set; }
            public bool ReferralOk { get; set; }
            public bool CommissionOk { get; set; }

            public bool FullyProvisioned
            {
                get { return WalletOk && AddressOk && ReferralOk && CommissionOk; }
            }
        }

        /// <summary>
        /// Ensure every per-user artifact exists. Idempotent and safe to call repeatedly;
        /// never throws for an individual step's failure (the failing flags are returned
        /// instead). Suitable for the OTP-verify path, lazy first-access repair, and a
        /// one-time backfill over existing users.
        /// </summary>
        public static ProvisionResult EnsureUserProvisioned(string userId)
        {
            var result = new ProvisionResult();
            if (string.IsNullOrEmpty(userId)) return result;

            // Wallet + deposit address own their race-safety and gateway-null handling.
            result.WalletOk = TryStep(() => WalletService.EnsureWallet(userId) != null);
            result.AddressOk = TryStep(() => WalletService.EnsureDepositAddress(userId) != null);
            result.ReferralOk = TryStep(() => EnsureReferral(userId));
            result.CommissionOk = TryStep(() => EnsureCommission(userId));
            return result;
        }

        private static bool TryStep(Func<bool> step)
        {
            try { return step(); }
            catch { return false; }
        }

        /// <summary>Create-if-missing a referral row with a unique-ish random code.</summary>
        private static bool EnsureReferral(string userId)
        {
            using (var ctx = new DBEntities())
            {
                if (ctx.tblM_User_Referral.Any(p => p.UserID == userId)) return true;

                var reff = new tblM_User_Referral
                {
                    UserID = userId,
                    DateCreated = DateTime.Now,
                    Code = Common.RandomString(8),
                    CreatedBy = "system"
                };
                ctx.tblM_User_Referral.Add(reff);
                try
                {
                    ctx.SaveChanges();
                    return true;
                }
                catch (DbUpdateException ex) when (WalletService.IsDuplicateKey(ex))
                {
                    // Lost a create race — the row now exists; treat as provisioned.
                    return true;
                }
            }
        }

        /// <summary>
        /// Create-if-missing a commission row, seeded from the system default
        /// (tblM_Setting ID 2, fallback 0.1) — mirrors the original registration logic.
        /// </summary>
        private static bool EnsureCommission(string userId)
        {
            using (var ctx = new DBEntities())
            {
                if (ctx.tblM_User_Commission.Any(p => p.UserID == userId)) return true;

                var setting = ctx.tblM_Setting.Where(p => p.ID == 2).FirstOrDefault();
                double rate = setting != null && setting.Value.HasValue ? setting.Value.Value : 0.1;

                var comm = new tblM_User_Commission
                {
                    UserID = userId,
                    CommissionID = Guid.NewGuid().ToString(),
                    DateCreated = DateTime.Now,
                    Commission = rate,
                    CreatedBy = "system"
                };
                ctx.tblM_User_Commission.Add(comm);
                try
                {
                    ctx.SaveChanges();
                    return true;
                }
                catch (DbUpdateException ex) when (WalletService.IsDuplicateKey(ex))
                {
                    return true;
                }
            }
        }
    }
}
