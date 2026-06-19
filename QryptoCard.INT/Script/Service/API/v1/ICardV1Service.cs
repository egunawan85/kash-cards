using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using QryptoCard.INT.Model.Service;

namespace QryptoCard.INT.Script.Service.API.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICardV1Service" in both code and config file together.
    [ServiceContract]
    public interface ICardV1Service
    {
        [OperationContract]
        OutputModel CardType(string em, tblM_Card_Type x);
        [OperationContract]
        OutputModel getCardTypeById(string em, tblM_Card_Type x);
        [OperationContract]
        OutputModel openCard(string em, tblT_Card x);
        [OperationContract]
        OutputModel getCardListActive(string em, tblT_Card x);
        [OperationContract]
        OutputModel getCardListAll(string em, tblT_Card x);
        [OperationContract]
        OutputModel getCardDetail(string em, tblT_Card x);
        [OperationContract]
        OutputModel getCardBalance(string em, CardBalanceModel x);
        [OperationContract]
        OutputModel getCardTransaction(string em, tblT_Card x);
        [OperationContract]
        OutputModel getCardTransactionDetail(string em, tblT_Card_Transaction x);
        [OperationContract]
        OutputModel depositCard(string em, tblT_Card_Deposit x);
        [OperationContract]
        OutputModel getCardDepositDetail(string em, tblT_Card_Deposit x);
        [OperationContract]
        OutputModel getCardDepositList(string em, tblT_Card x);
        [OperationContract]
        OutputModel cancelCardPurchase(string em, tblT_Card x);
        [OperationContract]
        OutputModel cancelDepositTransaction(string em, tblT_Card_Deposit x);
        [OperationContract]
        OutputModel getCardPurchaseDetail(string em, tblT_Card x);
    }
}
