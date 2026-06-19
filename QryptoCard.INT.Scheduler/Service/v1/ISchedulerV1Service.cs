using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Scheduler.Service.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ISchedulerV1Service" in both code and config file together.
    [ServiceContract]
    public interface ISchedulerV1Service
    {
        [OperationContract]
        void checkExpiredTransaction();
    }
}
