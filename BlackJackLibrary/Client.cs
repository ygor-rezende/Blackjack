using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BlackJackLibrary
{
    [DataContract]
    public  class Client
    {
        [DataMember]
        public uint ClientID { get; set; }
        [DataMember]
        public uint TotalPoints { get; set; }
        [DataMember]
        public string ClientName { get; set; }
        [DataMember]
        public uint Score { get; set; }
        [DataMember]
        public bool Stand { get; set; }

        public Client(uint clientID, uint totalPoints, string clientName, uint score, bool stand)
        {
            ClientID = clientID;
            TotalPoints = totalPoints;
            ClientName = clientName;
            Score = score;
            Stand = stand;
        }
    }
}
