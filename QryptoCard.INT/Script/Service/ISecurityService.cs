using System.ServiceModel;

namespace QryptoCard.INT.Script.Service
{
    // Credential-validation contract for the API tiers. The previous public
    // crypto helpers (base64*/encrypt*/decrypt*/dbtoapp/apptodb/signRSA/
    // decryptRSA/getwb/getcity) were unauthenticated decryption/signing oracles
    // with no callers and have been removed; only the three validators the API
    // edges actually use remain.
    [ServiceContract]
    public interface ISecurityService
    {
        [OperationContract]
        bool validateUser(string email, string passw);
        [OperationContract]
        bool validateAPI(string api, string sec);
        [OperationContract]
        bool validateAdmin(string email, string passw);
    }
}
