using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IUserV1Service" in both code and config file together.
    [ServiceContract]
    public interface IUserV1Service
    {
        [OperationContract]
        OutputModel getUser(string em, tblM_User x);
        [OperationContract]
        OutputModel getUserCommissionList(string em, vw_User_Commission x);
        [OperationContract]
        OutputModel updateUserCommission(string em, tblM_User_Commission x);
        [OperationContract]
        OutputModel getUserFeeList(string em, vw_User_Fee x);
        [OperationContract]
        OutputModel updateUserFee(string em, tblM_User_Fee x);
    }
}
