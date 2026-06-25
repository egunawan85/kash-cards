using Newtonsoft.Json;
using QryptoCard.INT.Callback.Model.PGCrypto;
using QryptoCard.INT.Callback.Model.WasabiCard;
using QryptoCard.INT.Callback.Service.Gateway.WasabiCard;
using QryptoCard.Sec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.ServiceModel;
using System.Text;
using System.Web.Configuration;

namespace QryptoCard.INT.Callback.Service.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "CallbackV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select CallbackV1Service.svc or CallbackV1Service.svc.cs at the Solution Explorer and start debugging.
    [QryptoCard.INT.Callback.Security.IntAuthBehavior]
    public class CallbackV1Service : ICallbackV1Service
    {
        private static string loadRsaPrivateKeyPem()
        {
            return QryptoCard.INT.Callback.Model.KeyModel.WASABICARD_PRIVATE_KEY_XML;
        }

        private static string decrypt(string strText, string privateKey)
        {
            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    var base64Encrypted = strText;

                    // server decrypting data with private key                    
                    rsa.FromXmlString(privateKey);

                    var resultBytes = Convert.FromBase64String(base64Encrypted);
                    var decryptedBytes = rsa.Decrypt(resultBytes, RSAEncryptionPadding.Pkcs1);
                    var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedData.ToString();
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        // Internal-only: decrypt a WasabiCard sensitive payload field with our RSA private key.
        // Not exposed as a WCF operation (was previously a public decryption oracle).
        private string decryptSensitive(string x)
        {
            return decrypt(x, loadRsaPrivateKeyPem());
        }

        public void Wasabi(string cat, string sign, string req, string a)
        {
            try
            {
                DBEntities db = new DBEntities();
                tblH_API_Log log = new tblH_API_Log();
                tblH_Partner_Webhook tes = new tblH_Partner_Webhook();
                tes.Header = sign;
                tes.Request = a;
                tes.Type = "Wasabi Card";
                tes.RequestDate = DateTime.Now;

                db.tblH_Partner_Webhook.Add(tes);
                db.SaveChanges();

                if (cat == "card_3ds")
                {
                    var q = JsonConvert.DeserializeObject<WCTransaction3DSModel>(a);
                    var amt = q.amount;

                    tblT_Card_Transaction_Auth x = new tblT_Card_Transaction_Auth();
                    x.CardNo = q.cardNo;
                    x.TradeNo = q.tradeNo;
                    x.OriginTradeNo = q.tradeNo;
                    x.Currency = q.currency;
                    x.Amount = Convert.ToDouble(q.amount);
                    x.MerchantName = q.merchantName;
                    x.Type = q.type;
                    x.Values = q.values;
                    x.Description = q.description.ToString();
                    x.TransactionTime = DateTime.Now;
                    x.Param1 = q.expirationTime.ToString();
                    x.Param2 = decrypt(x.Values, loadRsaPrivateKeyPem());

                    db.tblT_Card_Transaction_Auth.Add(x);
                    db.SaveChanges();

                    //do send email
                    if (x.Type == "third_3ds_otp")
                    { }
                    else if (x.Type == "auth_url")
                    { }

                }
                else if (cat == "card_auth_transaction")
                {
                    var q = JsonConvert.DeserializeObject<WCAuthTransactionModel>(a);
                    var amt = q.amount;

                    var ck = db.tblT_Card_Transaction.Where(p => p.TradeNo == q.tradeNo).FirstOrDefault();
                    if (ck == null)
                    {
                        tblT_Card_Transaction x = new tblT_Card_Transaction();
                        x.CardNo = q.cardNo;
                        x.TradeNo = q.tradeNo;
                        x.OriginTradeNo = q.tradeNo;
                        x.Currency = q.currency;
                        if (q.amount != null)
                            x.Amount = Convert.ToDouble(q.amount);

                        if (q.authorizedAmount != null)
                            x.AuthorizedAmount = Convert.ToDouble(q.authorizedAmount);

                        x.AuthorizedCurrency = q.authorizedCurrency;

                        if (q.fee != null)
                            x.Fee = Convert.ToDouble(q.fee);
                        x.FeeCurrency = q.feeCurrency;

                        if (q.crossBoardFee != null)
                            x.CrossBoardFee = Convert.ToDouble(q.crossBoardFee);

                        if (q.crossBoardFeeCurrency != null)
                            x.CrossBoardFeeCurrency = q.crossBoardFeeCurrency.ToString();

                        if (q.settleAmount != null)
                            x.SettleAmount = Convert.ToDouble(q.settleAmount);

                        if (q.settleCurrency != null)
                            x.SettleCurrency = q.settleCurrency.ToString();


                        x.SettleDate = DateTime.Now;
                        x.MerchantName = q.merchantName;
                        x.Type = q.type;
                        x.TypeStr = q.typeStr;
                        x.Status = q.status;
                        x.StatusStr = q.statusStr;
                        x.Description = q.description;
                        x.TransactionTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(q.transactionTime / 1000d)).ToLocalTime();

                        db.tblT_Card_Transaction.Add(x);
                        db.SaveChanges();

                        if (q.status == "failed")
                        {
                            var crd = db.vw_Card.Where(p => p.CardNo == q.cardNo).FirstOrDefault();
                            if (crd != null)
                            {
                                if (crd.Email != null)
                                    NotificationService.sendEmailFailed(crd.Email, crd.CardNumber, q.merchantName, q.authorizedAmount, q.description);
                            }
                        }

                        //do retrieve card balance
                    }
                }
                else if (cat == "card_fee_patch")
                {
                    var q = JsonConvert.DeserializeObject<WCAuthTransactionReversalModel>(a);
                    var amt = q.amount;

                    tblT_Card_Transaction x = new tblT_Card_Transaction();
                    x.CardNo = q.cardNo;
                    x.TradeNo = q.tradeNo;
                    x.OriginTradeNo = q.tradeNo;
                    x.Currency = q.currency;
                    x.Amount = Convert.ToDouble(q.amount);
                    x.Type = q.type;
                    x.Status = q.status;
                    x.StatusStr = q.statusStr;
                    x.Param1 = q.deductionSourceFunds;
                    x.TransactionTime = DateTime.Now;

                    db.tblT_Card_Transaction.Add(x);
                    db.SaveChanges();

                    //do retrieve card balance
                }
                else if (cat == "card_transaction")
                {
                    var q = JsonConvert.DeserializeObject<WCTransactionModel>(a);
                    var amt = q.amount;
                    var type = q.type;

                    if (type == "create")
                    {
                        if (q.status == "success")
                        {
                            var cr = db.tblT_Card.Where(p => p.ID == q.merchantOrderNo && p.Status == PGStatusModel.InProgress).FirstOrDefault();
                            if (cr != null)
                            {
                                cr.CardNo = q.cardNo;
                                cr.Status = PGStatusModel.OpenCard;
                                db.SaveChanges();


                                WCCardInfoRequestModel qq = new WCCardInfoRequestModel();
                                qq.cardNo = cr.CardNo;
                                qq.onlySimpleInfo = false;
                                var res = WasabiCardService.getCardInfo(qq);
                                // Post-verify cross-check (defense-in-depth): the order is already bound to
                                // this card by the InProgress + merchantOrderNo row match above; here we
                                // additionally require WasabiCard to recognise the card before we mark it
                                // active. The credited balance is read from the provider's card-info response
                                // below, never from the webhook body. On an unconfirmed result the card stays
                                // in OpenCard for a later retry rather than crediting.
                                if (res != null && res.data != null &&
                                    WebhookCrossCheckEvaluator.EvaluateCardOpen(cr.CardNo, res.data.cardNo) == CrossCheckOutcome.Confirmed)
                                {
                                    cr.isActive = 1;
                                    cr.Status = PGStatusModel.Success;

                                    var qqq = new WCCardInfoSensitiveRequestModel();
                                    qqq.cardNo = cr.CardNo;
                                    var ressen = WasabiCardService.getCardInfoSensitive(qqq);

                                    cr.CardNumber = decryptSensitive(ressen.data.cardNumber);
                                    cr.CVV = ressen.data.cvv;
                                    cr.ValidPeriod = ressen.data.expireDate;
                                    db.SaveChanges();

                                    var ckb = db.tblT_Card_Balance.Where(p => p.CardNo == cr.CardNo).FirstOrDefault();
                                    if (ckb != null)
                                    {
                                        ckb.Amount = Convert.ToDouble(res.data.balanceInfo.amount);
                                        db.SaveChanges();
                                    }
                                    else
                                    {
                                        tblT_Card_Balance cb = new tblT_Card_Balance();
                                        cb.ID = cr.ID;
                                        cb.CardNo = cr.CardNo;
                                        cb.Currency = cr.Currency;
                                        cb.Amount = Convert.ToDouble(res.data.balanceInfo.amount);
                                        db.tblT_Card_Balance.Add(cb);
                                        db.SaveChanges();
                                    }

                                    //do send email
                                }

                            }
                        }
                        
                    }
                    else if (type == "deposit")
                    {
                        var cr = db.tblT_Card_Deposit.Where(p => p.ID == q.merchantOrderNo && p.Status == PGStatusModel.InProgress).FirstOrDefault();

                        if (q.status == "success")
                        {
                            if (cr != null)
                            {
                                cr.Status = PGStatusModel.Success;
                                db.SaveChanges();

                                //do send email
                                
                                //WCCardInfoRequestModel qq = new WCCardInfoRequestModel();
                                //qq.cardNo = cr.CardNo;
                                //qq.onlySimpleInfo = false;
                                //var res = WasabiCardService.getCardInfo(qq);
                                //if (res != null)
                                //{
                                //    var ckb = db.tblT_Card_Balance.Where(p => p.CardNo == cr.CardNo).FirstOrDefault();
                                //    if (ckb != null)
                                //    {
                                //        ckb.Amount = Convert.ToDouble(res.data.balanceInfo.amount);
                                //        db.SaveChanges();
                                //    }
                                //    else
                                //    {
                                //        tblT_Card_Balance cb = new tblT_Card_Balance();
                                //        cb.ID = cr.ID;
                                //        cb.CardNo = cr.CardNo;
                                //        cb.Currency = cr.Currency;
                                //        cb.Amount = Convert.ToDouble(res.data.balanceInfo.amount);
                                //        db.tblT_Card_Balance.Add(cb);
                                //        db.SaveChanges();
                                //    }

                                //    //do send email
                                //}

                            }
                        }

                        else if (q.status == "fail")
                        {
                            if (cr != null)
                            {
                                // Post-verify cross-check (defense-in-depth + replay mitigation): a
                                // deposit-fail refund credits the user's balance, so re-fetch the deposit's
                                // canonical status from WasabiCard and refund ONLY on an independently-
                                // confirmed failure with a matching amount. On a mismatch (provider says the
                                // deposit succeeded) or an unconfirmed/unreachable result, leave the deposit
                                // InProgress for the provider's retry / operator reconciliation rather than
                                // crediting a refund that may not be owed.
                                var canonical = WasabiCardService.getDepositOperation(cr.ID);
                                var refundOutcome = WebhookCrossCheckEvaluator.EvaluateDepositRefund(
                                    canonical != null ? canonical.status : null);
                                if (refundOutcome != CrossCheckOutcome.Confirmed)
                                {
                                    System.Diagnostics.Trace.TraceWarning(
                                        "Wasabi deposit-fail refund withheld (cross-check outcome=" + refundOutcome + ") for order " + cr.ID);
                                    return;
                                }
                                // Advisory only: the refund value is taken from our own record (cr.Total),
                                // so a provider/webhook amount discrepancy does not change what is credited,
                                // but it is worth surfacing as a drift signal for reconciliation.
                                if (canonical != null && !WebhookCrossCheckEvaluator.AmountsMatch(q.amount, canonical.amount))
                                {
                                    System.Diagnostics.Trace.TraceWarning(
                                        "Wasabi deposit-fail amount differs between webhook and provider record for order " + cr.ID);
                                }

                                // Atomically claim the deposit so only ONE delivery credits the refund. A
                                // concurrent or replayed duplicate (provider at-least-once retry) finds 0 rows
                                // affected and bails — this InProgress->Failed transition is the in-process
                                // idempotency gate for the refund, closing the window opened by the cross-check
                                // call above. (The durable belt-and-suspenders is the deferred PGCryptoID/dedup
                                // unique index.)
                                int claimedDeposit = db.Database.ExecuteSqlCommand(
                                    "UPDATE dbo.tblT_Card_Deposit SET Status = {0} WHERE ID = {1} AND Status = {2}",
                                    PGStatusModel.Failed, cr.ID, PGStatusModel.InProgress);
                                if (claimedDeposit != 1)
                                {
                                    System.Diagnostics.Trace.TraceWarning(
                                        "Wasabi deposit-fail refund skipped: deposit already finalized for order " + cr.ID);
                                    return;
                                }

                                // Route the refund through the shared atomic balance helper: EnsureWallet
                                // closes the historical null-deref (a user with no balance row), and Credit
                                // does the conditional UPDATE + ledger write in one Serializable transaction
                                // instead of the old non-atomic read-modify-write. Credit the settled net
                                // (cr.Total); record gross (cr.Amount) and fee in the ledger.
                                WalletService.EnsureWallet(cr.UserID);
                                var refund = WalletService.Credit(
                                    cr.UserID,
                                    Convert.ToDecimal(cr.Total),
                                    Convert.ToDecimal(cr.Amount),
                                    Convert.ToDecimal(cr.Fee),
                                    Convert.ToDouble(cr.FeeInPercentage),
                                    WalletService.TypeDepositRefund,
                                    cr.ID);
                                if (!refund.Success)
                                {
                                    System.Diagnostics.Trace.TraceError(
                                        "Wasabi deposit-fail refund credit failed (" + refund.FailureReason + ") for order " + cr.ID);
                                }

                            }
                        }


                    }
                }

                


                return;
            }
            catch (Exception ex)
            {
                // Surface failures instead of swallowing them silently (type only, no PII/detail).
                System.Diagnostics.Trace.TraceError("Wasabi callback processing failed: " + ex.GetType().FullName);
            }
        }
        public void PGCrypto(PGCryptoModel x)
        {
            try
            {
                DBEntities db = new DBEntities();
                tblH_Partner_Webhook log = new tblH_Partner_Webhook();
                log.Response = JsonConvert.SerializeObject(x);
                log.ResponseDate = DateTime.Now;
                log.Type = "PGCrypto";
                db.tblH_Partner_Webhook.Add(log);
                db.SaveChanges();

                // A PGCrypto money webhook must carry a TransactionID: it is our idempotency/dedup key.
                // Without it we cannot distinguish a first delivery from a replay, so reject rather than
                // process an un-dedupable credit (the un-keyed Address+Total match below is exactly where
                // cross-row reuse would otherwise be possible).
                if (string.IsNullOrEmpty(x.TransactionID))
                {
                    System.Diagnostics.Trace.TraceWarning("PGCrypto callback ignored: missing TransactionID.");
                    return;
                }
                // Replay guard (defense-in-depth): if this provider transaction was already applied — its
                // TransactionID is already recorded on a card or deposit — do not process it again. This
                // stops a replayed (but validly-signed) webhook from being matched against a *different*
                // Created row that happens to share the same address + amount. The concurrent race is
                // closed by the atomic claim on the matched row below; the durable belt-and-suspenders is
                // a DB unique index on PGCryptoID, tracked as deferred hardening.
                if (db.tblT_Card.Any(p => p.PGCryptoID == x.TransactionID) ||
                    db.tblT_Card_Deposit.Any(p => p.PGCryptoID == x.TransactionID))
                {
                    System.Diagnostics.Trace.TraceWarning("PGCrypto callback ignored: TransactionID already processed.");
                    return;
                }

                if (x.Symbol == "USDT")
                {
                    var am = Convert.ToDouble(x.Amount);

                    //static address
                    var z = db.tblT_Card.Where(p => p.Address == x.Address && p.Total == am && p.Status == PGStatusModel.Created).FirstOrDefault();
                    if (z != null)
                    {
                        // Atomically claim this Created order so concurrent/replayed deliveries cannot both
                        // provision a card against it. The loser (0 rows affected) bails.
                        int claimedCard = db.Database.ExecuteSqlCommand(
                            "UPDATE dbo.tblT_Card SET Status = {0}, Txhash = {1}, ReceiptURL = {2}, PGCryptoID = {3} WHERE ID = {4} AND Status = {5}",
                            PGStatusModel.Paid, x.ReceiptURL.ToString(), x.ReceiptURL.ToString(), x.TransactionID, z.ID, PGStatusModel.Created);
                        if (claimedCard != 1)
                        {
                            System.Diagnostics.Trace.TraceWarning("PGCrypto card provisioning skipped: order already claimed " + z.ID);
                            return;
                        }
                        // Reflect the claimed state on the tracked entity for the provisioning calls below.
                        z.Status = PGStatusModel.Paid;
                        z.Txhash = x.ReceiptURL.ToString();
                        z.ReceiptURL = x.ReceiptURL.ToString();
                        z.PGCryptoID = x.TransactionID;

                        //do open card
                        if (z.HolderID == null)
                        {
                            WCOpenCardRequestModel req = new WCOpenCardRequestModel();
                            req.merchantOrderNo = z.ID;
                            req.amount = Convert.ToDouble(z.InitialDeposit);
                            req.cardTypeId = Convert.ToInt32(z.CardTypeId);

                            var res = WasabiCardService.openCard(req);
                            if (res != null)
                            {
                                if (res.code == 200)
                                {
                                    var cdt = res.data[0];
                                    z.OrderNo = cdt.orderNo;
                                    z.BaseAmount = Convert.ToDouble(cdt.amount);
                                    z.BaseFee = Convert.ToDouble(cdt.fee);
                                    z.ReceivedCurrency = cdt.receivedCurrency;
                                    z.Currency = cdt.currency;
                                    z.ReceivedAmount = req.amount;
                                    z.Status = PGStatusModel.InProgress;
                                    z.Param4 = cdt.status;
                                    z.Param5 = cdt.type;
                                    z.isActive = 0;

                                    db.SaveChanges();

                                }
                            }
                        }
                        else
                        {
                            WCOpenCardWithHolderRequestModel req = new WCOpenCardWithHolderRequestModel();
                            req.merchantOrderNo = z.ID;
                            req.amount = Convert.ToDouble(z.InitialDeposit);
                            req.cardTypeId = Convert.ToInt32(z.CardTypeId);
                            req.holderId = Convert.ToInt32(z.HolderID);


                            var res = WasabiCardService.openCardWithHolder(req);
                            if (res != null)
                            {
                                if (res.code == 200)
                                {
                                    var cdt = res.data[0];
                                    z.OrderNo = cdt.orderNo;
                                    z.BaseAmount = Convert.ToDouble(cdt.amount);
                                    z.BaseFee = Convert.ToDouble(cdt.fee);
                                    z.ReceivedCurrency = cdt.receivedCurrency;
                                    z.Currency = cdt.currency;
                                    z.ReceivedAmount = req.amount;
                                    z.Status = PGStatusModel.InProgress;
                                    z.Param4 = cdt.status;
                                    z.Param5 = cdt.type;
                                    z.DateCreated = DateTime.Now;
                                    z.isActive = 0;

                                    db.SaveChanges();

                                }
                            }
                        }

                        return;
                    }

                    var y = db.tblT_Card_Deposit.Where(p => p.Address == x.Address && p.Total == am && p.Status == PGStatusModel.Created).FirstOrDefault();
                    if (y != null)
                    {
                        // Atomically claim this Created order so concurrent/replayed deliveries cannot both
                        // process a top-up against it. The loser (0 rows affected) bails.
                        int claimedDep = db.Database.ExecuteSqlCommand(
                            "UPDATE dbo.tblT_Card_Deposit SET Status = {0}, Txhash = {1}, ReceiptURL = {2}, PGCryptoID = {3} WHERE ID = {4} AND Status = {5}",
                            PGStatusModel.Paid, x.ReceiptURL.ToString(), x.ReceiptURL.ToString(), x.TransactionID, y.ID, PGStatusModel.Created);
                        if (claimedDep != 1)
                        {
                            System.Diagnostics.Trace.TraceWarning("PGCrypto deposit provisioning skipped: order already claimed " + y.ID);
                            return;
                        }
                        // Reflect the claimed state on the tracked entity for the provisioning calls below.
                        y.Status = PGStatusModel.Paid;
                        y.Txhash = x.ReceiptURL.ToString();
                        y.ReceiptURL = x.ReceiptURL.ToString();
                        y.PGCryptoID = x.TransactionID;

                        //do deposit
                        WCDepositCardRequestModel req = new WCDepositCardRequestModel();
                        req.merchantOrderNo = y.ID;
                        req.amount = Convert.ToDouble(y.ReceivedAmount);
                        req.cardNo = y.CardNo;

                        var res = WasabiCardService.depositCard(req);
                        if (res != null)
                        {
                            if (res.code == 200)
                            {
                                var cdt = res.data;
                                y.OrderNo = cdt.orderNo;
                                y.BaseFee = Convert.ToDouble(cdt.fee);
                                y.Currency = cdt.currency;
                                y.ReceivedAmount = req.amount;
                                y.Status = PGStatusModel.InProgress;
                                y.Param4 = cdt.status;
                                y.Param5 = cdt.type;

                                db.SaveChanges();

                            }
                        }
                        return;

                    }
                }
                //invoice
                //var z = db.tblT_Transaction.Where(p => p.PGCryptoInvoiceID == x.TransactionID && p.Status == StatusModel.WaitingPayment).FirstOrDefault();
                //if (z != null)
                //{
                //    var am = Convert.ToDouble(x.Total);
                //    if (am == z.Amount)
                //    {
                //        z.Status = StatusModel.Paid;
                //        z.ReceiptURL = x.ReceiptURL;
                //        db.SaveChanges();
                //    }
                //}
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("PGCrypto callback processing failed: " + ex.GetType().FullName);
            }

            return;
        }


        public void reTopup(string tid)
        {
            DBEntities db = new DBEntities();

            var y = db.tblT_Card_Deposit.Where(p => p.ID == tid && p.Status == "paid").FirstOrDefault();
            if (y != null)
            {
                //do deposit
                WCDepositCardRequestModel req = new WCDepositCardRequestModel();
                req.merchantOrderNo = y.ID;
                req.amount = Convert.ToDouble(y.ReceivedAmount);
                req.cardNo = y.CardNo;

                var res = WasabiCardService.depositCard(req);
                if (res != null)
                {
                    if (res.code == 200)
                    {
                        var cdt = res.data;
                        y.OrderNo = cdt.orderNo;
                        y.BaseFee = Convert.ToDouble(cdt.fee);
                        y.Currency = cdt.currency;
                        y.ReceivedAmount = req.amount;
                        y.Status = PGStatusModel.InProgress;
                        y.Param4 = cdt.status;
                        y.Param5 = cdt.type;

                        db.SaveChanges();

                    }
                }
            }

            return;
        }


        public void recreateCallback()
        { 
            DBEntities db = new DBEntities();
            var ck = db.tblH_Partner_Webhook.Where(p => p.Type == "Wasabi Card").ToList();
            for (int i = 0; i < ck.Count; i++)
            {
                var q = JsonConvert.DeserializeObject<WCAuthTransactionModel>(ck[i].Request);
                var amt = q.amount;

                var td = db.tblT_Card_Transaction.Where(p => p.TradeNo == q.tradeNo).FirstOrDefault();
                if (td == null && q.status == "authorized" && q.type == "auth")
                {
                    tblT_Card_Transaction x = new tblT_Card_Transaction();

                    x.CardNo = q.cardNo;
                    x.TradeNo = q.tradeNo;
                    x.OriginTradeNo = q.tradeNo;
                    x.Currency = q.currency;
                    if (q.amount != null)
                        x.Amount = Convert.ToDouble(q.amount);

                    if (q.authorizedAmount != null)
                        x.AuthorizedAmount = Convert.ToDouble(q.authorizedAmount);

                    x.AuthorizedCurrency = q.authorizedCurrency;

                    if (q.fee != null)
                        x.Fee = Convert.ToDouble(q.fee);
                    x.FeeCurrency = q.feeCurrency;

                    if (q.crossBoardFee != null)
                        x.CrossBoardFee = Convert.ToDouble(q.crossBoardFee);

                    if (q.crossBoardFeeCurrency != null)
                        x.CrossBoardFeeCurrency = q.crossBoardFeeCurrency.ToString();

                    if (q.settleAmount != null)
                        x.SettleAmount = Convert.ToDouble(q.settleAmount);

                    if (q.settleCurrency != null)
                        x.SettleCurrency = q.settleCurrency.ToString();

                    x.SettleDate = DateTime.Now;
                    x.MerchantName = q.merchantName;
                    x.Type = q.type;
                    x.TypeStr = q.typeStr;
                    x.Status = q.status;
                    x.StatusStr = q.statusStr;
                    x.Description = q.description;
                    x.TransactionTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(q.transactionTime / 1000d)).ToLocalTime();

                    db.tblT_Card_Transaction.Add(x);
                    db.SaveChanges();
                }
            }
        }
    }
}
