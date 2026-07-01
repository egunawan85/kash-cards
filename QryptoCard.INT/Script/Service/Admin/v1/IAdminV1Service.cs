using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IAdminV1Service" in both code and config file together.
    [ServiceContract]
    public interface IAdminV1Service
    {
        [OperationContract]
        OutputModel Login(tblM_Admin x);
        [OperationContract]
        OutputModel LoginVerify(tblH_Admin_Login x);
        [OperationContract]
        OutputModel regenerateOTP(tblH_Admin_Login x);
        [OperationContract]
        void viewAdmin(vw_Admin x);

        [OperationContract]
        OutputModel forgotPassword(tblM_Admin x);
        [OperationContract]
        OutputModel checkForgotPassword(tblT_Admin_ForgotPassword x);
        [OperationContract]
        OutputModel changePassword(tblT_Admin_ForgotPassword x);
        [OperationContract]
        OutputModel getAdminFilter(string em, AdminFilterModel fil);
        [OperationContract]
        OutputModel getAdminDetail(string em, tblM_Admin fil);
        [OperationContract]
        OutputModel addAdmin(string em, tblM_Admin x);
        [OperationContract]
        OutputModel getInvitedAdmin(tblM_Admin x);
        [OperationContract]
        OutputModel updateInvitedAdmin(tblM_Admin x);
        [OperationContract]
        OutputModel banAdmin(string em, tblM_Admin x);

        // Dev-only test-credit tool (SD-2): credits a user's wallet via the verified
        // CreditDeposit path, walled by an environment hard-gate + root-admin-only +
        // audit log. Primitive parameters (no new DataContract) keep the proxy edit
        // minimal; the env gate inside the implementation is the load-bearing wall.
        [OperationContract]
        OutputModel devCreditWallet(string em, string userId, decimal amount, string reference);

        // Admin-only card refund: cancels the whole WasabiCard and returns its unused balance to the
        // buyer's wallet, clawing back any referral commission. Root-admin-only + audit-logged (no env
        // gate — refunds must work in prod). Primitive param (the order id) keeps the proxy edit minimal.
        [OperationContract]
        OutputModel refundCard(string em, string orderId);

        // Deposit-into-card money-trail reconcile (READ-ONLY ops visibility): reads
        // vw_Card_Funding_Reconcile (one stitched row per intent) with server-side, fully
        // parameterized filters. No money moves, nothing mutates. Owner/Admin only. Primitive
        // params (dates as ISO strings) keep the proxy edit minimal — no new DataContract.
        [OperationContract]
        OutputModel getFundingReconcile(string em, string status, string userId, string intentId,
            bool overpaidOnly, int stuckMinutes, string fromDate, string toDate, int top);




        [OperationContract]
        OutputModel getAdminData(string em, string x);

        [OperationContract]
        OutputModel updateAdminData(string em, tblM_Admin x);
        [OperationContract]
        OutputModel updatePassword(string em, PasswordChangeModel x);
        [OperationContract]
        OutputModel updateEmailOTP(string em, tblM_Admin x);
        [OperationContract]
        OutputModel updateEmail(string em, tblH_Admin_OTP x);
    }
}
