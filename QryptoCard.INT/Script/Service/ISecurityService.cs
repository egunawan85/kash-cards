using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ISecurityService" in both code and config file together.
    [ServiceContract]
    public interface ISecurityService
    {
        [OperationContract]
        bool validateUser(string email, string passw);
        [OperationContract]
        bool validateAPI(string api, string sec);
        [OperationContract]
        bool validateAdmin(string email, string passw);
        //[OperationContract]
        //bool validateAPI(string apikey, string seckey);
        [OperationContract]
        string base64Encode(string str);
        [OperationContract]
        string base64Decode(string str);
        [OperationContract]
        string encryptapp(string str);
        [OperationContract]
        string decryptapp(string str);
        [OperationContract]
        string encryptdb(string str);
        [OperationContract]
        string decryptdb(string str);
        [OperationContract]
        string dbtoapp(string str);
        [OperationContract]

        string apptodb(string str);
        [OperationContract]
        void signRSA();
        [OperationContract]
        void decryptRSA(string txt);



        [OperationContract]
        void getwb(string x);

        [OperationContract]
        void getcity();
    }
}
