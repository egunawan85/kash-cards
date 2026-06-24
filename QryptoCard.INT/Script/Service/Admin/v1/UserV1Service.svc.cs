using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "UserV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select UserV1Service.svc or UserV1Service.svc.cs at the Solution Explorer and start debugging.
    public class UserV1Service : IUserV1Service
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

        // Allowlist: ONLY Owner/Admin may change financial settings (fees, commissions, prices).
        // Deny-by-default (case/whitespace-insensitive) so an unknown or variant role string can't slip through.
        bool isDeniedFinanceMutation(string em)
        {
            var role = (getRole(em) ?? "").Trim();
            return !(role.Equals(RoleModel.Owner, StringComparison.OrdinalIgnoreCase)
                  || role.Equals(RoleModel.Admin, StringComparison.OrdinalIgnoreCase));
        }

        public OutputModel getUser(tblM_User x)
        {
            try
            {
                var data = db.tblM_User.Where(p => p.isActive == 1).ToList();
                for (int i = 0; i < data.Count; i++)
                {
                    var id = data[i].UserID;
                    data[i].TotalCard = db.tblT_Card.Where(p => p.UserID == id && p.Status == "success").Count();
                }
                op.Message = "Success retrieve commission";
                op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                op.Status = "success";
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getUserCommissionList(vw_User_Commission x)
        {
            try
            {
                var data = db.vw_User_Commission.ToList();

                op.Message = "Success retrieve commission";
                op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                op.Status = "success";
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateUserCommission(string em, tblM_User_Commission x)
        {
            try
            {
                if (isDeniedFinanceMutation(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                var data = db.tblM_User_Commission.Where(p => p.ID == x.ID && p.UserID == x.UserID).FirstOrDefault();
                if (data != null)
                {
                    data.Commission = x.Commission;
                    db.SaveChanges();

                    op.Message = "Success update commission";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                }
                else
                {
                    op.Message = "Failed update commission, id not found";
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

        public OutputModel getUserFeeList(vw_User_Fee x)
        {
            try
            {
                var data = db.vw_User_Fee.ToList();

                op.Message = "Success retrieve fee";
                op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                op.Status = "success";
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateUserFee(string em, tblM_User_Fee x)
        {
            try
            {
                if (isDeniedFinanceMutation(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorized to perform this action";
                    return op;
                }

                var data = db.tblM_User_Fee.Where(p => p.ID == x.ID && p.UserID == x.UserID).FirstOrDefault();
                if (data != null)
                {
                    data.Fee = x.Fee;
                    db.SaveChanges();

                    op.Message = "Success update fee";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                }
                else
                {
                    op.Message = "Failed update fee, id not found";
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
    }
}
