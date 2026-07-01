using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Models;
using System;

namespace QryptoCard.Dashboard.Services
{
    // Every call routes through AuthClient (Bearer attach + silent refresh on 401).
    public class CardService
    {
        OutputModel op = new OutputModel();

        public OutputModel getCardTypes()
        {
            try
            {
                string path = "/v1/card/type";
                return op = AuthClient.ExecuteJsonGet(path);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getCardTypesByID(CardTypeModel x)
        {
            try
            {
                string path = "/v1/card/type/id";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getHolderDetail(CardholderModel x)
        {
            try
            {
                string path = "/v1/card/holder/detail";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel checkHolder(CardholderModel x)
        {
            try
            {
                string path = "/v1/card/holder/check/cardtypeid";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getCardList(CardModel x)
        {
            try
            {
                string path = "/v1/card/list";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getCardListAll(CardModel x)
        {
            try
            {
                string path = "/v1/card/list/all";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getCardDetail(CardModel x)
        {
            try
            {
                string path = "/v1/card/detail/id";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel openCard(CardModel x)
        {
            try
            {
                string path = "/v1/card/open";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel cancelCardTransaction(CardModel x)
        {
            try
            {
                string path = "/v1/card/trx/cancel";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel depositCard(CardDepositModel x)
        {
            try
            {
                string path = "/v1/card/deposit";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel depositCardList(CardDepositModel x)
        {
            try
            {
                string path = "/v1/card/deposit/list";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel depositCardDetail(CardDepositModel x)
        {
            try
            {
                string path = "/v1/card/deposit/detail";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel cancelDeposit(CardModel x)
        {
            try
            {
                string path = "/v1/card/deposit/cancel";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel trxCardList(CardModel x)
        {
            try
            {
                string path = "/v1/card/trx/list";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel trxCardDetail(CardTransactionModel x)
        {
            try
            {
                string path = "/v1/card/trx/detail";
                return op = AuthClient.ExecuteJsonPost(path, x);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        // ---- Deposit-into-card: funding-intent lifecycle -------------------------
        // Every call is user-scoped server-side (em = the bearer identity); the app never trusts a
        // client-supplied user. All no-op while CardFundingStreamingEnabled is OFF.

        public OutputModel createFundingIntent(long cardTypeId, decimal amount)
        {
            try
            {
                string path = "/v1/card/funding/intent";
                return op = AuthClient.ExecuteJsonPost(path, new { cardTypeId = cardTypeId, amount = amount });
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel createFundingTopUp(string cardNo, decimal amount)
        {
            try
            {
                string path = "/v1/card/funding/topup";
                return op = AuthClient.ExecuteJsonPost(path, new { cardNo = cardNo, amount = amount });
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getFundingIntentStatus(string intentId)
        {
            try
            {
                string path = "/v1/card/funding/status";
                return op = AuthClient.ExecuteJsonPost(path, new { intentId = intentId });
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel cancelFundingIntent(string intentId)
        {
            try
            {
                string path = "/v1/card/funding/cancel";
                return op = AuthClient.ExecuteJsonPost(path, new { intentId = intentId });
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getFundingOpenIntents()
        {
            try
            {
                string path = "/v1/card/funding/list";
                return op = AuthClient.ExecuteJsonPost(path, new { });
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }
    }
}
