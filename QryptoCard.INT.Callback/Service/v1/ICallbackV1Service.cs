using QryptoCard.INT.Callback.Model.PGCrypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Callback.Service.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICallbackV1Service" in both code and config file together.
    [ServiceContract]
    public interface ICallbackV1Service
    {
        [OperationContract]
        void Wasabi(string cat, string sign, string req, string a);
        [OperationContract]
        void PGCrypto(PGCryptoModel x);


        [OperationContract]
        void recreateCallback();

        [OperationContract]
        void reTopup(string tid);

        // Reconciliation sweep entry point (invoked by the scheduled trigger over the loopback
        // endpoint). Returns the number of stranded orders handled this pass.
        [OperationContract]
        int ReconcilePendingProvider();

        // WasabiCard balance monitor + auto-fund tick (invoked by the scheduled trigger over the
        // loopback endpoint). Reads the float, performs a floor refill if enabled+needed, evaluates
        // the low-balance/coverage alert. Returns a compact JSON summary.
        [OperationContract]
        string RunWasabiCardMonitor();

        // Deposit-into-card streaming pump tick (forward + confirm). On the contract so it can be wired
        // to the scheduled trigger exactly like RunWasabiCardMonitor. NOTE: the loopback controller
        // route + generated client (Reference.cs) + scheduler-trigger.ps1 entry still need to be added
        // to actually drive it (the reachability increment). No-op while streaming is OFF.
        [OperationContract]
        string RunCardFundingPump();
    }
}
