using Newtonsoft.Json;
using QryptoCard.INT.Model;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;
using QryptoCard.Sec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Web.UI;

namespace QryptoCard.INT.Script.Service.App.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "CardV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select CardV1Service.svc or CardV1Service.svc.cs at the Solution Explorer and start debugging.
    public class CardV1Service : ICardV1Service
    {
        DBEntities db = new DBEntities();
        OutputModel op = new OutputModel();

        string getUserId(string em)
        {
            var a = db.tblM_User.Where(p => p.Email == em).FirstOrDefault();
            return a.UserID;

        }

        public void testAPI()
        {
            try
            {
                //rebuid holder
                WCGetHolderRequestModel gh = new WCGetHolderRequestModel();
                gh.pageNum = 1;
                gh.pageSize = 100;
                var ck = WasabiCardService.getHolderList(gh);
                if (ck != null)
                {
                    var ls = db.tblM_Cardholder.Where(p => p.State == null).ToList();
                    for (int i = 0; i < ls.Count; i++)
                    {
                        var x = ck.data.records;
                        var id = Convert.ToInt64(ls[i].HolderID);
                        var z = x.Where(p => p.holderId == id).FirstOrDefault();
                        if (z != null)
                        {
                            ls[i].CountryStr = z.countryStr;
                            ls[i].State = z.state;
                            ls[i].StateStr = z.stateStr;
                            ls[i].TownStr = z.townStr;
                            ls[i].Status = z.status;
                            ls[i].StatusStr = z.statusStr;
                            db.SaveChanges();
                        }
                    }
                    
                }

                //get card type
                //var data = WasabiCardService.getCardType();
                //if (data != null)
                //{
                //    var x = data.data;
                //    for (int i = 0; i < x.Count; i++)
                //    {
                //        var id = x[i].cardTypeId;
                //        var ck = db.tblM_Card_Type.Where(p => p.CardTypeId == id).FirstOrDefault();
                //        if (ck == null)
                //        { 
                //            tblM_Card_Type c = new tblM_Card_Type();
                //            c.CardTypeId = id;
                //            c.Organization = x[i].organization;
                //            c.Country = x[i].country;
                //            c.BankCardBin = x[i].bankCardBin;
                //            c.Type = x[i].type;
                //            c.TypeStr = x[i].typeStr;
                //            c.CardName = x[i].cardName;
                //            c.CardDesc = x[i].cardDesc;
                //            c.CardPrice = x[i].cardPrice;
                //            c.OriginalCardPrice = x[i].cardPrice;
                //            c.CardPriceCurrency = x[i].cardPriceCurrency;
                //            c.OriginalCardPriceCurrency = x[i].cardPriceCurrency;
                //            for (int j = 0; j < x[i].support.Count; j++)
                //            {
                //                if (c.Support == "" || c.Support == null)
                //                    c.Support = x[i].support[j];
                //                else
                //                    c.Support = c.Support + "," + x[i].support[j];
                //            }
                //            for (int j = 0; j < x[i].supportHolderRegin.Count; j++)
                //            {
                //                if (c.SupportHolderRegin == "" || c.SupportHolderRegin == null)
                //                    c.SupportHolderRegin = x[i].supportHolderRegin[j];
                //                else
                //                    c.SupportHolderRegin = c.SupportHolderRegin + "," + x[i].supportHolderRegin[j];
                //            }
                //            for (int j = 0; j < x[i].supportHolderAreaCode.Count; j++)
                //            {
                //                if (c.SupportHolderAreaCode == "" || c.SupportHolderAreaCode == null)
                //                    c.SupportHolderAreaCode = x[i].supportHolderAreaCode[j];
                //                else
                //                    c.SupportHolderAreaCode = c.SupportHolderAreaCode + "," + x[i].supportHolderAreaCode[j];
                //            }
                //            if (x[i].needCardHolder)
                //                c.NeedCardHolder = 1;
                //            else
                //                c.NeedCardHolder = 0;

                //            if (x[i].needDepositForActiveCard)
                //                c.NeedDepositForActiveCard = 1;
                //            else
                //                c.NeedDepositForActiveCard = 0;

                //            c.DepositAmountMinQuotaForActiveCard = x[i].depositAmountMinQuotaForActiveCard;
                //            c.DepositAmountMaxQuotaForActiveCard = x[i].depositAmountMaxQuotaForActiveCard;
                //            c.FiatCurrency = x[i].fiatCurrency;
                //            c.MaxCount = x[i].maxCount;
                //            c.Status = x[i].status;
                //            if (x[i].status == "offline")
                //                c.isActive = 0;
                //            else
                //                c.isActive = 1;

                //            c.RechargeMinQuota = x[i].extFieldVO.rechargeCurrencyInfos[0].rechargeMinQuota;
                //            c.RechargeMaxQuota = x[i].extFieldVO.rechargeCurrencyInfos[0].rechargeMaxQuota;
                //            c.OriginalRechargeFee = x[i].extFieldVO.rechargeCurrencyInfos[0].rechargeFee;
                //            c.OriginalRechargeFeeRate = x[i].extFieldVO.rechargeCurrencyInfos[0].rechargeFeeRate;
                //            c.RechargeFee = c.OriginalRechargeFee;
                //            c.RechargeFeeRate = c.OriginalRechargeFeeRate;

                //            c.DateCreated = DateTime.Now;

                //            db.tblM_Card_Type.Add(c);
                //            db.SaveChanges();

                //        }
                //    }
                //}

                //create holder
                //WCCreateHolderRequestModel ho = new WCCreateHolderRequestModel();
                //ho.cardTypeId = 111015;
                //ho.email = "syapril@windowslive.com";
                //ho.firstName = "Syapril";
                //ho.lastName = "Suryanto";
                //ho.address = "Block 94, Shap Li Mok Estate";
                //ho.country = "HK";
                //ho.mobile = "66270257";
                //ho.areaCode = "+852";
                //ho.birthday = "2000-12-12";
                //ho.postCode = "82108";
                //ho.town = "HK_KKC_1";
                //var ch = WasabiCardService.createHolder(ho);
                //if (ch != null)
                //{
                //    if (ch.data.status == "pass_audit")
                //    {
                //        tblM_Cardholder cho = new tblM_Cardholder();
                //        cho.ID = Guid.NewGuid().ToString();
                //        cho.HolderID = ch.data.holderId;

                //        WCGetHolderRequestModel gh = new WCGetHolderRequestModel();
                //        gh.pageNum = 1;
                //        gh.pageSize = 100;
                //        var ck = WasabiCardService.getHolderList(gh);
                //        if (ck != null)
                //        {
                //            var x = ck.data.records;
                //            var z = x.Where(p => p.holderId == cho.HolderID).FirstOrDefault();
                //            if (z != null)
                //            {
                //                cho.Address = z.address;
                //                cho.AreaCode = z.areaCode;
                //                cho.Birthday = z.birthday;
                //                cho.CardTypeId = ho.cardTypeId;
                //                cho.Country = z.country;
                //                cho.CountryStr = z.countryStr;
                //                cho.DateCreated = DateTime.Now;
                //                cho.Email = z.email;
                //                cho.FirstName = z.firstName;
                //                cho.LastName = z.lastName;
                //                cho.Mobile = z.mobile;
                //                cho.PostCode = z.postCode;
                //                cho.State = z.state;
                //                cho.StateStr = z.stateStr;
                //                cho.Status = z.status;
                //                cho.StatusStr = z.statusStr;
                //                cho.Town = z.town;
                //                cho.TownStr = z.townStr;
                //                cho.isActive = 1;
                //                db.tblM_Cardholder.Add(cho);
                //                db.SaveChanges();
                //            }
                //        }
                //    }
                //}


                //create card with holder
                //WCOpenCardWithHolderRequestModel cwh = new WCOpenCardWithHolderRequestModel();
                //cwh.merchantOrderNo = 1.ToString("00000000000000000000");
                //cwh.cardTypeId = 111015;
                //cwh.holderId = 57370;
                //cwh.amount = 50;

                //var ca = WasabiCardService.openCardWithHolder(cwh);
                //if (ca != null)
                //{
                //    if (ca.code == 200)
                //    {
                //        var cdt = ca.data[0];
                //        tblT_Card cd = new tblT_Card();
                //        cd.ID = cwh.merchantOrderNo;
                //        cd.OrderNo = cdt.orderNo;
                //        cd.BaseAmount = Convert.ToDouble(cdt.amount);
                //        cd.BaseFee = Convert.ToDouble(cdt.fee);
                //        cd.ReceivedCurrency = cdt.receivedCurrency;
                //        cd.Currency = cdt.currency;
                //        cd.ReceivedAmount = cwh.amount;
                //        cd.Status = StatusModel.InProgress;
                //        cd.Param4 = cdt.status;
                //        cd.Param5 = cdt.type;
                //        cd.DateCreated = DateTime.Now;
                //        cd.isActive = 1;

                //        cd.CardTypeId = cwh.cardTypeId;
                //        cd.isNeedCardholder = 1;
                //        cd.HolderID = cwh.holderId;
                //        db.tblT_Card.Add(cd);
                //        db.SaveChanges();

                //    }
                //}


                //WCCardTransactionRequestModel gh = new WCCardTransactionRequestModel();
                //gh.pageNum = 1;
                //gh.pageSize = 100;
                //gh.type = "create";
                //var ck = WasabiCardService.getCardTransaction(gh);
                //if (ck != null)
                //{
                //    if (ck.code == 200)
                //    {
                //        var x = ck.data.records.Where(p => p.orderNo == "1885971484952256512").FirstOrDefault();
                //        if (x != null)
                //        {
                //            var cd = db.tblT_Card.Where(p => p.OrderNo == "1885971484952256512").FirstOrDefault();
                //            cd.CardNo = x.cardNo;
                //            cd.Status = x.status;
                //            db.SaveChanges();

                //            WCCardInfoRequestModel ci = new WCCardInfoRequestModel();
                //            ci.cardNo = x.cardNo;
                //            ci.onlySimpleInfo = false;
                //            var z = WasabiCardService.getCardInfo(ci);
                //            if (z != null)
                //            {
                //                if (z.code == 200)
                //                {
                //                    cd.CardNumber = z.data.cardNumber;
                //                    cd.CVV = z.data.cvv;
                //                    cd.ValidPeriod = z.data.validPeriod;

                //                    tblT_Card_Balance cb = new tblT_Card_Balance();
                //                    cb.CardNo = cd.CardNo;
                //                    cb.Currency = z.data.balanceInfo.currency;
                //                    cb.Amount = Convert.ToDouble(z.data.balanceInfo.amount);
                //                    cb.ID = cd.ID;
                //                    cb.UsedAmount = Convert.ToDouble(z.data.balanceInfo.usedAmount);
                //                    db.tblT_Card_Balance.Add(cb);

                //                    db.SaveChanges();
                //                }
                //            }
                //        }
                //    }
                //}
                return;

            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return;
        }

        public OutputModel CardType(tblM_Card_Type x)
        {
            try
            {
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

        public OutputModel getCardTypeById(tblM_Card_Type x)
        {
            try
            {
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

        public void createCardHolder(string uid, long cardtypeid, string fn, string ln, string em)
        {
            var adr = db.tblM_Address_Generator.Where(p => p.isUsed == 0 && p.isActive == 1 && p.CityCode != null).OrderByDescending(p => p.ID).FirstOrDefault();
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
                    if (data == null)
                    {
                        op.Status = "failed";
                        op.Message = "You have pending transaction on this card";
                        return op;
                    }

                    var qry = db.tblM_User_Crypto_Deposit.Where(p => p.UserID == uid && p.isActive == 1).FirstOrDefault();
                    if (qry == null)
                    {
                        op.Status = "failed";
                        op.Message = "No crypto address found for payment";
                        return op;
                    }

                    x.AddressID = qry.ID;
                    x.Address = qry.Address;
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

                    if (x.UserID == "59da7833-7d71-4821-be7e-bd1bb2a36510")
                        x.FeeInPercentage = Convert.ToDouble(2);
                    else
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
                    x.Status = StatusModel.Created;
                    x.isActive = 0;
                    x.DateCreated = DateTime.Now;
                    x.DateExpired = x.DateCreated.Value.AddHours(1);


                    var st = db.tblM_Setting_Counter.Where(p => p.ID == 1).FirstOrDefault();
                    st.Value += 1;
                    x.ID = "QRYCRDBUY" + st.Value.Value.ToString("000000000000");

                    db.tblT_Card.Add(x);
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success create card transaction";
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

        public OutputModel getCardList(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);

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

        public OutputModel getCardDetail(string em, vw_Card x)
        {
            try
            {
                var uid = getUserId(em);

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

        public OutputModel getCardBalance(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);

                var data = db.tblT_Card_Balance.Where(p => p.ID == x.ID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Card is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success get card balance";
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

        public OutputModel getCardTransaction(string em, tblT_Card x)
        {
            try
            {
                var uid = getUserId(em);

                var data = db.tblT_Card_Transaction.Where(p => p.CardNo == x.CardNo && (p.Status == "succeed" || p.Status == "success" || p.Status == "authorized")).OrderByDescending(p => p.TransactionTime).Take(20).ToList();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Transaction is not found";
                    return op;
                }
                else
                {
                    op.Status = "success";
                    op.Message = "Success create card transaction";
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

                var data = db.tblT_Card_Transaction.Where(p => p.ID == x.ID).FirstOrDefault();

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

                var data = db.tblT_Card_Deposit.Where(p => p.UserID == uid && p.Amount == x.Amount && p.Status == StatusModel.Created).FirstOrDefault();

                if (data != null)
                {
                    op.Status = "failed";
                    op.Message = "You have pending deposit with same amount";
                    return op;
                }
                else
                {
                    var card = db.tblT_Card.Where(p => p.CardNo == x.CardNo).FirstOrDefault();
                    if (card == null)
                    {
                        op.Status = "failed";
                        op.Message = "Card is not found";
                        return op;
                    }

                    //x.FeeInPercentage = Convert.ToDouble(ct.RechargeFeeRate);

                    //var comm = db.tblM_User_Fee.Where(p => p.UserID == x.UserID).FirstOrDefault();
                    //if (comm != null)
                    //{
                    //    x.FeeInPercentage = comm.Fee;
                    //}
                    //else
                    //{
                    //    if (card.HolderID == null)
                    //    {
                    //        var f = db.tblM_Setting.Where(p => p.ID == 2).FirstOrDefault();
                    //        if (f != null)
                    //        {
                    //            x.FeeInPercentage = f.Value;
                    //        }
                    //        else
                    //            x.FeeInPercentage = 1;
                    //    }
                    //    else
                    //    {
                    //        var f = db.tblM_Setting.Where(p => p.ID == 3).FirstOrDefault();
                    //        if (f != null)
                    //        {
                    //            x.FeeInPercentage = f.Value;
                    //        }
                    //        else
                    //            x.FeeInPercentage = 1;
                    //    }
                    //}



                    x.UserID = uid;

                    var ct = db.tblM_Card_Type.Where(p => p.CardTypeId == card.CardTypeId).FirstOrDefault();
                    if (x.UserID == "59da7833-7d71-4821-be7e-bd1bb2a36510")
                        x.FeeInPercentage = Convert.ToDouble(2);
                    else
                        x.FeeInPercentage = Convert.ToDouble(ct.RechargeFeeRate);

                    x.Currency = "USD";
                    x.Fee = (Convert.ToDouble(x.FeeInPercentage) / 100) * x.Amount.Value;
                    x.Total = Math.Round((x.Amount.Value + x.Fee.Value), 2);
                    x.ReceivedAmount = x.Amount.Value;
                    x.Status = StatusModel.Created;
                    x.DateTransaction = DateTime.Now;
                    x.DateExpired = x.DateTransaction.Value.AddHours(1);

                    var qry = db.tblM_User_Crypto_Deposit.Where(p => p.UserID == uid && p.isActive == 1).FirstOrDefault();
                    if (qry == null)
                    {
                        op.Status = "failed";
                        op.Message = "No crypto address found for payment";
                        return op;
                    }

                    x.AddressID = qry.ID;
                    x.Address = qry.Address;

                    var st = db.tblM_Setting_Counter.Where(p => p.ID == 2).FirstOrDefault();
                    st.Value += 1;
                    x.ID = "QRYCRDPST" + st.Value.Value.ToString("000000000000");

                    db.tblT_Card_Deposit.Add(x);
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success create card deposit";
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

                var data = db.tblT_Card_Deposit.Where(p => p.CardNo == x.CardNo && p.UserID == uid).OrderByDescending(p => p.DateTransaction).ToList();

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



        public OutputModel cancelCardTransaction(string em, vw_Card x)
        {
            try
            {
                var uid = getUserId(em);

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


        public void recreateCardHolder(int holderid)
        {
            var data = db.tblM_Cardholder.Where(p => p.HolderID == holderid).FirstOrDefault();
            if (data != null)
            {
                WCCreateHolderRequestModel ho = new WCCreateHolderRequestModel();
                ho.cardTypeId = 111028;
                ho.email = data.Email;
                ho.firstName = data.FirstName;
                ho.lastName = data.LastName;
                ho.address = data.Address;
                ho.country = "US";
                ho.mobile = data.Mobile;
                ho.areaCode = "+1";
                ho.birthday = generateBirthdate();
                ho.postCode = data.PostCode;
                ho.town = data.Town;
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
                                cho.UserID = data.UserID;
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

                                data.isActive = 0;

                                db.SaveChanges();
                            }
                        }
                    }
                }
            }
            
            return;
        }

        public void checkCard(string cardNo)
        {
            try
            {
                var qqq = new WCCardInfoSensitiveRequestModel();
                qqq.cardNo = cardNo;
                var res = WasabiCardService.getCardInfoSensitive(qqq);

                

                //WCCardInfoRequestModel req = new WCCardInfoRequestModel();
                //req.cardNo = cardNo;
                //req.onlySimpleInfo = false;
                //var res = WasabiCardService.getCardInfo(req);

                var ed = decrypt(res.data.expireDate);
                var cv = decrypt(res.data.cvv);
                var cr = db.tblT_Card.Where(p => p.CardNo == cardNo).FirstOrDefault();
                if (cr != null)
                {
                    cr.CardNumber = decrypt(res.data.cardNumber);
                    cr.CVV = res.data.cvv;
                    cr.ValidPeriod = res.data.expireDate;
                    //db.SaveChanges();

                    //cd.ValidPeriod = ed;
                    //cd.CVV = cv;
                    //cd.CardNumber = res.data.cardNumber;
                    if (cr.Status == "open card")
                    {
                        cr.Status = "success";
                        cr.isActive = 1;
                    }
                    db.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                var z = ex.Message;
            }
            return;
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
