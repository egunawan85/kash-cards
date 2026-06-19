using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "AutomationV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select AutomationV1Service.svc or AutomationV1Service.svc.cs at the Solution Explorer and start debugging.
    public class AutomationV1Service : IAutomationV1Service
    {
        DBEntities db = new DBEntities();
        public void InsertAddress(List<List<string>> data)
        {
            var xx = data[1];

            tblM_Address_Generator adr = new tblM_Address_Generator();
            adr.Street = xx[0];
            adr.City = xx[1];
            adr.State = xx[2];
            adr.PostalCode = xx[3];
            adr.PhoneNumber = xx[4].Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
            adr.Country = xx[5];
            adr.Latitude = xx[7];
            adr.Longitude = xx[6];
            var ck = db.tblM_Country_City.Where(p => p.City == adr.City).FirstOrDefault();
            if (ck != null)
            {
                adr.CityCode = ck.Code;
                adr.isActive = 1;
            }
            else
                adr.isActive = 0;
            adr.isUsed = 0;
            db.tblM_Address_Generator.Add(adr);
            db.SaveChanges();

            return;
        }
    }
}
