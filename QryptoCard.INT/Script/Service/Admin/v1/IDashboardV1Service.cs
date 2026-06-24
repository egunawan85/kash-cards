using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IDashboardV1Service" in both code and config file together.
    [ServiceContract]
    public interface IDashboardV1Service
    {
        [OperationContract]
        OutputModel getDashboardData(string em, DashboardAdminModel a);
        [OperationContract]
        OutputModel get10ActiveCardTransaction(string em);
    }
}
