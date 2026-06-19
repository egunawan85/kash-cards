using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;
using QryptoCard.Sec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ManualService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select ManualService.svc or ManualService.svc.cs at the Solution Explorer and start debugging.
    public class ManualService : IManualService
    {
        DBEntities db = new DBEntities();

        public void doCommissionCard()
        {
            var crd = db.tblT_Card.Where(p => p.Status == "success").ToList();
            for (int i = 0; i < crd.Count; i++)
            {
                var id = crd[i].UserID;
                var usr = db.tblM_User.Where(p => p.UserID == id).FirstOrDefault();
                if (usr.InvitedBy != null)
                {
                    var trxid = crd[i].ID;
                    var ckc = db.tblT_Commission.Where(p => p.TransactionID == trxid).FirstOrDefault();
                    if (ckc == null)
                    {
                        var ucomm = db.tblM_User_Commission.Where(p => p.UserID == usr.InvitedBy).FirstOrDefault();
                        if (ucomm != null)
                        {
                            var comm = new tblT_Commission();
                            comm.UserID = usr.InvitedBy;
                            comm.CommisionID = Guid.NewGuid().ToString();
                            comm.TransactionID = trxid;
                            comm.DateCreated = crd[i].DateCreated;
                            comm.Commission = (ucomm.Commission / 100) * crd[i].InitialDeposit;

                            var bal = db.tblM_User_Balance.Where(p => p.UserID == usr.InvitedBy).FirstOrDefault();

                            tblH_User_Balance hbl = new tblH_User_Balance();
                            hbl.TransactionID = crd[i].ID;
                            hbl.UserID = usr.InvitedBy;
                            hbl.BalanceID = bal.BalanceID;
                            hbl.Type = "Initial Deposit";
                            hbl.BalancePrevious = bal.Balance;
                            hbl.Amount = Convert.ToDecimal(crd[i].InitialDeposit);
                            hbl.Commision = Convert.ToDecimal(comm.Commission);
                            hbl.CommisionInPercentage = ucomm.Commission;

                            bal.Balance += Convert.ToDecimal(comm.Commission);

                            hbl.Balance = bal.Balance;
                            hbl.BalanceHold = 0;
                            hbl.CreatedDate = DateTime.Now;

                            db.tblH_User_Balance.Add(hbl);
                            db.SaveChanges();

                        }


                    }
                }
            }
        }
        public void doCommissionDeposit()
        {
            var crd = db.tblT_Card_Deposit.Where(p => p.Status == "success").ToList();
            for (int i = 0; i < crd.Count; i++)
            {
                var id = crd[i].UserID;
                var usr = db.tblM_User.Where(p => p.UserID == id).FirstOrDefault();
                if (usr.InvitedBy != null)
                {
                    var trxid = crd[i].ID;
                    var ckc = db.tblT_Commission.Where(p => p.TransactionID == trxid).FirstOrDefault();
                    if (ckc == null)
                    {
                        var ucomm = db.tblM_User_Commission.Where(p => p.UserID == usr.InvitedBy).FirstOrDefault();
                        if (ucomm != null)
                        {
                            var comm = new tblT_Commission();
                            comm.UserID = usr.InvitedBy;
                            comm.CommisionID = Guid.NewGuid().ToString();
                            comm.TransactionID = trxid;
                            comm.DateCreated = crd[i].DateTransaction;
                            comm.Commission = (ucomm.Commission / 100) * crd[i].Amount;

                            var bal = db.tblM_User_Balance.Where(p => p.UserID == usr.InvitedBy).FirstOrDefault();

                            tblH_User_Balance hbl = new tblH_User_Balance();
                            hbl.TransactionID = crd[i].ID;
                            hbl.UserID = usr.InvitedBy;
                            hbl.BalanceID = bal.BalanceID;
                            hbl.Type = "Initial Deposit";
                            hbl.BalancePrevious = bal.Balance;
                            hbl.Amount = Convert.ToDecimal(crd[i].Amount);
                            hbl.Commision = Convert.ToDecimal(comm.Commission);
                            hbl.CommisionInPercentage = ucomm.Commission;

                            bal.Balance += Convert.ToDecimal(comm.Commission);

                            hbl.Balance = bal.Balance;
                            hbl.BalanceHold = 0;
                            hbl.CreatedDate = DateTime.Now;

                            db.tblH_User_Balance.Add(hbl);
                            db.SaveChanges();

                        }


                    }
                }
            }
        }


        public void checkBalance()
        {
            var x = WasabiCardService.getAccountInfo();
            return;
        }

        public void checkCardInfo()
        {
            var e = new WCCardInfoRequestModel();
            var x = WasabiCardService.getCardInfo(e);
            return;
        }

        public void cancelCard(string cardno)
        {
            var e = new WCCancelCardRequestModel();
            e.cardNo = cardno;
            e.merchantOrderNo = Guid.NewGuid().ToString();
            var x = WasabiCardService.cancelCard(e);
            return;
        }

        public void generateAPI(string userid)
        {
            var ap = db.tblM_User_API.Where(p => p.UserID == userid).FirstOrDefault();
            if (ap == null)
            {
                tblM_User_API api = new tblM_User_API();
                api.UserID = userid;
                api.DateCreated = DateTime.Now;
                api.APIKey = Guid.NewGuid().ToString();
                api.SecretKey = Sec.Secure.EncryptDB(api.APIKey);
                db.tblM_User_API.Add(api);
                db.SaveChanges();
                var sec = Secure.DBtoAPP(api.SecretKey);
            }
            return;
        }
    }
}
