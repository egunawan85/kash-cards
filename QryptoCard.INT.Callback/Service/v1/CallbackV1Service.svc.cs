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
                            // Money-critical finalization (bind card, provider cross-check, activate,
                            // record card balance) is shared with the reconciliation sweep so it lives
                            // in one place; the sensitive PAN/CVV enrichment below stays webhook-only.
                            var fo = CardFinalizationService.FinalizeOpenSuccess(q.merchantOrderNo, q.cardNo);
                            if (fo == CardFinalizationService.FinalizeOutcome.Confirmed)
                            {
                                var cr = db.tblT_Card.Where(p => p.ID == q.merchantOrderNo).FirstOrDefault();
                                if (cr != null && !string.IsNullOrEmpty(cr.CardNo))
                                {
                                    var qqq = new WCCardInfoSensitiveRequestModel();
                                    qqq.cardNo = cr.CardNo;
                                    var ressen = WasabiCardService.getCardInfoSensitive(qqq);
                                    if (ressen != null && ressen.data != null)
                                    {
                                        cr.CardNumber = decryptSensitive(ressen.data.cardNumber);
                                        cr.CVV = ressen.data.cvv;
                                        cr.ValidPeriod = ressen.data.expireDate;
                                        db.SaveChanges();
                                    }
                                }

                                //do send email
                            }
                        }
                        
                    }
                    else if (type == "deposit")
                    {
                        var cr = db.tblT_Card_Deposit.Where(p => p.ID == q.merchantOrderNo && (p.Status == PGStatusModel.InProgress || p.Status == PGStatusModel.PendingProvider)).FirstOrDefault();

                        if (q.status == "success")
                        {
                            if (cr != null)
                            {
                                // Shared money-critical finalization (also used by the reconciliation sweep).
                                CardFinalizationService.FinalizeTopUpSuccess(q.merchantOrderNo);

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

                                // Refund atomically: the deposit's claim (InProgress/PendingProvider->Failed)
                                // and the wallet credit commit or roll back together inside one Serializable
                                // transaction. The claim is also the idempotency gate — a concurrent or
                                // replayed delivery finds 0 rows to claim and is a no-op — and folding it into
                                // the credit closes the crash window that previously could leave a deposit
                                // Failed-but-unrefunded. EnsureWallet first so a user without a balance row
                                // (historical null-deref) still gets the refund. Credit the settled net
                                // (cr.Total); record the fee in the ledger.
                                WalletService.EnsureWallet(cr.UserID);
                                var refund = WalletService.CreditRefund(
                                    cr.UserID,
                                    Convert.ToDecimal(cr.Total),
                                    Convert.ToDecimal(cr.Fee),
                                    Convert.ToDouble(cr.FeeInPercentage),
                                    cr.ID);
                                if (!refund.Success && refund.FailureReason != "claim_lost")
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

        // Anti-dust floor on wallet credits (R12): deposits settling below this net amount are
        // journalled but not credited, to prevent dust/precision griefing. Card-action minimums
        // are separate and unchanged.
        private const decimal PGCryptoCreditFloor = 1m;

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
                // Wallet-only credit model: every confirmed USDT deposit to a user's static
                // address credits that user's prepaid balance. There is no order matching and no
                // direct-to-card provisioning from a deposit — card funding is now a balance debit
                // on the spend path, so the legacy Address+Total+Created match and its inline
                // WasabiCard provisioning are removed (R1). Per-event dedup + the credit happen
                // atomically inside WalletService.CreditDeposit; this method no longer needs the
                // old card/deposit replay guard.

                // Non-USDT funds sent to the (TRC20/USDT) address are real but unhandled — never
                // silent-skip; surface for manual handling (C2.d).
                if (x.Symbol != "USDT")
                {
                    System.Diagnostics.Trace.TraceWarning(
                        "PGCrypto callback: non-USDT symbol '" + x.Symbol + "' for tx " + x.TransactionID + " — manual handling required.");
                    return;
                }

                // Confirmed-status gate: credit only on a settled/paid deposit. A pre-confirmation
                // delivery is ignored (no credit); the confirmed delivery carries isPaid == 1.
                if (x.isPaid != 1)
                {
                    System.Diagnostics.Trace.TraceWarning(
                        "PGCrypto callback: unconfirmed deposit (isPaid=" + x.isPaid + ") for tx " + x.TransactionID + " — not credited.");
                    return;
                }

                // Address ownership: the credited user is derived strictly from OUR record (the
                // static deposit address -> owning UserID), never from the webhook body.
                var dep = db.tblM_User_Crypto_Deposit
                    .FirstOrDefault(p => p.Address == x.Address && p.isActive == 1);
                if (dep == null)
                {
                    // Real funds we cannot attribute to a user — log + alert, never credit.
                    System.Diagnostics.Trace.TraceError(
                        "PGCrypto callback: deposit to unknown/inactive address for tx " + x.TransactionID + " — manual handling required.");
                    return;
                }

                // Anti-dust floor: the webhook journal row above already records the deposit; we
                // simply do not credit sub-floor dust (R12). Net is what Runegate settled (R15).
                decimal net = x.Total ?? 0m;
                if (net < PGCryptoCreditFloor)
                {
                    System.Diagnostics.Trace.TraceWarning(
                        "PGCrypto callback: sub-floor deposit " + net + " for tx " + x.TransactionID + " — logged, not credited.");
                    return;
                }

                // Ensure the wallet exists (lazy-provisioned users), then credit the NET amount and
                // write the per-event dedup row in ONE atomic transaction — a replayed or
                // concurrent duplicate rolls back as a no-op. Commission is recorded for
                // reconciliation; the gross deposit is recoverable as Amount + Commision.
                WalletService.EnsureWallet(dep.UserID);
                var credit = WalletService.CreditDeposit(
                    dep.UserID, net, x.Commision ?? 0m, x.CommisionInPercentage ?? 0d,
                    x.TransactionID, x.Status, JsonConvert.SerializeObject(x));

                if (!credit.Success && credit.FailureReason != "duplicate_event")
                {
                    // Operational failure (not a benign duplicate). The webhook journal row enables
                    // replay/reconciliation; non-200-on-failure so the gateway retries is the
                    // coupled callback-contract change tracked separately (C2.a).
                    System.Diagnostics.Trace.TraceError(
                        "PGCrypto credit failed (" + credit.FailureReason + ") for tx " + x.TransactionID);
                }
                else if (credit.Success)
                {
                    // Deposit-into-card settlement (streaming model). On a GENUINE new credit only,
                    // apply it to the depositing user's open funding intent; when covered the intent
                    // advances to Funding for the streaming forwarder. Gated by
                    // CardFundingStreamingEnabled; a no-op (and no double-apply) otherwise.
                    CardFundingSettlementService.OnDepositCredited(dep.UserID, net, x.TransactionID);

                    // WasabiCard auto-funding — legacy Tier 2 (eager pass-through), gated by its own
                    // WasabiCardAutoFundEnabled switch. Retired by the streaming model above; kept
                    // behind its independent switch so the two paths never both move money. Only on a
                    // GENUINE new credit (never a duplicate_event replay) so a redelivered webhook
                    // can't double-forward; idempotent (keyed on the TransactionID).
                    WasabiCardFundingService.OnDepositCredited(net, x.TransactionID);
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


        public int ReconcilePendingProvider()
        {
            try
            {
                return ReconciliationService.ReconcilePendingProvider();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("ReconcilePendingProvider failed: " + ex.GetType().FullName);
                return 0;
            }
        }

        public string RunWasabiCardMonitor()
        {
            try
            {
                return WasabiCardFundingService.RunMonitorTick();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("RunWasabiCardMonitor failed: " + ex.GetType().FullName);
                return "{\"error\":\"monitor_failed\"}";
            }
        }

        // Deposit-into-card streaming pump: forwards covered intents to WasabiCard and confirms the
        // float credit (Funding -> Confirming -> Issuing). The INT-tier issuance tick then opens/tops
        // up the card. Driven by the scheduler like RunWasabiCardMonitor; a no-op while the streaming
        // switch (CardFundingStreamingEnabled) is OFF.
        public string RunCardFundingPump()
        {
            try
            {
                return CardFundingForwardService.RunTick();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("RunCardFundingPump failed: " + ex.GetType().FullName);
                return "{\"error\":\"pump_failed\"}";
            }
        }

        public void reTopup(string tid)
        {
            DBEntities db = new DBEntities();

            // Reconciliation tool for the wallet-only model: only re-provision a deposit that is
            // PendingProvider — i.e. the balance was ALREADY debited but the provider result was
            // ambiguous. Re-provisioning such an order does not (and must not) debit again. The
            // legacy "paid" status is never produced now, and re-funding a completed deposit would
            // double-fund the card with no second debit, so it is no longer eligible here.
            var y = db.tblT_Card_Deposit.Where(p => p.ID == tid && p.Status == PGStatusModel.PendingProvider).FirstOrDefault();
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
