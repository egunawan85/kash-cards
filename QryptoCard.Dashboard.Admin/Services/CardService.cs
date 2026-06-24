using Newtonsoft.Json;
using QryptoCard.Dashboard.Admin.Models;
using QryptoCard.Dashboard.Admin.Models.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Services
{
    public class CardService
    {
        OutputModel op = new OutputModel();

        public OutputModel getCardType()
        {
            try
            {
                Common.trustConnection();
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

        public OutputModel getCardTypeDetail(CardTypeModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type/id";
                return op = AuthClient.ExecuteJsonPost(path, adm);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updateCardPrice(CardTypeModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type/price";
                return op = AuthClient.ExecuteJsonPut(path, adm);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updateCardDepositFee(CardTypeModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type/deposit/fee";
                return op = AuthClient.ExecuteJsonPut(path, adm);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getActiveCards()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/active";
                return op = AuthClient.ExecuteJsonGet(path);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getAllCards()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/all";
                return op = AuthClient.ExecuteJsonGet(path);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getCardTtransaction(CardFilterModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/transaction";
                return op = AuthClient.ExecuteJsonPost(path, adm);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getDepositTtransaction(DepositFilterModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/deposit/transaction";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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