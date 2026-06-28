using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "CardV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select CardV1Service.svc or CardV1Service.svc.cs at the Solution Explorer and start debugging.
    public class CardV1Service : ICardV1Service
    {
        DBEntities db = new DBEntities();
        OutputModel op = new OutputModel();

        string getAdminId(string em)
        {
            var a = db.tblM_Admin.Where(p => p.Email == em).FirstOrDefault();
            return a.AdminID;
        }

        string getRole(string em)
        {
            var a = db.vw_Admin.Where(p => p.Email == em).FirstOrDefault();
            return a == null ? null : a.Role;
        }

        // Allowlist: ONLY Owner/Admin may change financial settings (card price, deposit fee).
        // Deny-by-default (case/whitespace-insensitive) so an unknown or variant role string can't slip through.
        bool isDeniedFinanceMutation(string em)
        {
            var role = (getRole(em) ?? "").Trim();
            return !(role.Equals(RoleModel.Owner, StringComparison.OrdinalIgnoreCase)
                  || role.Equals(RoleModel.Admin, StringComparison.OrdinalIgnoreCase));
        }

        // Allowlist for admin-console reads (all-users card/deposit data): permit only a
        // recognized admin role. Deny-by-default so an unidentifiable caller (no/unknown
        // role) cannot pull cross-user data — these endpoints previously had no role gate.
        bool isDeniedAdminRead(string em)
        {
            var role = (getRole(em) ?? "").Trim();
            return !(role.Equals(RoleModel.Owner, StringComparison.OrdinalIgnoreCase)
                  || role.Equals(RoleModel.Admin, StringComparison.OrdinalIgnoreCase)
                  || role.Equals(RoleModel.Approver, StringComparison.OrdinalIgnoreCase)
                  || role.Equals(RoleModel.Signer, StringComparison.OrdinalIgnoreCase)
                  || role.Equals(RoleModel.Viewer, StringComparison.OrdinalIgnoreCase));
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

        public OutputModel getActiveCard(string em)
        {
            try
            {
                if (isDeniedAdminRead(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                var data = db.vw_Card.Where(p => p.Status == "success").OrderByDescending(p => p.DateCreated).ToList();

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

        public OutputModel getCardListAll(string em, vw_Card x)
        {
            try
            {
                if (isDeniedAdminRead(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                var data = db.vw_Card.OrderByDescending(p => p.DateCreated).ToList();

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


        public OutputModel getCardPurchaseFilter(string em, CardFilterModel fil)
        {
            try
            {
                if (isDeniedAdminRead(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                var date = Convert.ToDateTime("1/1/0001 12:00:00 AM");
                var data = db.vw_Card.OrderByDescending(p => p.DateCreated).ToList();
                if (fil.CardTypeId != -1)
                {
                    data = data.Where(p => p.CardTypeId == fil.CardTypeId).ToList();
                }
                if (fil.Status != "all")
                {
                    data = data.Where(p => p.Status == fil.Status).ToList();
                }
                if (fil.StartDate == null)
                {
                    fil.StartDate = DateTime.Today;
                    fil.EndDate = fil.StartDate.Value.AddHours(23).AddMinutes(59);
                    data = data.Where(p => p.DateCreated >= fil.StartDate && p.DateCreated <= fil.EndDate).ToList();
                }
                else
                {
                    if (fil.EndDate == null)
                    {
                        fil.EndDate = fil.StartDate.Value.AddHours(23).AddMinutes(59);
                        data = data.Where(p => p.DateCreated >= fil.StartDate && p.DateCreated <= fil.EndDate).ToList();
                    }
                    else
                    {
                        fil.EndDate = fil.EndDate.Value.AddHours(23).AddMinutes(59);
                        data = data.Where(p => p.DateCreated >= fil.StartDate && p.DateCreated <= fil.EndDate).ToList();
                    }
                }


                op.Status = "success";
                op.Message = "Success get card purchase list";
                op.Data = JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getDepositTrxFilter(string em, DepositFilterModel fil)
        {
            try
            {
                if (isDeniedAdminRead(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                var date = Convert.ToDateTime("1/1/0001 12:00:00 AM");
                var data = db.vw_Card_Deposit.OrderByDescending(p => p.DateTransaction).ToList();
                if (fil.CardNo != "all")
                {
                    data = data.Where(p => p.CardNo == fil.CardNo).ToList();
                }
                if (fil.Status != "all")
                {
                    data = data.Where(p => p.Status == fil.Status).ToList();
                }
                //if (fil.StartDate == null)
                //{
                //    fil.StartDate = DateTime.Today;
                //    fil.EndDate = fil.StartDate.Value.AddHours(23).AddMinutes(59);
                //    data = data.Where(p => p.DateTransaction >= fil.StartDate && p.DateTransaction <= fil.EndDate).ToList();
                //}
                //else
                //{
                //    if (fil.EndDate == null)
                //    {
                //        fil.EndDate = fil.StartDate.Value.AddHours(23).AddMinutes(59);
                //        data = data.Where(p => p.DateTransaction >= fil.StartDate && p.DateTransaction <= fil.EndDate).ToList();
                //    }
                //    else
                //    {
                //        fil.EndDate = fil.EndDate.Value.AddHours(23).AddMinutes(59);
                //        data = data.Where(p => p.DateTransaction >= fil.StartDate && p.DateTransaction <= fil.EndDate).ToList();
                //    }
                //}


                op.Status = "success";
                op.Message = "Success get deposit transaction list";
                op.Data = JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateCardPrice(string em, tblM_Card_Type x)
        {
            try
            {
                if (isDeniedFinanceMutation(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                var data = db.tblM_Card_Type.Where(p => p.ID == x.ID && p.CardTypeId == x.CardTypeId).FirstOrDefault();
                if (data != null)
                {
                    data.CardPrice = x.CardPrice;
                    db.SaveChanges();

                    op.Message = "Success update card price to " + data.CardPrice + " " + data.CardPriceCurrency;
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                }
                else
                {
                    op.Message = "Failed update price, id not found";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateCardDepositFee(string em, tblM_Card_Type x)
        {
            try
            {
                if (isDeniedFinanceMutation(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                var data = db.tblM_Card_Type.Where(p => p.ID == x.ID && p.CardTypeId == x.CardTypeId).FirstOrDefault();
                if (data != null)
                {
                    var oldfee = Convert.ToDouble(data.RechargeFee);
                    var newfee = Convert.ToDouble(x.RechargeFee);
                    var ck = db.tblM_User_Fee.Where(p => p.Fee == oldfee).ToList();
                    for (int i = 0; i < ck.Count; i++)
                    {
                        ck[i].Fee = newfee;
                    }


                    data.RechargeFeeRate = x.RechargeFeeRate;
                    db.SaveChanges();

                    op.Message = "Success update card deposit fee to " + data.RechargeFeeRate + "%";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                }
                else
                {
                    op.Message = "Failed update ddeposit fee, id not found";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        // ---- Global card pricing (tblM_Setting: CardPrice + CardDepositFeeRate) -------------
        // The customer-facing price/fee are now GLOBAL settings (overlaid onto the live catalog
        // by CardCatalogService), not per-card-type columns. These two endpoints read/update
        // those settings; the admin card page's pricing panel drives them.

        public OutputModel getCardPricing(string em)
        {
            try
            {
                if (isDeniedAdminRead(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                double price = ReadSetting(CardCatalogService.SettingCardPrice, CardCatalogService.DefaultCardPrice);
                double fee = ReadSetting(CardCatalogService.SettingDepositFeeRate, CardCatalogService.DefaultDepositFeeRate);

                // Reuse tblM_Card_Type as the transport shape (already a shared data contract):
                // CardPrice = global card price, RechargeFeeRate = global deposit fee %.
                var dto = new tblM_Card_Type
                {
                    CardPrice = price.ToString(CultureInfo.InvariantCulture),
                    CardPriceCurrency = "USD",
                    RechargeFeeRate = fee.ToString(CultureInfo.InvariantCulture),
                };

                op.Status = "success";
                op.Message = "Success get card pricing";
                op.Data = JsonConvert.SerializeObject(dto, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateCardPricing(string em, tblM_Card_Type x)
        {
            try
            {
                if (isDeniedFinanceMutation(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                // Parse defensively (InvariantCulture) and reject negatives — these drive the
                // money path (every buy/top-up reads them), so a bad value must not be persisted.
                double price, fee;
                if (!double.TryParse(x.CardPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out price) || price < 0)
                {
                    op.Status = "failed";
                    op.Message = "Card price must be a number greater than or equal to 0";
                    return op;
                }
                if (!double.TryParse(x.RechargeFeeRate, NumberStyles.Any, CultureInfo.InvariantCulture, out fee) || fee < 0)
                {
                    op.Status = "failed";
                    op.Message = "Deposit fee must be a number greater than or equal to 0";
                    return op;
                }

                UpsertSetting(CardCatalogService.SettingCardPrice, price);
                UpsertSetting(CardCatalogService.SettingDepositFeeRate, fee);
                db.SaveChanges();

                op.Status = "success";
                op.Message = "Success update card pricing";
                op.Data = JsonConvert.SerializeObject(x, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        double ReadSetting(string name, double fallback)
        {
            var s = db.tblM_Setting.FirstOrDefault(p => p.Name == name);
            return (s != null && s.Value.HasValue) ? s.Value.Value : fallback;
        }

        void UpsertSetting(string name, double value)
        {
            var s = db.tblM_Setting.FirstOrDefault(p => p.Name == name);
            if (s == null)
            {
                s = new tblM_Setting { Name = name, Value = value, DateCreated = DateTime.Now };
                db.tblM_Setting.Add(s);
            }
            else
            {
                s.Value = value;
            }
        }
    }
}
