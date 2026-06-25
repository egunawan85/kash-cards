using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.App.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IUserV1Service" in both code and config file together.
    [ServiceContract]
    public interface IUserV1Service
    {
        [OperationContract]
        OutputModel getDashboardData(string em, DashboardModel a);


        [OperationContract]
        OutputModel Register(tblM_User x);
        [OperationContract]
        OutputModel RegisterVerify(tblH_User_Register x);
        [OperationContract]
        OutputModel regenerateOTPRegister(tblH_User_Register x);
        [OperationContract]
        OutputModel Login(tblM_User x);
        [OperationContract]
        OutputModel LoginVerify(tblH_User_Login x);
        [OperationContract]
        OutputModel regenerateOTP(tblH_User_Login x);
        [OperationContract]
        OutputModel forgotPassword(tblM_User x);
        [OperationContract]
        OutputModel checkForgotPassword(tblT_User_ForgotPassword x);
        [OperationContract]
        OutputModel changePassword(tblT_User_ForgotPassword x);
        [OperationContract]
        OutputModel getUserData(string em, string x);
        [OperationContract]
        OutputModel updateUserData(string em, tblM_User x);
        [OperationContract]
        OutputModel updatePassword(string em, PasswordChangeModel x);
        [OperationContract]
        OutputModel updateEmailOTP(string em, tblM_User x);
        [OperationContract]
        OutputModel updateEmail(string em, tblH_User_OTP x);
        [OperationContract]
        OutputModel enable2FA(string em, tblM_User_2FA x);
        [OperationContract]
        OutputModel get2FA(string em);
        [OperationContract]
        OutputModel getReferralCode(string em, tblM_User_Referral x);
        [OperationContract]
        OutputModel getBalance(string em, tblM_User_Balance x);
        [OperationContract]
        OutputModel getDepositAddress(string em);
        [OperationContract]
        OutputModel getLedger(string em, int page, int pageSize);





        [OperationContract]
        OutputModel generateKeyOTP(string em);
        [OperationContract]
        OutputModel validateKeyOTP(string em, tblH_User_OTP x);

        [OperationContract]
        OutputModel getReferralJoined(string em);
    }
}
