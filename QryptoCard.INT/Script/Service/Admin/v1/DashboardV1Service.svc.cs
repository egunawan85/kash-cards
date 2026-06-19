using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "DashboardV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select DashboardV1Service.svc or DashboardV1Service.svc.cs at the Solution Explorer and start debugging.
    public class DashboardV1Service : IDashboardV1Service
    {
        DBEntities db = new DBEntities();
        OutputModel op = new OutputModel();

        string getAdminId(string em)
        {
            var a = db.tblM_Admin.Where(p => p.Email == em).FirstOrDefault();
            return a.AdminID;
        }

        //total deposit card sold user registered

        public OutputModel getDashboardData(DashboardAdminModel a)
        {
            try
            {
                DashboardAdminModel x = new DashboardAdminModel();
                x.TotalCards = 0;
                x.TotalUsers = 0;
                x.TotalDeposit = 0;

                var crd = db.tblT_Card.Where(p => p.Status == "success").ToList();
                if (crd.Count > 0)
                    x.TotalCards = crd.Count;

                var comm = db.tblM_User.Where(p => p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).ToList();
                if (comm.Count > 0)
                    x.TotalUsers = comm.Count;

                var rate = db.tblT_Card_Deposit.Where(p => p.Status == "success").ToList();
                if (rate != null)
                    x.TotalDeposit = rate.Sum(p => p.Amount).Value;

                op.Status = "success";
                op.Message = "Success get dashboard data";
                op.Data = JsonConvert.SerializeObject(x, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel get10ActiveCardTransaction()
        {
            try
            {

                var data = db.vw_Card.OrderByDescending(p => p.DateCreated).Take(10).ToList();
                if (data.Count != 0)
                {
                    op.Message = "Success get transaction list";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                }
                else
                {
                    op.Message = "No transaction found";
                    op.Status = "failed";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }
    }
}
