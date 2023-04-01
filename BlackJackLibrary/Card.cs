using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BlackJackLibrary
{
    // Card Class
    [DataContract]
    public class Card
    {
        // Public properties: read-only for clients
        [DataMember]
        public uint Suit { get; private set; }
        [DataMember]
        public uint Rank { get; private set; }

        // Constructor: hidden from clients
        internal Card(uint s, uint r)
        {
            Suit = s;
            Rank = r;
        }

        // To string Method: Retuns an ascii code string representing a card
        // When printing a card its image is displayed 
        public override string ToString()
        {
            uint asciiCodeRank = Rank >= 12 ? Rank + 1 : Rank;
            string card = char.ConvertFromUtf32(Int32.Parse(Suit.ToString("X") + asciiCodeRank.ToString("X"), System.Globalization.NumberStyles.HexNumber) + 0x1F000);
            Console.WriteLine(card);
            return card;
        }

    } // end class
}
