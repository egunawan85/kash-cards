using Newtonsoft.Json;
using QryptoCard.INT.Model;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;
using QryptoCard.Sec;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Web.UI;

namespace QryptoCard.INT.Script.Service.API.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "CardV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select CardV1Service.svc or CardV1Service.svc.cs at the Solution Explorer and start debugging.
    public class CardV1Service : ICardV1Service
    {
        DBEntities db = new DBEntities();
        OutputModel op = new OutputModel();

        string getUserId(string em)
        {
            var a = db.tblM_User_API.Where(p => p.APIKey == em).FirstOrDefault();
            return a.UserID;

        }

        public OutputModel CardType(string em, tblM_Card_Type x)
        {
            try
            {
                string uid = getUserId(em);
                var data = db.tblM_Card_Type.Where(p => p.isActive == 1).ToList();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "No card available";
                    return op;
                }
                else
                {

                    op.Status = "success";
                    op.Message = "Success";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getCardTypeById(string em, tblM_Card_Type x)
        {
            try
            {
                string uid = getUserId(em);
                var data = db.tblM_Card_Type.Where(p => p.CardTypeId == x.CardTypeId && p.isActive == 1).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "No card available";
                    return op;
                }
                else
                {

                    op.Status = "success";
                    op.Message = "Success";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public string getDate()
        {
            return generateBirthdate();
        }

        string generateBirthdate()
        {
            var min = DateTime.Parse("1970/01/01");
            var max = DateTime.Parse("2000/12/31");

            var minTicks = min.Ticks;
            var maxTicks = max.Ticks;

            var baseTicks = maxTicks - minTicks;

            var rnd = new Random();

            var toAdd = (long)(rnd.NextDouble() * baseTicks);

            var newDate = new DateTime(minTicks + toAdd);

            return newDate.ToString("yyyy-MM-dd");
        }

        public OutputModel getHolderDetail(string em, tblM_Cardholder x)
        {
            try
            {
                var uid = getUserId(em);

                var data = db.tblM_Cardholder.Where(p => p.UserID == uid && p.HolderID == x.HolderID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Cardholder is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get cardholder detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel checkHolderByCardTypeId(string em, tblM_Cardholder x)
        {
            try
            {
                var uid = getUserId(em);

                var data = db.tblM_Cardholder.Where(p => p.UserID == uid && p.CardTypeId == x.CardTypeId && p.isActive == 1 && p.Status == "pass_audit").FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Cardholder is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get cardholder detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public void createCardHolder(string emu, long cardtypeid, string fn, string ln, string em)
        {
            string uid = getUserId(emu);
            var adr = db.tblM_Address_Generator.Where(p => p.isUsed == 0 && p.isActive == 1 && p.CityCode != null).FirstOrDefault();
            if (adr != null)
            {
                WCCreateHolderRequestModel ho = new WCCreateHolderRequestModel();
                ho.cardTypeId = cardtypeid;
                ho.email = em;
                ho.firstName = fn;
                ho.lastName = ln;
                ho.address = adr.Street;
                ho.country = "US";
                ho.mobile = adr.PhoneNumber;
                ho.areaCode = "+1";
                ho.birthday = generateBirthdate();
                ho.postCode = adr.PostalCode;
                ho.town = adr.CityCode;
                var ch = WasabiCardService.createHolder(ho);
                if (ch != null)
                {
                    if (ch.data.status == "pass_audit")
                    {
                        tblM_Cardholder cho = new tblM_Cardholder();
                        cho.ID = Guid.NewGuid().ToString();
                        cho.HolderID = ch.data.holderId;

                        WCGetHolderRequestModel gh = new WCGetHolderRequestModel();
                        gh.pageNum = 1;
                        gh.pageSize = 100;
                        var ck = WasabiCardService.getHolderList(gh);
                        if (ck != null)
                        {
                            var x = ck.data.records;
                            var z = x.Where(p => p.holderId == cho.HolderID).FirstOrDefault();
                            if (z != null)
                            {
                                cho.UserID = uid;
                                cho.Address = z.address;
                                cho.AreaCode = z.areaCode;
                                cho.Birthday = z.birthday;
                                cho.CardTypeId = ho.cardTypeId;
                                cho.Country = z.country;
                                cho.CountryStr = z.countryStr;
                                cho.DateCreated = DateTime.Now;
                                cho.Email = z.email;
                                cho.FirstName = z.firstName;
                                cho.LastName = z.lastName;
                                cho.Mobile = z.mobile;
                                cho.PostCode = z.postCode;
                                cho.State = z.state;
                                cho.StateStr = z.stateStr;
                                cho.Status = z.status;
                                cho.StatusStr = z.statusStr;
                                cho.Town = z.town;
                                cho.TownStr = z.townStr;
                                cho.isActive = 1;
                                db.tblM_Cardholder.Add(cho);
                                db.SaveChanges();
                            }
                        }
                    }
                }
            }

        }

        public OutputModel openCard(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);
                
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                if (x.CardTypeId == null)
                {
                    op.Status = "failed";
                    op.Message = "CardTypeId parameter cannot be null";
                    return op;
                }

                if (x.InitialDeposit == null || x.InitialDeposit < 20)
                {
                    op.Status = "failed";
                    op.Message = "InitialDeposit parameter cannot be null and cannot be less than $20";
                    return op;
                }

                if (x.UserReferenceID == null || x.UserReferenceID == "")
                {
                    op.Status = "failed";
                    op.Message = "UserReferenceID parameter cannot be null";
                    return op;
                }

                var data = db.tblM_Card_Type.Where(p => p.CardTypeId == x.CardTypeId && p.isActive == 1).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card is not available";
                    return op;
                }
                else
                {
                    var ck = db.tblT_Card.Where(p => p.CardTypeId == x.CardTypeId && p.UserID == uid && p.Status == StatusModel.Created).FirstOrDefault();
                    if (ck != null)
                    {
                        op.Status = "failed";
                        op.Message = "You have pending transaction on this card";
                        return op;
                    }

                    // Wallet-only: paid from the prepaid balance, no static-address lookup.
                    x.UserID = uid;

                    if (data.NeedCardHolder == 1)
                    {
                        x.isNeedCardholder = 1;

                        if (x.HolderID == null)
                        {
                            if (x.Param1 == "" && x.Param2 == "" && x.Param3 == "")
                            {
                                op.Status = "failed";
                                op.Message = "Card holder data cannot be empty";
                                return op;
                            }
                            else
                            {
                                var addr = db.tblM_Address_Generator.Where(p => p.isActive == 1 && p.isUsed == 0 && p.CityCode != null).FirstOrDefault();
                                if (addr == null)
                                {
                                    op.Status = "failed";
                                    op.Message = "No address available for card holder";
                                    return op;
                                }
                                WCCreateHolderRequestModel chx = new WCCreateHolderRequestModel();
                                chx.cardTypeId = data.CardTypeId.Value;
                                chx.areaCode = "+1";
                                chx.mobile = addr.PhoneNumber;
                                chx.email = x.Param3;
                                chx.firstName = x.Param1;
                                chx.lastName = x.Param2;
                                chx.birthday = generateBirthdate();
                                chx.country = "US";
                                chx.address = addr.Street;
                                chx.town = addr.CityCode;
                                chx.postCode = addr.PostalCode;

                                var chdr = WasabiCardService.createHolder(chx);
                                if (chdr != null && chdr.code == -1)
                                {
                                    op.Status = "failed";
                                    op.Message = chdr.msg;
                                    return op;
                                }

                                if (chdr.data.status == "pass_audit")
                                {
                                    tblM_Cardholder chu = new tblM_Cardholder();
                                    chu.ID = Guid.NewGuid().ToString();
                                    chu.UserID = x.UserID;
                                    chu.HolderID = chdr.data.holderId;
                                    chu.Address = chx.address;
                                    chu.Mobile = chx.mobile;
                                    chu.Email = chx.email;
                                    chu.FirstName = chx.firstName;
                                    chu.LastName = chx.lastName;
                                    chu.Birthday = chx.birthday;
                                    chu.Country = chx.country;
                                    chu.CardTypeId = chx.cardTypeId;
                                    chu.AreaCode = chx.areaCode;
                                    chu.Town = chx.town;
                                    chu.PostCode = chx.postCode;
                                    chu.DateCreated = DateTime.Now;
                                    chu.isActive = 1;
                                    db.tblM_Cardholder.Add(chu);

                                    addr.isUsed = 1;
                                    addr.DateUsed = DateTime.Now;
                                    db.SaveChanges();

                                    x.HolderID = chu.HolderID;
                                }
                                else
                                {
                                    op.Status = "failed";
                                    op.Message = "Holder name is not pass for checking. Please use another name";
                                    return op;
                                }

                            }
                        }


                        //createCardHolder(uid, x.CardTypeId.Value, x.Param1, x.Param2, x.Param3);

                        //if (x.HolderID != null)
                        //{
                        //    var ch = db.tblM_Cardholder.Where(p => p.isActive == 1 && p.UserID == x.UserID && p.HolderID == x.HolderID && p.CardTypeId == x.CardTypeId).FirstOrDefault();
                        //    if (ch == null)
                        //    {
                        //        op.Status = "failed";
                        //        op.Message = "Card holder is not found";
                        //        return op;
                        //    }
                        //}
                        //else
                        //{

                        //}


                    }



                    if (x.InitialDeposit == null)
                    {
                        op.Status = "failed";
                        op.Message = "Initial deposit amount cannot be null";
                        return op;
                    }
                    if (x.InitialDeposit < Convert.ToDouble(data.DepositAmountMinQuotaForActiveCard))
                    {
                        op.Status = "failed";
                        op.Message = "Minimum initial deposit amount is " + data.DepositAmountMinQuotaForActiveCard + " USD";
                        return op;
                    }
                    if (x.InitialDeposit > Convert.ToDouble(data.DepositAmountMaxQuotaForActiveCard))
                    {
                        op.Status = "failed";
                        op.Message = "Minimum initial deposit amount is " + data.DepositAmountMaxQuotaForActiveCard + " USD";
                        return op;
                    }

                    x.Price = Convert.ToDouble(data.CardPrice);
                    //x.InitialDeposit = Convert.ToDouble(data.DepositAmountMinQuotaForActiveCard);

                    x.FeeInPercentage = Convert.ToDouble(data.RechargeFeeRate);

                    //var comm = db.tblM_User_Fee.Where(p => p.UserID == x.UserID).FirstOrDefault();
                    //if (comm != null)
                    //{
                    //    x.FeeInPercentage = comm.Fee;
                    //}
                    //else
                    //{
                    //    var f = db.tblM_Setting.Where(p => p.ID == 2).FirstOrDefault();
                    //    if (f != null)
                    //    {
                    //        x.FeeInPercentage = f.Value;
                    //    }
                    //    else
                    //        x.FeeInPercentage = 1;
                    //}

                    x.Currency = "USD";
                    x.ReceivedCurrency = "USD";
                    x.Fee = (Convert.ToDouble(x.FeeInPercentage) / 100) * x.InitialDeposit.Value;
                    x.Total = x.Price + x.InitialDeposit + x.Fee;
                    x.ReceivedAmount = x.InitialDeposit;
                    x.DateExpired = DateTime.Now.AddHours(1);

                    // Atomic sequence (race-safe) instead of read-increment-save on the counter row.
                    x.ID = "QRYCRDBUY" + CounterService.Next(1).ToString("000000000000");

                    // Pay from the prepaid balance (debit-first, then provision via WasabiCard).
                    var spend = CardSpendService.OpenCard(x);
                    if (!spend.Success)
                    {
                        op.Status = "failed";
                        op.Message = spend.Message;
                        return op;
                    }
                    x.Status = spend.Status;
                    op.Status = "success";
                    op.Message = spend.Message;
                    op.Data = JsonConvert.SerializeObject(x, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getCardListActive(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);
                
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                var data = db.vw_Card.Where(p => p.UserID == uid && p.isActive == 1).ToList();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card is not found";
                    return op;
                }
                else
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        WCCardInfoRequestModel req = new WCCardInfoRequestModel();
                        req.cardNo = data[i].CardNo;
                        req.onlySimpleInfo = false;
                        var res = WasabiCardService.getCardInfo(req);
                        if (res != null)
                        {
                            if (res.code == 200)
                            {
                                data[i].Param5 = res.data.balanceInfo.amount;
                            }
                        }
                    }

                    op.Status = "success";
                    op.Message = "Success get cards";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getCardListAll(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);

                var data = db.vw_Card.Where(p => p.UserID == uid).OrderByDescending(p => p.DateCreated).ToList();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get cards";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getCardDetail(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);
                
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                var data = db.vw_Card.Where(p => p.UserID == uid && p.ID == x.ID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card is not found";
                    return op;
                }
                else
                {
                    WCCardInfoRequestModel req = new WCCardInfoRequestModel();
                    req.cardNo = data.CardNo;
                    req.onlySimpleInfo = false;
                    var res = WasabiCardService.getCardInfo(req);
                    if (res != null)
                    {
                        if (res.code == 200)
                        {

                            WCCardInfoSensitiveRequestModel reqq = new WCCardInfoSensitiveRequestModel();
                            reqq.cardNo = data.CardNo;
                            var ress = WasabiCardService.getCardInfoSensitive(reqq);

                            data.CVV = ress.data.cvv;
                            data.ValidPeriod = ress.data.expireDate;
                            data.Param5 = res.data.balanceInfo.amount;
                        }
                    }

                    data.CVV = Convert.ToBase64String(Encoding.ASCII.GetBytes(decrypt(data.CVV)));
                    data.ValidPeriod = Convert.ToBase64String(Encoding.ASCII.GetBytes(decrypt(data.ValidPeriod)));
                    op.Status = "success";
                    op.Message = "Success get card detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getCardBalance(string em, CardBalanceModel x)
        {
            try
            {
                var uid = getUserId(em);
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                if (x.ID == null || x.ID == "")
                {
                    op.Status = "failed";
                    op.Message = "ID parameter cannot be null";
                    return op;
                }

                var data = db.tblT_Card.Where(p => p.ID == x.ID && p.UserID == uid).FirstOrDefault();


                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card is not found";
                    return op;
                }
                else
                {
                    WCCardInfoRequestModel req = new WCCardInfoRequestModel();
                    req.cardNo = data.CardNo;
                    req.onlySimpleInfo = false;
                    var res = WasabiCardService.getCardInfo(req);
                    if (res != null)
                    {
                        if (res.code == 200)
                        {
                            CardBalanceModel bal = new CardBalanceModel();
                            bal.ID = data.ID;
                            bal.Currency = data.Currency;
                            bal.Balance = res.data.balanceInfo.amount;


                            op.Status = "success";
                            op.Message = "Success get card balance";
                            op.Data = JsonConvert.SerializeObject(bal, Formatting.None);
                        }
                        else
                        {
                            op.Status = "failed";
                            op.Message = "Something bad happened";
                        }
                    }
                    else
                    {
                        op.Status = "failed";
                        op.Message = "Something bad happened";
                    }
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getCardTransaction(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);

                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                if (x.ID == null || x.ID == "")
                {
                    op.Status = "failed";
                    op.Message = "ID parameter cannot be null";
                    return op;
                }
                var cd = db.tblT_Card.Where(p => p.ID == x.ID && p.UserID == uid).FirstOrDefault();
                if (cd == null)
                {
                    op.Status = "failed";
                    op.Message = "ID is not found";
                    return op;
                }
                var data = db.tblT_Card_Transaction.Where(p => p.CardNo == cd.CardNo && (p.Status == "succeed" || p.Status == "success" || p.Status == "authorized")).OrderByDescending(p => p.TransactionTime).Take(20).ToList();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Transaction is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get card transaction";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getCardTransactionDetail(string em, tblT_Card_Transaction x)
        {
            try
            {
                var uid = getUserId(em);
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                var data = db.tblT_Card_Transaction.Where(p => p.ID == x.ID).FirstOrDefault();
                // Authorization: the transaction's card must belong to the caller; else treat as not-found.
                if (data != null && !db.tblT_Card.Any(c => c.CardNo == data.CardNo && c.UserID == uid))
                    data = null;

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Transaction is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get transaction";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel depositCard(string em, tblT_Card_Deposit x)
        {
            try
            {
                var uid = getUserId(em);
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                if (x.ID == null || x.ID == "")
                {
                    op.Status = "failed";
                    op.Message = "ID parameter cannot be null";
                    return op;
                }

                if (x.Amount == null || x.Amount < 20)
                {
                    op.Status = "failed";
                    op.Message = "Amount parameter cannot be null and cannot be less than $20";
                    return op;
                }

                if (x.UserReferenceID == null || x.UserReferenceID == "")
                {
                    op.Status = "failed";
                    op.Message = "UserReferenceID parameter cannot be null";
                    return op;
                }

                var data = db.tblT_Card_Deposit.Where(p => p.UserID == uid && p.Amount == x.Amount && p.Status == StatusModel.Created).FirstOrDefault();

                if (data != null)
                {
                    op.Status = "failed";
                    op.Message = "You have pending deposit with same amount";
                    return op;
                }
                else
                {
                    var card = db.tblT_Card.Where(p => p.ID == x.ID && p.UserID == uid).FirstOrDefault();
                    if (card == null)
                    {
                        op.Status = "failed";
                        op.Message = "Card is not found";
                        return op;
                    }

                    x.UserID = uid;

                    var ct = db.tblM_Card_Type.Where(p => p.CardTypeId == card.CardTypeId).FirstOrDefault();
                    x.FeeInPercentage = Convert.ToDouble(ct.RechargeFeeRate);

                    x.CardNo = card.CardNo;
                    x.Currency = "USD";
                    x.Fee = (Convert.ToDouble(x.FeeInPercentage) / 100) * x.Amount.Value;
                    x.Total = Math.Round((x.Amount.Value + x.Fee.Value), 2);
                    x.ReceivedAmount = x.Amount.Value;
                    x.Status = StatusModel.Created;
                    x.DateTransaction = DateTime.Now;
                    x.DateExpired = x.DateTransaction.Value.AddHours(1);

                    // Wallet-only: top-ups are paid from the prepaid balance, no static-address wait.
                    // Atomic sequence (race-safe) instead of read-increment-save on the counter row.
                    x.ID = "QRYCRDPST" + CounterService.Next(2).ToString("000000000000");

                    // Debit balance, then provision the top-up via WasabiCard.
                    var spend = CardSpendService.TopUp(x);
                    if (!spend.Success)
                    {
                        op.Status = "failed";
                        op.Message = spend.Message;
                        return op;
                    }
                    x.Status = spend.Status;
                    op.Status = "success";
                    op.Message = spend.Message;
                    op.Data = JsonConvert.SerializeObject(x, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getCardDepositDetail(string em, tblT_Card_Deposit x)
        {
            try
            {
                var uid = getUserId(em);
                
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                if (x.ID == null || x.ID == "")
                {
                    op.Status = "failed";
                    op.Message = "ID parameter cannot be null";
                    return op;
                }
                var data = db.tblT_Card_Deposit.Where(p => p.UserID == uid && p.ID == x.ID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Transaction is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get card deposit detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }


        public OutputModel getCardDepositList(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                if (x.ID == null || x.ID == "")
                {
                    op.Status = "failed";
                    op.Message = "ID parameter cannot be null";
                    return op;
                }
                var cd = db.tblT_Card.Where(p => p.ID == x.ID && p.UserID == uid).FirstOrDefault();
                if (cd == null)
                {
                    op.Status = "failed";
                    op.Message = "ID is not found";
                    return op;
                }
                
                var data = db.tblT_Card_Deposit.Where(p => p.CardNo == cd.CardNo && p.UserID == uid).OrderByDescending(p => p.DateTransaction).ToList();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Deposit is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get card transaction";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }



        public OutputModel cancelCardPurchase(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                if (x.ID == null || x.ID == "")
                {
                    op.Status = "failed";
                    op.Message = "ID parameter cannot be null";
                    return op;
                }

                var data = db.tblT_Card.Where(p => p.UserID == uid && p.ID == x.ID && p.Status == StatusModel.Created).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card is not found";
                    return op;
                }
                else
                {

                    data.Status = StatusModel.Cancelled;
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success cancel card transaction";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel cancelDepositTransaction(string em, tblT_Card_Deposit x)
        {
            try
            {
                var uid = getUserId(em);
                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                if (x.ID == null || x.ID == "")
                {
                    op.Status = "failed";
                    op.Message = "ID parameter cannot be null";
                    return op;
                }
                var data = db.tblT_Card_Deposit.Where(p => p.UserID == uid && p.ID == x.ID && p.Status == StatusModel.Created).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card deposit is not found";
                    return op;
                }
                else
                {

                    data.Status = StatusModel.Cancelled;
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success cancel deposit transaction";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }


        public OutputModel getCardPurchaseDetail(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);

                if (x == null)
                {
                    op.Status = "failed";
                    op.Message = "No valid parameters found";
                    return op;
                }

                var data = db.vw_Card.Where(p => p.UserID == uid && p.ID == x.ID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get card detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        static string decrypt(string strText)
        {
            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    var base64Encrypted = strText;

                    // server decrypting data with private key                    
                    rsa.FromXmlString(loadRsaPrivateKeyPem());

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
        private static string loadRsaPrivateKeyPem()
        {
            return KeyModel.WASABICARD_PRIVATE_KEY_XML;
        }
    }
}
