using QryptoCard.INT.Callback.Model.PGCrypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Callback.Service.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICallbackV1Service" in both code and config file together.
    [ServiceContract]
    public interface ICallbackV1Service
    {
        [OperationContract]
        void Wasabi(string cat, string sign, string req, string a);
        [OperationContract]
        void PGCrypto(PGCryptoModel x);


        [OperationContract]
        void recreateCallback();

        [OperationContract]
        void reTopup(string tid);
    }
}
