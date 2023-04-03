/*
 * Program:         BlackjackClient.exe (.NET Framework version)
 * Module:          MainWindow.xaml.cs
 * Author:          T. Haworth
 * Date:            March 9, 2023
 * Description:     An Windows WPF client to use and demonstrate the features/services 
 *                  of the CardsLibrary.dll assembly. This version has been modified to 
 *                  use the Shoe class as a WCF service. It also uses an endpoint
 *                  configuration that is declared "administratively" in the client's
 *                  App.config file.
 *                  
 *                  Note that we had to add a reference to the .NET Framework 
 *                  assembly System.ServiceModel.dll.
 */

using System;
using System.Windows;
using System.ServiceModel;  // WCF types
using BlackJackLibrary;
using System.Threading;
using System.Linq;

namespace CardsGUIClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ICallback
    {
        // --------------------- Member variables ---------------------
        private readonly IShoe shoe; // Note: Type IShoe instead of Shoe
        private bool isClientTurn = false;
        private uint clientId, activeClientId;
        private bool stand;
        private uint cardsOnHandCount = 0;
        // ------------------------ Constructor -----------------------
        public MainWindow()
        {
            InitializeComponent();

            try
            {

                DuplexChannelFactory<IShoe> channel = new DuplexChannelFactory<IShoe>(this,"ShoeEndPoint");
                shoe = channel.CreateChannel();
                clientId = shoe.RegisterForCallbacks();
                //If client id == 1 this is the first player so far
                //Release this client
                if (clientId == 1)
                    isClientTurn= true;

                // Add 2 Cardbacks to Dealers Hand.
                ListDealerCards.Items.Add(shoe.GetCardback());
                ListDealerCards.Items.Add(shoe.GetCardback());

                Card card1 = shoe.Draw();
                Card card2 = shoe.Draw();
                ListCards.Items.Add(card2);
                ListCards.Items.Add(card1);

                UpdateCardCounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        // ---------------------- Event handlers ----------------------

        // Runs when the user clicks the Hit button
        private void ButtonHit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isClientTurn) // Check if it is the client's turn
                {
                    // Modified to receive a string instead of a Card object from Draw()
                    Card card = shoe.Draw();
                    ListCards.Items.Add(card);

                    UpdateCardCounts();
                }
                else
                {
                    MessageBox.Show("Another player is playing.", "Wait for your turn");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Runs when the user clicks the Stand button
        private void ButtonStand_Click(object sender, RoutedEventArgs e)
        {


            try
            {
                if (isClientTurn)
                {
                    isClientTurn = false;
                    stand = true;
                    shoe.UpdateLibraryWithClientInfo(clientId, cardsOnHandCount, stand);
                    MessageBox.Show("You chose to stand. Wait for the round's results.");
                }
                else
                {
                    MessageBox.Show($"Another player is playing.", $"Wait for your turn"); //[{isSomeonesTurn}]
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Runs when the user clicks the Close button
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Runs when the user slides the slider control to change the number of decks

        // ---------------------- Helper methods ----------------------

        // Reinitializes the Shoe and Hand card counts in the GUI
        private void UpdateCardCounts()
        {
            
            currentPoints.Content = $"You have a total of: ";
            CardOnHandLabel.Content = $"Player {clientId} cards on hand";
            CurrentPlayer.Content = $"Current player playing: ";
            cardsOnHandCount = 0;
            foreach (Card card in ListCards.Items)
            {
                //If the card is an ace and the sum of points are 
                //less or equal to 10, the ace counts as 11 points
                if(card.Rank == 1 && cardsOnHandCount <= 10)
                {
                    cardsOnHandCount += 11;
                }
                else
                {
                    cardsOnHandCount += (uint)card.Rank;
                }                
            }
            currentPoints.Content += cardsOnHandCount.ToString() + " points. ";

            if(cardsOnHandCount == 21)
            {
                currentPoints.Content += "Congratulations! You got a Blackjack!";
                stand = true;
                isClientTurn = false;
            }

            if (cardsOnHandCount > 21)
            {
                isClientTurn = false;
                currentPoints.Content += "You are busted!";
                stand = true;
                //shoe.Shuffle();
                //currentPoints.Content = "You have a total of: ";
            }

            //Inform other clients the number of points
            shoe.UpdateLibraryWithClientInfo(clientId, cardsOnHandCount, stand);
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            shoe.UnregisterForCallbacks(clientId);
            (shoe as IClientChannel)?.Close();
        }

        private void ListCards_Copy_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void ListCards_Copy1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void ListCards_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }


        //ICallback interface method implementation
        //Receives an LibraryCallback object with the information from the library
        //Updates the client object with the information received
        public void UpdateClient(LibraryCallback info)
        {
            if (System.Threading.Thread.CurrentThread == this.Dispatcher.Thread)
            {
                ListPlayers.Items.Clear();
                foreach (var client in info.Clients)
                {
                    ListPlayers.Items.Add($"Player {client.ClientID}: {client.Score}");
                }

                if(info.IsRoundDone)
                {
                    //reset player information
                    ListDealerCards.Items.Clear();
                    foreach (var card in info.DealerCards)
                        ListDealerCards.Items.Add(card);

                    MessageBox.Show("This round is over. Starting a new round.");

                    // Add 2 Cardbacks to Dealers Hand.
                    ListDealerCards.Items.Clear();
                    ListDealerCards.Items.Add(info.Cardback);
                    ListDealerCards.Items.Add(info.Cardback);

                    ListCards.Items.Clear();
                    ListCards.Items.Add(info.ClientCards[0]);
                    ListCards.Items.Add(info.ClientCards[1]);
                    cardsOnHandCount = 0;
                    stand = false;

                    UpdateCardCounts();

                    activeClientId = info.NextClientID;
                    CurrentPlayer.Content = $"Current player playing: Player {activeClientId}";
                    if (activeClientId == clientId && cardsOnHandCount < 21)
                    {
                        isClientTurn = true;
                    }
                }
                else
                {
                    activeClientId = info.NextClientID;
                    CurrentPlayer.Content = $"Current player playing: Player {activeClientId}";
                    if (activeClientId == clientId)
                    {
                        isClientTurn = true;
                    }
                }
            }
            else
            {
                Action<LibraryCallback> updateDelegate = UpdateClient;
                this.Dispatcher.BeginInvoke(updateDelegate, info);
            }
        }

       
    } // end partial class
}
