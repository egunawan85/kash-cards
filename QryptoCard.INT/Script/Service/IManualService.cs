using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IManualService" in both code and config file together.
    [ServiceContract]
    public interface IManualService
    {
        [OperationContract]
        void doCommissionCard();
        [OperationContract]
        void doCommissionDeposit();

        [OperationContract]
        void checkBalance();

        [OperationContract]
        void cancelCard(string cardno);

        [OperationContract]
        void generateAPI(string userid);
    }
}
