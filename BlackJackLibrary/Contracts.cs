using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace BlackJackLibrary
{
    //ICallback interface: Defines the methods to update the clients
    //when updates are made
    [ServiceContract]
    public interface ICallback
    {
        [OperationContract(IsOneWay =true)]
        void UpdateClient(LibraryCallback info);
    }
    
    
    //IShoe Interface: Represents the service contract to be used by the Host
    [ServiceContract(CallbackContract = typeof(ICallback))]
    public interface IShoe
    {
        [OperationContract(IsOneWay = true)]
        void Shuffle();
        [OperationContract]
        Card Draw();
        uint NumDecks { [OperationContract] get; [OperationContract] set; }
        uint NumCards { [OperationContract] get; }
        [OperationContract]
        uint RegisterForCallbacks();
        [OperationContract]
        void UnregisterForCallbacks(uint clientId);
        [OperationContract(IsOneWay = true)]
        void UpdateLibraryWithClientInfo(uint clientId, uint clientPoints, bool stand);
        HashSet<Client> getClients();
        int NumClients();
    }
}
