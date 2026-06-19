using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace QryptoCard.Scrapper.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IAutomationService" in both code and config file together.
    [ServiceContract]
    public interface IAutomationService
    {
        [OperationContract]
        void USAddressGenerator();
        [OperationContract]
        Task<int> AddressGenerator();
    }
}
