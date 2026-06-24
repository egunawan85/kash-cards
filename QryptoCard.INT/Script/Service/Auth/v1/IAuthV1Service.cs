using QryptoCard.INT.Model.Service;
using System.ServiceModel;

namespace QryptoCard.INT.Script.Service.Auth.v1
{
    // Auth-token WCF contract (opaque-random Bearer tokens, Runegate parity).
    //
    //   - mintAfterOtpVerify requires an OTP code on top of the existing
    //     Login() password verification, so any localhost caller reaching the
    //     service cannot skip 2FA. (There is deliberately no mint() without OTP.)
    //   - revokeAllForSubject takes a serviceToken compared constant-time
    //     against SecretsConfig "AUTH_SERVICE_REVOKE_TOKEN". Without the secret,
    //     any localhost-reachable caller could mass-logout every user.
    //
    // Wire shape mirrors the existing AdminV1Service / UserV1Service: every
    // operation returns OutputModel { Status, Message, Data }.
    //
    // Operations:
    //   mintAfterOtpVerify      Login (password) -> mintAfterOtpVerify (OTP) -> tokens
    //   refresh                 (refresh) -> fresh (access, refresh), RTR with reuse detection
    //   verify                  (access) -> AuthVerifyResponse, indexed lookup
    //   revoke                  (refresh) -> kills entire rotation chain (idempotent), audit-logged
    //   revokeAllForSubject     ban / password-change -> kills every chain for the subject, audit-logged
    [ServiceContract]
    public interface IAuthV1Service
    {
        [OperationContract]
        OutputModel mintAfterOtpVerify(string otpSessionId, string otpCode, string subjectType);

        [OperationContract]
        OutputModel refresh(string refreshToken);

        [OperationContract]
        OutputModel verify(string accessToken);

        [OperationContract]
        OutputModel revoke(string refreshToken);

        [OperationContract]
        OutputModel revokeAllForSubject(string subject, string subjectType, string serviceToken);
    }
}
