using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace BlackJackLibrary
{
    // Shoe Class: Implements the WCF service contract
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Shoe : IShoe
    {
        // Private member variables
        //Dictionary of cards: Key - name, value - integer value for a card
        private static readonly Dictionary<string, uint> ranks = new Dictionary<string, uint>()
        {
            { "Ace", 1 },
            { "Two", 2 },
            { "Three", 3 },
            { "Four", 4 },
            { "Five", 5 },
            { "Six", 6 },
            { "Seven", 7 },
            { "Eight", 8 },
            { "Nine", 9 },
            { "Ten", 10 },
            { "Jack", 11 },
            { "Queen", 12 },
            { "King", 13 },
        };

        //Dictionary of Suits: Key - name, value - integer value to represent a card in Hexadecimal
        //The hexadecimal value when converted to ascii and concatatenated with the rank
        //gererates a visual image for the card (see method To String in the Card.cs)
        private static readonly Dictionary<string, uint> suits = new Dictionary<string, uint>()
        {
            { "Spades", 10 },
            { "Hearts", 11 },
            { "Diamonds", 12 },
            { "Clubs", 13 },
        };

        // List of Card objects
        private List<Card> cards = new List<Card>();
        // List of the Dealers cards
        private List<Card> dealerCards = new List<Card>();
        // Index of card to track cards in the list
        private int cardsIndex = 0;
        // Default number of decks = 6
        private uint numDecks = 6;  
        //List of callbacks (1 per client)
        private HashSet<ICallback> callbacks= new HashSet<ICallback>();
        //List of clients
        private HashSet<Client> clients = new HashSet<Client>();
        // Number of players
        private static uint numPlayers = 0;
        //Next client to play
        private uint nextClientId = 1;

        // Default constructor: populates the cards collection
        public Shoe()
        {
            Repopulate();
        }

        // Public Properties
        // A property to allow any client to read/initialize the number of decks
        public uint NumDecks
        {            
            get { return numDecks; }
            set
            {
                if (value != numDecks)
                {
                    numDecks = value;
                    Repopulate();
                }
            }
        }

        // A property to allow any client to know how many Card objects are available to be drawn
        public uint NumCards
        {
            get { return (uint)(cards.Count - cardsIndex); }
        }

        //Public Methods   

        //Draw method: Returns a Card; the next available Card in the cards collection
        public Card Draw()
        {
            if (cardsIndex == cards.Count)
            {
                throw new InvalidOperationException("No card available in the shoe.");
            }
            Card card = cards[cardsIndex++];
            //UpdateAllClients(false);
            Console.WriteLine($"Shoe dealing {cards[cardsIndex]}");
            return card;
        }

        public Card GetCardback() => new Card(10, 0);

        //Shuffle Method: Shuffle the sequence of cards in the cards collection randomly
        public void Shuffle()
        {
            Console.WriteLine($"Shoe shuffling");

            Random rng = new Random();
            cards = cards.OrderBy(card => rng.Next()).ToList();

            cardsIndex = 0;
            //UpdateAllClients(true);
        }

        //Register the client for callback and returns an int representing the client's id.
        public uint RegisterForCallbacks()
        {
            ICallback callback = OperationContext.Current.GetCallbackChannel<ICallback>();
            //The Add method in the HashSet only adds a element if it is a new element
            if (callbacks.Add(callback))
            {
                ++numPlayers;
                Client client = new Client(numPlayers, 0, $"Player {numPlayers}", 0, false);
                clients.Add(client);
                return client.ClientID;
            }
            else
            {
                return 0;
            }
                
        }

        public void UnregisterForCallbacks(uint clientId)
        {
            ICallback callback = OperationContext.Current.GetCallbackChannel<ICallback>();
            //The Remove method in the HashSet only adds a element if it is a new element
            Client client = clients.First(e => e.ClientID == clientId);
            if(nextClientId == clientId)
            {
                nextClientId = FindNextClientID(clients, client);
            }
                
            if (client != null)
            {
                clients.Remove(client);
                callbacks.Remove(callback);
                LibraryCallback info = new LibraryCallback(clients, false, new List<Card>(), new List<Card>(), nextClientId);
                foreach (ICallback calback in callbacks)
                {
                    calback.UpdateClient(info);
                }
            }
        }

        //private void UpdateAllClients(bool emptyHand)
        //{
        //    LibraryCallback info = new LibraryCallback(NumCards, numDecks, emptyHand);

        //    foreach (ICallback calback in callbacks)
        //    {
        //        calback.UpdateClient(info);
        //    }
        //}

        public void UpdateLibraryWithClientInfo(uint clientId, uint clientPoints, bool stand)
        {
            //Finds the client in the list to be updated by its ID
            Client client = clients.First(e => e.ClientID == clientId);

            //Check if the client stands and update the next client ID
            if(stand)
                nextClientId = FindNextClientID(clients, client);


            if (client != null)
            {
                clients.Remove(client);
                client.TotalPoints = clientPoints;
                client.Stand = stand;
                clients.Add(client);
                bool isRoundFinished = ComputeRoundResults(ref clients);
                LibraryCallback info = new LibraryCallback(clients, isRoundFinished, new List<Card>(), new List<Card>(), nextClientId);
                if (isRoundFinished)
                {



                    isRoundFinished = false;
                    Shuffle();
                    // deal 2 new cards for dealer
                    dealerCards.Clear();
                    dealerCards.AddRange(new List<Card>() { Draw(), Draw() });
                    info.DealerCards = dealerCards;

                    while (info.DealerCards.Sum(card => card.Rank) <= 16)
                    {
                        info.DealerCards.Add(Draw());
                    }

                    //deal 2 new cards for each player                    
                    foreach (ICallback calback in callbacks)
                    {
                        List<Card> cards = new List<Card>() { Draw(), Draw(), };
                        info.ClientCards = cards;
                        calback.UpdateClient(info);      
                    }
                }
                else
                {
                    foreach (ICallback calback in callbacks)
                    {
                        calback.UpdateClient(info);
                    }
                }
                
                
                
            } 
        }


        // Helper methods

        
        private uint FindNextClientID(HashSet<Client> clients, Client currentClient)
        {
            int nextClientIndex = Array.IndexOf(clients.ToArray(), currentClient) + 1;
            uint nextId;
            if (nextClientIndex >= clients.Count)
                nextId = clients.ElementAt(0).ClientID;
            else
                nextId = clients.ElementAt(nextClientIndex).ClientID;
            return nextId;
        }
        
        //Repopulate Method: Clears the cards collection, populates it with new cards
        // and then randomizes the cards collection
        private void Repopulate()
        {
            Console.WriteLine($"Shoe repopulating with {numDecks} decks");

            // clears the current list
            cards.Clear();

            // Re-populate the list with new cards
            for (int d = 0; d < numDecks; d++)
            {
                // For each suit in SuitID...
                foreach (var s in suits)
                {
                    // For each rank in RankdID...
                    foreach (var r in ranks)
                    {
                        // Create and add a new Card 
                        cards.Add(new Card(s.Value, r.Value));
                    }
                }
            }

            // Shuffles the cards in the list
            Shuffle();
        }


        //
        private bool ComputeRoundResults(ref HashSet<Client> clients)
        {
            //check if any client got a blackjack
            var blackjackClients = clients.Where(element => element.TotalPoints == 21);

            uint dealersTotalPoints = 0;
            foreach (Card card in dealerCards)
            {
                //If the card is an ace and the sum of points are 
                //less or equal to 10, the ace counts as 11 points
                if (card.Rank == 1 && dealersTotalPoints <= 10)
                {
                    dealersTotalPoints += 11;
                }
                else
                {
                    dealersTotalPoints += (uint)card.Rank;
                }
            }
            bool dealerHasBlackjack = dealersTotalPoints == 21 ? true : false;
            if (blackjackClients.Count() > 1 || dealerHasBlackjack || (blackjackClients.Count() >= 1 && dealerHasBlackjack))
            {
                return true;
            }
            else if (blackjackClients.Count() == 1)
            {
                blackjackClients.First().Score++;
                return true;
            }
            //check if all clients have stand
            Client client = clients.FirstOrDefault(element => element.Stand == false);
            if (client == null)
            {
                //find client with highest points
                var orderedClients = clients.Where(element => element.TotalPoints < 21).OrderBy(element => element.TotalPoints);
                if (orderedClients.Count() != 0)
                {
                    uint highestPoints = orderedClients.Last().TotalPoints;
                    //check if there are other clients with the same total points
                    var tiedClients = orderedClients.Where(element => element.TotalPoints == highestPoints).ToList();

                    if (tiedClients.Count() == 1 && highestPoints > dealersTotalPoints)
                    {
                        tiedClients[0].Score++;
                    }
                }
                return true;
            }

            return false;
        }
        public int NumClients()
        {
            return clients.Count;
        }
        public HashSet<Client> getClients()
        {
            return clients;
        }
    } // end class
}
