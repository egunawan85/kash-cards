using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.App.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICardV1Service" in both code and config file together.
    [ServiceContract]
    public interface ICardV1Service
    {
        [OperationContract]
        string getDate();




        [OperationContract]
        OutputModel CardType(tblM_Card_Type x);
        [OperationContract]
        OutputModel getCardTypeById(tblM_Card_Type x);
        [OperationContract]
        OutputModel getHolderDetail(string em, tblM_Cardholder x);
        [OperationContract]
        OutputModel checkHolderByCardTypeId(string em, tblM_Cardholder x);
        [OperationContract]
        OutputModel openCard(string em, tblT_Card x);
        [OperationContract]
        OutputModel getCardList(string em, tblT_Card x);
        [OperationContract]
        OutputModel getCardListAll(string em, tblT_Card x);
        [OperationContract]
        OutputModel getCardDetail(string em, vw_Card x);
        [OperationContract]
        OutputModel getCardBalance(string em, tblT_Card x);
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
        OutputModel cancelCardTransaction(string em, vw_Card x);
        [OperationContract]
        OutputModel cancelDepositTransaction(string em, tblT_Card_Deposit x);



        [OperationContract]
        void createCardHolder(string em, long cardtypeid, string fn, string ln, string holderEmail);
        [OperationContract]
        void recreateCardHolder(string em, int holderid);





        [OperationContract]
        void checkCard(string em, string cardNo);

        // Deposit-into-card: funding-intent lifecycle (gated by CardFundingStreamingEnabled).
        [OperationContract]
        OutputModel createCardFundingIntent(string em, long cardTypeId, decimal amount);
        [OperationContract]
        OutputModel createCardFundingTopUp(string em, string cardNo, decimal amount);
        [OperationContract]
        OutputModel getCardFundingIntentStatus(string em, string intentId);
        [OperationContract]
        OutputModel cancelCardFundingIntent(string em, string intentId);
        [OperationContract]
        OutputModel getCardFundingOpenIntents(string em);

        // Scheduled issuance tick (issues/tops-up cards for intents whose funds landed at WasabiCard,
        // plus the pending-intent expiry sweep). NOT user-facing — no `em`, and the app API exposes it
        // ONLY behind a scheduler-authed loopback route. Returns a compact JSON summary.
        [OperationContract]
        string RunCardFundingIssuance();
    }
}
