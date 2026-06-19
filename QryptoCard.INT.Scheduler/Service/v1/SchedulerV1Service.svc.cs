using QryptoCard.INT.Scheduler.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Scheduler.Service.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SchedulerV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select SchedulerV1Service.svc or SchedulerV1Service.svc.cs at the Solution Explorer and start debugging.
    public class SchedulerV1Service : ISchedulerV1Service
    {
        DBEntities db = new DBEntities();

        public void checkExpiredTransaction()
        {
            try
            {
                int co = 0;
                var data = db.tblT_Card.Where(p => p.Status == StatusModel.Created).ToList();
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].DateExpired < DateTime.Now)
                    {
                        data[i].Status = StatusModel.Expired;
                        data[i].DateModified = DateTime.Now;

                        db.SaveChanges();
                        co++;
                    }
                }


                var inv = db.tblT_Card_Deposit.Where(p => p.Status == StatusModel.Created).ToList();
                for (int i = 0; i < inv.Count; i++)
                {
                    if (inv[i].DateExpired < DateTime.Now)
                    {
                        inv[i].Status = StatusModel.Expired;
                        
                        db.SaveChanges();
                        co++;
                    }
                }

                //op.Message = "Berhasil jalankan scheduler for topup : " + co.ToString();
                //op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                //op.Status = "success";
            }
            catch (Exception ex)
            {
                //op.Data = null;
                //op.Message = ex.Message;
                //op.Status = "error";
            }
            return;
        }

    }
}
