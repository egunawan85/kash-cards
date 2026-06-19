using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IAutomationV1Service" in both code and config file together.
    [ServiceContract]
    public interface IAutomationV1Service
    {
        [OperationContract]
        void InsertAddress(List<List<string>> data);
    }
}
