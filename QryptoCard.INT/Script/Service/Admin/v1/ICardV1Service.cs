using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICardV1Service" in both code and config file together.
    [ServiceContract]
    public interface ICardV1Service
    {
        [OperationContract]
        OutputModel CardType(tblM_Card_Type x);
        [OperationContract]
        OutputModel getCardTypeById(tblM_Card_Type x);
        [OperationContract]
        OutputModel getActiveCard(string em);
        [OperationContract]
        OutputModel getCardListAll(string em, vw_Card x);
        [OperationContract]
        OutputModel getCardPurchaseFilter(string em, CardFilterModel fil);
        [OperationContract]
        OutputModel getDepositTrxFilter(string em, DepositFilterModel fil);

        [OperationContract]
        OutputModel updateCardPrice(string em, tblM_Card_Type x);
        [OperationContract]
        OutputModel updateCardDepositFee(string em, tblM_Card_Type x);

        // Global card pricing (tblM_Setting: CardPrice + CardDepositFeeRate).
        [OperationContract]
        OutputModel getCardPricing(string em);
        [OperationContract]
        OutputModel updateCardPricing(string em, tblM_Card_Type x);
    }
}
