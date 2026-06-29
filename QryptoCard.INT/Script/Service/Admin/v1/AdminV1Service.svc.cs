using Newtonsoft.Json;
using QryptoCard.INT.Model;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service.Auth.v1;
using QryptoCard.INT.Security;
using QryptoCard.Sec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "AdminV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select AdminV1Service.svc or AdminV1Service.svc.cs at the Solution Explorer and start debugging.
    public class AdminV1Service : IAdminV1Service
    {
        // Two per-instance contexts, mirroring the AuthV1Service pattern:
        //   - db:     legacy DBEntities (.edmx) for tblM_Admin / tblM_User / vw_Admin
        //   - authDb: code-first AuthDbContext, used here to append the test-credit
        //             audit row to tblH_Auth_Log (the shared security audit ledger).
        DBEntities db;
        AuthDbContext authDb;
        OutputModel op = new OutputModel();

        // Production ctor — field-initialise both contexts from config.
        public AdminV1Service()
        {
            db = new DBEntities();
            authDb = new AuthDbContext();
        }

        // Test ctor — point both contexts at an explicit connection (the legacy
        // DBEntities takes an EntityConnection string; AuthDbContext a plain
        // SqlClient string), both targeting the same LocalDB under test.
        public AdminV1Service(DBEntities legacyDb, AuthDbContext authContext)
        {
            db = legacyDb;
            authDb = authContext;
        }

        string getAdminId(string em)
        {
            var a = db.tblM_Admin.Where(p => p.Email == em).FirstOrDefault();
            return a.AdminID;
        }

        // Null-safe AdminID lookup for audit: returns null rather than throwing when
        // no row matches (the authenticated-email contract means this is normally
        // populated, but the audit path must never crash on a missing row).
        string tryGetAdminId(string em)
        {
            var a = db.tblM_Admin.Where(p => p.Email == em).FirstOrDefault();
            return a?.AdminID;
        }

        // protected virtual so DB-less tests can supply a role without the physical
        // vw_Admin object (an EF defining-query view that the LocalDb test fixture
        // does not materialise). Production behaviour is unchanged.
        protected virtual string getRole(string em)
        {
            var a = db.vw_Admin.Where(p => p.Email == em).FirstOrDefault();
            return a == null ? null : a.Role;
        }

        // Allowlist: ONLY Owner/Admin may read/act on other admins' records.
        // Deny-by-default (case/whitespace-insensitive) so an unknown, null, or variant
        // role string can't slip through.
        bool isDeniedAdminManage(string em)
        {
            var role = (getRole(em) ?? "").Trim();
            return !(role.Equals(RoleModel.Owner, StringComparison.OrdinalIgnoreCase)
                  || role.Equals(RoleModel.Admin, StringComparison.OrdinalIgnoreCase));
        }

        // ---------- devCreditWallet (dev-only test-credit tool, SD-2) ----------

        // Per-call ceiling on a single test credit. A dev-only convenience to blunt a
        // fat-finger (e.g. a stray extra zero); it is NOT a security control — the
        // environment gate is. Kept generous because the sandbox merchant wallet that
        // backs real card buys is itself funded ~$10M.
        const decimal MaxTestCreditAmount = 1000000m;

        // Audit EventType written to tblH_Auth_Log for every test-credit attempt,
        // including refusals (so probes leave a trail, matching the auth ledger).
        const string TestCreditEventType = "dev_test_credit";

        // Namespacing prefix on the credit's TransactionID. Guarantees a test credit
        // can never collide with — or be mistaken for — a real PGCrypto provider
        // TransactionID in the shared dedup table, while still reusing the exact
        // verified CreditDeposit dedup/idempotency path.
        const string TestCreditTxnPrefix = "DEVCREDIT-";

        /// <summary>
        /// Dev-only tool that credits a user's wallet through the existing verified
        /// credit path (<see cref="WalletService.CreditDeposit"/>) so sandbox spend
        /// flows have a fundable balance without a real USDT deposit. Walled three ways
        /// (defense in depth):
        ///   1. Environment hard-gate (load-bearing, fail-closed): refuses unless
        ///      QRYPTO_ENVIRONMENT is an explicit dev/sandbox value. Checked FIRST, so
        ///      prod money-minting is impossible even for a root-admin caller.
        ///   2. Root-admin (Owner) only — the highest-privilege role.
        ///   3. Audit-logged — every attempt (credited or refused) appends a
        ///      tblH_Auth_Log row capturing who/when/amount/target/outcome.
        /// Idempotent: pass a stable <paramref name="reference"/> and a replay dedupes
        /// to "duplicate_event" rather than double-crediting.
        /// </summary>
        public OutputModel devCreditWallet(string em, string userId, decimal amount, string reference)
        {
            try
            {
                // Resolve the acting admin up front for the audit trail. These are
                // reads only — no money moves before the gates below pass — and they
                // sit inside the try so a DB hiccup returns a clean error rather than
                // an unhandled fault.
                string adminId = tryGetAdminId(em);
                string actingRole = (getRole(em) ?? "").Trim();

                // WALL 1 (load-bearing, fail-closed) — environment hard-gate. Checked
                // before role and before any mutation: an attacker with full root-admin
                // in production still cannot reach the credit.
                if (!TestCreditGate.IsAllowedEnvironment())
                {
                    auditTestCredit(adminId, userId, amount, reference, "env_refused");
                    op.Status = "failed";
                    op.Message = "Test-credit is disabled in this environment.";
                    return op;
                }

                // WALL 2 — root-admin (Owner) only. Deny-by-default: anything that is
                // not exactly Owner (case/whitespace-insensitive) — Admin, Viewer, an
                // unknown/null role — is refused.
                if (!actingRole.Equals(RoleModel.Owner, StringComparison.OrdinalIgnoreCase))
                {
                    auditTestCredit(adminId, userId, amount, reference, "not_owner");
                    op.Status = "failed";
                    op.Message = "You are not authorized to run this endpoint.";
                    return op;
                }

                // Input validation.
                if (string.IsNullOrWhiteSpace(userId))
                {
                    op.Status = "failed";
                    op.Message = "A target userId is required.";
                    return op;
                }
                if (amount <= 0m)
                {
                    op.Status = "failed";
                    op.Message = "Amount must be greater than zero.";
                    return op;
                }
                if (amount > MaxTestCreditAmount)
                {
                    auditTestCredit(adminId, userId, amount, reference, "amount_over_cap");
                    op.Status = "failed";
                    op.Message = "Amount exceeds the per-call test-credit cap.";
                    return op;
                }

                // Target user must exist — never credit a wallet for a non-user.
                var targetUser = db.tblM_User.Where(p => p.UserID == userId).FirstOrDefault();
                if (targetUser == null)
                {
                    auditTestCredit(adminId, userId, amount, reference, "user_not_found");
                    op.Status = "failed";
                    op.Message = "Target user not found.";
                    return op;
                }

                // Namespaced idempotency key. A caller-supplied reference makes the
                // credit idempotent (replay dedupes); otherwise each call is unique.
                string txid = TestCreditTxnPrefix +
                    (string.IsNullOrWhiteSpace(reference) ? Guid.NewGuid().ToString() : reference.Trim());

                // Route through the existing verified credit path: EnsureWallet first
                // (a first-time target otherwise fails closed as wallet_missing), then
                // the atomic, deduped CreditDeposit (ledger + balance in one tx).
                WalletService.EnsureWallet(userId);
                var res = WalletService.CreditDeposit(
                    userId: userId,
                    netAmount: amount,
                    commission: 0m,
                    commissionInPercentage: 0d,
                    transactionId: txid,
                    status: "DEV_TEST_CREDIT",
                    dedupRequest: JsonConvert.SerializeObject(
                        new { tool = "devCreditWallet", by = em, reference = reference }, Formatting.None));

                if (!res.Success)
                {
                    auditTestCredit(adminId, userId, amount, txid, "credit_failed:" + res.FailureReason);
                    op.Status = "failed";
                    op.Message = res.FailureReason == "duplicate_event"
                        ? "This test-credit reference has already been applied."
                        : "Test-credit failed: " + res.FailureReason;
                    return op;
                }

                auditTestCredit(adminId, userId, amount, txid, "credited");
                op.Status = "success";
                op.Message = "Test-credit applied.";
                op.Data = JsonConvert.SerializeObject(
                    new { transactionId = txid, balanceNew = res.BalanceNew }, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        // ---------- refundCard (admin card refund) ----------

        // Audit EventType written to tblH_Auth_Log for every refund attempt (incl. refusals),
        // so probes/denials leave a trail exactly like the test-credit tool.
        const string CardRefundEventType = "admin_card_refund";

        /// <summary>
        /// Admin-only card refund. Cancels the whole WasabiCard at the provider and returns the card's
        /// unused balance to the buyer's wallet, clawing back any referral commission paid on the card's
        /// orders (<see cref="CardRefundService.RefundByOrder"/>). Walled two ways (defense in depth):
        ///   1. Root-admin (Owner) only — the highest-privilege role; deny-by-default.
        ///   2. Audit-logged — every attempt (refunded or refused) appends a tblH_Auth_Log row capturing
        ///      who/when/order/outcome.
        /// Unlike the dev-credit tool there is NO environment gate: a refund returns the user's own money
        /// and must work in production. The money path itself (cancel -> credit) is atomic, deduped per
        /// card, and finalizes synchronously from the cancel response.
        /// </summary>
        public OutputModel refundCard(string em, string orderId)
        {
            // Resolved up front so the outer catch can still attribute the audit row to the acting admin.
            string adminId = null;
            try
            {
                adminId = tryGetAdminId(em);
                string actingRole = (getRole(em) ?? "").Trim();

                // WALL — root-admin (Owner) only. Deny-by-default: anything not exactly Owner is refused.
                if (!actingRole.Equals(RoleModel.Owner, StringComparison.OrdinalIgnoreCase))
                {
                    auditCardRefund(adminId, orderId, "not_owner", null);
                    op.Status = "failed";
                    op.Message = "You are not authorized to run this endpoint.";
                    return op;
                }

                if (string.IsNullOrWhiteSpace(orderId))
                {
                    auditCardRefund(adminId, orderId, "missing_order", null);
                    op.Status = "failed";
                    op.Message = "An order id is required.";
                    return op;
                }

                var result = CardRefundService.RefundByOrder(orderId.Trim(), em);

                auditCardRefund(adminId, orderId, result.Outcome, result);

                if (!result.Success)
                {
                    op.Status = "failed";
                    op.Message = result.Message ?? ("Refund failed: " + result.Outcome);
                    return op;
                }

                op.Status = "success";
                op.Message = result.Message;
                op.Data = JsonConvert.SerializeObject(new
                {
                    cardNo = result.CardNo,
                    refundedAmount = result.RefundedAmount,
                    buyerBalanceNew = result.BuyerBalanceNew,
                    commissionsReversed = result.CommissionsReversed
                }, Formatting.None);
            }
            catch (Exception ex)
            {
                // Audit the error path too (every attempt leaves a trail, matching the doc-comment).
                auditCardRefund(adminId, orderId, "error:" + ex.Message, null);
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        // Appends one audit row to the shared security ledger (tblH_Auth_Log) for a card-refund attempt.
        // Best-effort: the money mutation (when it happened) is already durably recorded in the balance
        // ledger, so an audit-write failure must surface but never mask or undo the primary outcome.
        void auditCardRefund(string adminId, string orderId, string outcome, CardRefundService.RefundResult result)
        {
            try
            {
                authDb.tblH_Auth_Log.Add(new tblH_Auth_Log
                {
                    LogID = Guid.NewGuid().ToString(),
                    EventType = CardRefundEventType,
                    Subject = adminId,
                    SubjectType = "admin",
                    RefreshTokenID = null,
                    RotationChainRoot = null,
                    SourceIP = WcfSourceIp.TryGet(),
                    Details = JsonConvert.SerializeObject(new
                    {
                        orderId = orderId,
                        outcome = outcome,
                        cardNo = result?.CardNo,
                        refundedAmount = result?.RefundedAmount,
                        commissionsReversed = result?.CommissionsReversed
                    }, Formatting.None),
                    DateLogged = DateTime.UtcNow
                });
                authDb.SaveChanges();
            }
            catch
            {
                // Swallow: auditing is secondary to the (already-committed) money mutation.
            }
        }

        // Appends one audit row to the shared security ledger (tblH_Auth_Log) for a
        // test-credit attempt. Best-effort: the credit (when it happened) is already
        // durably recorded in the tblH_User_Balance ledger, so an audit-write failure
        // must surface but never mask or undo the primary outcome.
        void auditTestCredit(string adminId, string userId, decimal amount, string reference, string result)
        {
            try
            {
                authDb.tblH_Auth_Log.Add(new tblH_Auth_Log
                {
                    LogID = Guid.NewGuid().ToString(),
                    EventType = TestCreditEventType,
                    Subject = adminId,            // who (acting root-admin)
                    SubjectType = "admin",
                    RefreshTokenID = null,
                    RotationChainRoot = null,
                    SourceIP = WcfSourceIp.TryGet(),
                    Details = JsonConvert.SerializeObject(new
                    {
                        targetUserId = userId,    // target
                        amount = amount,          // amount
                        reference = reference,
                        result = result           // outcome (credited / *_refused / ...)
                    }, Formatting.None),
                    DateLogged = DateTime.UtcNow  // when
                });
                authDb.SaveChanges();
            }
            catch
            {
                // Swallow: auditing is secondary to the (already-committed) credit and
                // must not turn a successful mint into a thrown error for the caller.
            }
        }

        public OutputModel Login(tblM_Admin x)
        {
            try
            {
                var data = db.tblM_Admin.Where(p => p.Email == x.Email).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    DateTime now = DateTime.Now;
                    if (QryptoCard.INT.Security.PasswordLockout.IsLockedOut(db.Database, "tblM_Admin", "AdminID", data.AdminID, now))
                    {
                        // Option B: a locked account is indistinguishable from an ordinary wrong password.
                        op.Status = "failed";
                        op.Message = "Your password is incorrect";
                        return op;
                    }

                    if (!QryptoCard.INT.Security.PasswordHasher.Verify(x.Password, data.Password))
                    {
                        QryptoCard.INT.Security.PasswordLockout.RecordFailure(db.Database, "tblM_Admin", "AdminID", data.AdminID, now);
                        op.Status = "failed";
                        op.Message = "Your password is incorrect";
                        return op;
                    }

                    QryptoCard.INT.Security.PasswordLockout.RecordSuccess(db.Database, "tblM_Admin", "AdminID", data.AdminID);

                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    tblH_Admin_Login a = new tblH_Admin_Login();
                    a.ID = Guid.NewGuid().ToString();
                    a.AdminID = data.AdminID;

                    //Random r = new Random();
                    //var z = r.Next(0, 1000000);
                    //string s = String.Empty;
                    //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                    //    s = "000000";
                    //else
                    //    s = z.ToString("000000");

                    var code = Common.getOTPCode();
                    a.Code = QryptoCard.Sec.OtpCodes.Hash(code);
                    a.DateCreated = DateTime.Now;
                    a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                    a.isVerify = 0;
                    db.tblH_Admin_Login.Add(a);
                    db.SaveChanges();
                    NotificationMailkitService.sendEmailOTP(data.Email, data.FirstName + " " + data.LastName, code);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                    op.Data = a.ID;
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public void viewAdmin(vw_Admin x)
        { return; }


        public OutputModel LoginVerify(tblH_Admin_Login x)
        {
            try
            {
                var data = db.tblH_Admin_Login.Where(p => p.ID == x.ID && p.isVerify == 0).FirstOrDefault();

                if (data == null || !QryptoCard.Sec.OtpCodes.Verify(x.Code, data.Code) || QryptoCard.Sec.OtpCodes.IsExpired(data.DateExpired, DateTime.Now))
                {
                    // Count a wrong-code guess against this live session and lock it (isVerify=-1)
                    // at the threshold, so a locked session reads as "not found" on the next try.
                    if (data != null && !QryptoCard.Sec.OtpCodes.Verify(x.Code, data.Code))
                        QryptoCard.INT.Security.OtpLockout.RecordFailure(db.Database, "tblH_Admin_Login", "ID", data.ID);
                    op.Status = "failed";
                    op.Message = "Your session is ended";
                    return op;
                }
                else
                {
                    data.isVerify = 1;
                    data.Param1 = DateTime.Now.ToString();
                    db.SaveChanges();
                    var adm = db.vw_Admin.Where(p => p.AdminID == data.AdminID).FirstOrDefault();
                    op.Status = "success";
                    op.Message = "Success Get Admin";
                    op.Data = JsonConvert.SerializeObject(adm, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel regenerateOTP(tblH_Admin_Login x)
        {
            try
            {
                var data = db.tblH_Admin_Login.Where(p => p.ID == x.ID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your session is ended";
                    return op;
                }
                else
                {
                    // Invalidate any still-pending OTP rows for this admin before issuing a new one,
                    // so a resend supersedes rather than accumulates live codes.
                    foreach (var stale in db.tblH_Admin_Login.Where(p => p.AdminID == data.AdminID && p.isVerify == 0).ToList())
                        stale.isVerify = 1;

                    tblH_Admin_Login a = new tblH_Admin_Login();
                    a.ID = Guid.NewGuid().ToString();
                    a.AdminID = data.AdminID;

                    //Random r = new Random();
                    //var z = r.Next(0, 1000000);
                    //string s = String.Empty;
                    //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                    //    s = "000000";
                    //else
                    //    s = z.ToString("000000");

                    var code = Common.getOTPCode();
                    a.Code = QryptoCard.Sec.OtpCodes.Hash(code);
                    a.DateCreated = DateTime.Now;
                    a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                    a.isVerify = 0;
                    db.tblH_Admin_Login.Add(a);
                    db.SaveChanges();
                    var adm = db.tblM_Admin.Where(p => p.AdminID == a.AdminID).FirstOrDefault();
                    NotificationMailkitService.sendEmailOTP(adm.Email, adm.FirstName + " " + adm.LastName, code);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                    op.Data = a.ID;
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel forgotPassword(tblM_Admin x)
        {
            try
            {
                var data = db.tblM_Admin.Where(p => p.Email == x.Email).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    var q = new tblT_Admin_ForgotPassword();
                    q.AdminID = data.AdminID;
                    q.Hash = Guid.NewGuid().ToString();
                    q.isVerified = 0;
                    q.isActive = 1;
                    q.DateCreated = DateTime.Now;
                    db.tblT_Admin_ForgotPassword.Add(q);
                    db.SaveChanges();

                    var hash = Secure.Base64Encode(q.Hash);

                    NotificationMailkitService.sendEmailPasswordAdmin(data.Email, data.FirstName + " " + data.LastName, hash);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel checkForgotPassword(tblT_Admin_ForgotPassword x)
        {
            try
            {
                var data = db.tblT_Admin_ForgotPassword.Where(p => p.Hash == x.Hash).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.isVerified == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your session has been completed";
                        return op;
                    }

                    //var b = db.tblM_Admin.Where(p => p.AdminID == data.AdminID).FirstOrDefault();

                    op.Status = "success";
                    op.Message = "Success validate session";
                    //op.Data = JsonConvert.SerializeObject(b, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel changePassword(tblT_Admin_ForgotPassword x)
        {
            try
            {
                var data = db.tblT_Admin_ForgotPassword.Where(p => p.Hash == x.Hash).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.isVerified == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your session has been completed";
                        return op;
                    }

                    var b = db.tblM_Admin.Where(p => p.AdminID == data.AdminID).FirstOrDefault();
                    b.Password = QryptoCard.INT.Security.PasswordHasher.Hash(x.Param1);
                    data.isVerified = 1;

                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success change password";
                    //op.Data = JsonConvert.SerializeObject(b, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getAdminFilter(string em, AdminFilterModel fil)
        {
            try
            {
                string uid = getAdminId(em);
                string role = getRole(em);

                if (isDeniedAdminManage(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorize to run this endpoint";
                    return op;
                }

                var data = db.vw_Admin.Where(p => p.isActive == 1 && p.isBanned == 0).OrderBy(p => p.DateJoin).ToList();
                if (fil.isVerified)
                    data = data.Where(p => p.isVerified == 1).ToList();
                if (fil.Role != "all")
                {
                    data = data.Where(p => p.Role == fil.Role).ToList();
                }

                op.Status = "success";
                op.Message = "Success get admin list";
                op.Data = JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getAdminDetail(string em, tblM_Admin fil)
        {
            try
            {
                // Allow-list role gate: only Owner/Admin may view another admin's detail.
                if (isDeniedAdminManage(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorize to run this endpoint";
                    return op;
                }

                var data = db.vw_Admin.Where(p => p.AdminID == fil.AdminID).FirstOrDefault();
                if (data != null)
                {
                    op.Status = "success";
                    op.Message = "Success get admin detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
                else
                {
                    op.Status = "failed";
                    op.Message = "Failed get admin detail";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel addAdmin(string em, tblM_Admin x)
        {
            try
            {
                string uid = getAdminId(em);
                string role = getRole(em);

                if (isDeniedAdminManage(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorize to run this endpoint";
                    return op;
                }

                var data = db.tblM_Admin.Where(p => p.Email == x.Email && p.isActive == 1).FirstOrDefault();
                if (data != null)
                {
                    op.Status = "failed";
                    op.Message = "Email address registered. Please choose another email address.";
                    return op;
                }

                var rl = db.tblM_Admin_Role.Where(p => p.Role == x.Password).FirstOrDefault();
                x.RoleID = rl.RoleID;

                x.DateInvited = DateTime.Now;
                x.InvitedBy = uid;
                x.AdminID = Guid.NewGuid().ToString();
                x.isBanned = 0;
                x.isVerified = 0;
                x.isActive = 1;

                db.tblM_Admin.Add(x);
                db.SaveChanges();

                var id = x.AdminID;
                var b = db.vw_Admin.Where(p => p.AdminID == id && p.isActive == 1).FirstOrDefault();

                var hash = Secure.Base64Encode(x.AdminID);
                var url = KeyModel.QRYPTO_URL_ADMIN_INVITE + hash;
                NotificationMailkitService.sendEmailAdminInvitation(b.Email, b.InvitedByFirstName + " " + b.InvitedByLastName, x.FirstName + " " + x.LastName, url);

                op.Status = "success";
                op.Message = "Success invite admin. Please ask admin to check email to verify his/her account.";
                //op.Data = JsonConvert.SerializeObject(data, Formatting.None);
            }
            //catch (DbEntityValidationException e)
            //{
            //    string str = "";
            //    foreach (var eve in e.EntityValidationErrors)
            //    {
            //        Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
            //            eve.Entry.Entity.GetType().Name, eve.Entry.State);
            //        foreach (var ve in eve.ValidationErrors)
            //        {
            //            Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
            //                ve.PropertyName, ve.ErrorMessage);
            //        }
            //    }
            //    op.Data = null;
            //    op.Status = "error";
            //    throw;
            //}
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getInvitedAdmin(tblM_Admin x)
        {
            try
            {
                var data = db.vw_Admin.Where(p => p.AdminID == x.AdminID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your session is not found";
                    return op;
                }
                else
                {
                    if (data.isVerified == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your admin has been completed. Please go to login page.";
                        return op;
                    }


                    op.Status = "success";
                    op.Message = "Success get admin detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateInvitedAdmin(tblM_Admin x)
        {
            try
            {
                var data = db.tblM_Admin.Where(p => p.AdminID == x.AdminID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your session is not found";
                    return op;
                }
                else
                {
                    if (data.isVerified == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your admin has been completed. Please go to login page.";
                        return op;
                    }

                    data.Password = QryptoCard.INT.Security.PasswordHasher.Hash(x.Password);
                    data.Phone = x.Phone;
                    data.isActive = 1;
                    data.isVerified = 1;
                    data.isBanned = 0;
                    data.DateVerified = DateTime.Now;
                    data.DateJoin = data.DateVerified;
                    db.SaveChanges();


                    op.Status = "success";
                    op.Message = "Onboarding completed. Please login.";
                    //op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel banAdmin(string em, tblM_Admin x)
        {
            try
            {
                string uid = getAdminId(em);
                string role = getRole(em);

                if (isDeniedAdminManage(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorize to run this endpoint";
                    return op;
                }

                var data = db.tblM_Admin.Where(p => p.AdminID == x.AdminID).FirstOrDefault();
                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Admin ID is not found.";
                    return op;
                }

                if (data.isBanned == 1)
                {
                    op.Status = "failed";
                    op.Message = "This admin already banned.";
                    return op;
                }

                // Bug fix: mutate the loaded entity (data), not the inbound wire object (x),
                // and actually ban (isBanned = 1) rather than clearing the flag.
                data.isBanned = 1;
                data.BannedBy = uid;
                data.DateBanned = DateTime.Now;
                db.SaveChanges();

                op.Status = "success";
                op.Message = "Success banned admin";
                //op.Data = JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getAdminData(string em, string x)
        {
            try
            {
                // IDOR fix: only ever return the authenticated caller's own admin record
                // (AdminID derived from em); the client-supplied id is ignored so a caller
                // cannot read another admin's data.
                string uid = getAdminId(em);
                var data = db.vw_Admin.Where(p => p.AdminID == uid).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                    op.Message = "Success get data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateAdminData(string em, tblM_Admin x)
        {
            try
            {
                // IDOR fix: the record updated is the authenticated caller's own (AdminID
                // from em), never the client-supplied x.AdminID.
                string uid = getAdminId(em);
                var data = db.tblM_Admin.Where(p => p.AdminID == uid).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    data.FirstName = x.FirstName;
                    data.LastName = x.LastName;
                    data.Phone = x.Phone;
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success update data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updatePassword(string em, PasswordChangeModel x)
        {
            try
            {
                // IDOR fix: the password changed is the authenticated caller's own (AdminID
                // from em), never the client-supplied x.AdminID.
                string uid = getAdminId(em);
                var data = db.tblM_Admin.Where(p => p.AdminID == uid).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (!QryptoCard.INT.Security.PasswordHasher.Verify(x.CurrentPassword, data.Password))
                    {
                        op.Status = "failed";
                        op.Message = "Your current password is wrong";
                        return op;
                    }
                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    data.Password = QryptoCard.INT.Security.PasswordHasher.Hash(x.Password);
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success update data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateEmailOTP(string em, tblM_Admin x)
        {
            try
            {
                // IDOR fix: the change-email OTP is always issued for the authenticated
                // caller (AdminID from em), never the client-supplied x.AdminID.
                string uid = getAdminId(em);

                var q = db.tblM_Admin.Where(p => p.Email == x.Email && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();

                if (q != null)
                {
                    op.Status = "failed";
                    op.Message = "Email address was registered. Please input another email address.";
                    return op;
                }
                else
                {
                    var data = db.tblM_Admin.Where(p => p.AdminID == uid).FirstOrDefault();
                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    tblH_Admin_OTP a = new tblH_Admin_OTP();
                    a.OTPID = Guid.NewGuid().ToString();
                    a.AdminID = uid;
                    a.Name = "Change Email";

                    //Random r = new Random();
                    //var z = r.Next(0, 1000000);
                    //string s = String.Empty;
                    //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                    //    s = "000000";
                    //else
                    //    s = z.ToString("000000");

                    var code = Common.getOTPCode();
                    a.Code = QryptoCard.Sec.OtpCodes.Hash(code);
                    a.DateCreated = DateTime.Now;
                    a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                    a.isVerify = 0;
                    db.tblH_Admin_OTP.Add(a);
                    db.SaveChanges();

                    var u = db.tblM_Admin.Where(p => p.AdminID == a.AdminID).FirstOrDefault();
                    NotificationMailkitService.sendEmailOTP(x.Email, u.FirstName + " " + u.LastName, code);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                    op.Data = a.OTPID;

                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateEmail(string em, tblH_Admin_OTP x)
        {
            try
            {
                string uid = getAdminId(em);

                var otp = db.tblH_Admin_OTP.Where(p => p.isVerify == 0 && p.AdminID == uid && p.OTPID == x.OTPID).OrderByDescending(p => p.DateCreated).FirstOrDefault();
                if (otp == null)
                {
                    op.Status = "failed";
                    op.Message = "OTP Session is not found. Please re-click 'generate key' button";
                    return op;
                }
                else
                {
                    if (!QryptoCard.Sec.OtpCodes.Verify(x.Code, otp.Code) || QryptoCard.Sec.OtpCodes.IsExpired(otp.DateExpired, DateTime.Now))
                    {
                        // Count a wrong-code guess and lock the session (isVerify=-1) at the threshold.
                        if (!QryptoCard.Sec.OtpCodes.Verify(x.Code, otp.Code))
                            QryptoCard.INT.Security.OtpLockout.RecordFailure(db.Database, "tblH_Admin_OTP", "OTPID", otp.OTPID);
                        op.Status = "failed";
                        op.Message = "Your OTP is wrong";
                        return op;
                    }
                    else
                    {
                        otp.isVerify = 1;
                        db.SaveChanges();

                        var data = db.tblM_Admin.Where(p => p.AdminID == uid).FirstOrDefault();
                        data.Email = x.MerchantID;
                        db.SaveChanges();

                        op.Status = "success";
                        op.Message = "Success update email address";
                    }
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }
    }
}
