/*!	\file		MainWindow.xaml.cs
	\author		Haohan Liu, Dmytro Liaska
	\date		2019-04-08

  MainWindow class implementation
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Windows;

using GoFishLibrary;

namespace GoFishGUIClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public partial class MainWindow : Window, ICallback
    {
        private IShoe shoe = null;
        public int callbackID;

        // count of book
        private int countOfBook = 0;

        // list for cards on hand
        private List<Card> cardsOnHand;

        // current players
        Dictionary<int, string> playerList;

        // player name
        private string myName;

        // empty shoe
        private bool isShoeEmpty = false;
        // bool to determine if it is my turn
        private bool myTurn = false;
        // bool to determine if winner msg already displayed
        private bool winnerMsgDisplayed = false;

        public MainWindow(string name)
        {
            InitializeComponent();
            myName = name.ToCapital();

            try
            {
                DuplexChannelFactory<IShoe> channel = new DuplexChannelFactory<IShoe>(this, "ShoeEndpoint");
                shoe = channel.CreateChannel();

                // Subscribe to the callbacks
                callbackID = shoe.RegisterForCallbacks(myName);
                if (callbackID == -1)
                {
                    MessageBox.Show("Game has started");
                    this.Close();
                }
                else
                {
                    Title = "Go fish - " + myName;
                    txtMyName.Text = myName;

                    playerList = new Dictionary<int, string>();
                    cardsOnHand = new List<Card>();

                    // draw a set of cards
                    DrawCards();

                    // if the current player is the first player in game
                    // enable Ask For btn, game start from the current player
                    if (callbackID == 1)
                    {
                        gofishBtn.IsEnabled = false;
                        askforBtn.IsEnabled = true;
                    }
                    else
                    {
                        gofishBtn.IsEnabled = false;
                        askforBtn.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        // Helper methods
        // draw a set of cards for new user
        private void DrawCards()
        {
            for (int i = 0; i < 5; i++)
            {
                Card card = shoe.Draw();
                cardsOnHand.Add(card);
            }
        }

        // call shoe.BookCheck to see if cards on hand have four cards that have same rank
        // if so, remove them from cards on hand and update cards list
        private void BookCheck()
        {
            // Check if "I" have book on hand
            Card c_ = shoe.BookCheck(callbackID, cardsOnHand);

            if (c_ != null)
            {
                List<Card> updatedCardsOnHand = new List<Card>();

                foreach (var card_ in cardsOnHand)
                {
                    if (card_.Rank != c_.Rank)
                        updatedCardsOnHand.Add(card_);
                }

                cardsOnHand = updatedCardsOnHand;
                // Update My Cards list
                lbMyCards.Items.Clear();
                foreach (var card in cardsOnHand.OrderByDescending(c => c.Rank))
                {
                    lbMyCards.Items.Insert(0, card);
                }

                MessageBox.Show($"Book of {c_.Rank.ToString().ToCapital()}");
                // update the count of book
                countOfBook += 1;
                txtBookCount.Text = countOfBook.ToString();

                if (myTurn && cardsOnHand.Count == 0 && !isShoeEmpty)
                {
                    MessageBox.Show("Go Fish!");
                    gofishBtn.IsEnabled = true;
                    askforBtn.IsEnabled = false;
                }
            }
        }

        // Event Handlers
        // Go Fish btn clicked (shoe.Draw())
        private void drawBtn_Click(object sender, RoutedEventArgs e)
        {
            //myTurn = false;

            Card card = shoe.Draw(true);

            cardsOnHand.Add(card);
            lbMyCards.Items.Insert(0, card);

            // Check if "I" have book on hand
            BookCheck();
        }

        private void askforBtn_Click(object sender, RoutedEventArgs e)
        {
            if (lbPlayers.SelectedItem != null && lbMyCards.SelectedItem != null)
            {
                string playerName_ = lbPlayers.SelectedItem.ToString();
                Card selectedCard_ = (Card)lbMyCards.SelectedItem;

                // ask to see if the target player has the card
                List<Card> cardsReceived = shoe.AskFor(playerName_, selectedCard_, myName);

                if(cardsReceived != null && cardsReceived.Count > 0)
                {
                    foreach (var card in cardsReceived)
                    {
                        cardsOnHand.Add(card);
                        lbMyCards.Items.Insert(0, card);
                    }

                    // update count of Cards on hand
                    txtHandCount.Text = cardsOnHand.Count.ToString();

                    // Check if "I" have book on hand
                    BookCheck();
                }
                else
                {
                    if (!isShoeEmpty)
                    {
                        MessageBox.Show("Go Fish!");
                        gofishBtn.IsEnabled = true;
                        askforBtn.IsEnabled = false;
                    }
                    else
                    {
                        StandBy();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a player and a card to ask");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (shoe != null)
                    // Unsubscribe from the callbacks to prevent a runtime error in the service
                    shoe.UnregisterForCallbacks(callbackID);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                this.Close();
            }
        }

        // Implement ICallback contract
        private delegate void ClientUpdateDelegate(CallbackInfo info);

        public void UpdateGui(CallbackInfo info)
        {
            if (System.Threading.Thread.CurrentThread == this.Dispatcher.Thread)
            {
                // if there is a winner
                if (info.GameWinners != null || info.GameOver)
                {
                    if (!winnerMsgDisplayed)
                    {
                        //winnerMsgDisplayed = true;

                        if (info.GameWinners.Count == 1)
                        {
                            MessageBox.Show($"{info.GameWinners[0]} wins the game!");
                        }
                        else
                        {
                            string winnerNames = string.Empty;
                            foreach (var w in info.GameWinners)
                            {
                                winnerNames += $"{w}, ";
                            }

                            MessageBox.Show($"{winnerNames}win the game!");
                        }

                        shoe.ResetGame();

                        if (shoe != null)
                            // Unsubscribe from the callbacks to prevent a runtime error in the service
                            shoe.UnregisterForCallbacks(callbackID);

                        this.Close();
                    }

                    //StandBy();

                    //return;
                }
                else
                {
                    // Update Game Info
                    txtHandCount.Text = cardsOnHand.Count.ToString();
                    txtShoeCount.Text = info.NumCards.ToString();

                    // Update My Cards list
                    lbMyCards.Items.Clear();
                    foreach (var card in cardsOnHand.OrderByDescending(c => c.Rank))
                    {
                        lbMyCards.Items.Insert(0, card);
                    }

                    // Update Player List
                    //lbPlayers.ItemsSource = info.PlayerNames;
                    lbPlayers.Items.Clear();
                    foreach (var player in info.PlayerNames.OrderByDescending(p => p))
                    {
                        if (player.ToCapital() != myName.ToCapital())
                            lbPlayers.Items.Insert(0, player);
                    }

                    // Update Game Log
                    tbGameLog.Text = info.GameLogs;

                    // switch activate user
                    if (info.PlayerIdx == callbackID)
                    {
                        if (cardsOnHand.Count == 0 && 
                            gofishBtn.IsEnabled == false && 
                            askforBtn.IsEnabled == false)
                        {
                            MessageBox.Show("Now it is your turn, please go fish!");

                            gofishBtn.IsEnabled = true;
                            askforBtn.IsEnabled = false;
                        } 
                        else
                        {
                            //MessageBox.Show("Your turn!");
                            if (gofishBtn.IsEnabled == false && askforBtn.IsEnabled == false)
                            {
                                MessageBox.Show("Your turn!");
                            }

                            Play();
                        }

                        myTurn = true;
                    }
                    else
                    {
                        myTurn = false;
                        StandBy();
                    }

                    // shoe empty and game is on the last round
                    isShoeEmpty = info.EmptyShoe;

                    // if shoe empty && cardsOnHand is empty
                    if (isShoeEmpty && cardsOnHand.Count == 0 && !winnerMsgDisplayed)
                    {
                        StandBy();
                        // switch to the next player
                        shoe.NextPlayer();
                    }
                }
            }
            else
            {
                // Only the main (dispatcher) thread can change the GUI
                this.Dispatcher.BeginInvoke(new ClientUpdateDelegate(UpdateGui), info);
            }
        }

        public void RemoveMyCards(int count, Card card_)
        {
            lbMyCards.Items.Remove(card_);
        }

        // ask Target Player if the player has target card(s)
        public List<Card> AskTargetPlayer(Card targetCard)
        {
            List<Card> matchedCards = new List<Card>();
            List<Card> unmatchedCards = new List<Card>();

            // check if "I" have the requested card
            foreach (var card_ in cardsOnHand)
            {
                if (card_.Rank == targetCard.Rank)
                {
                    matchedCards.Add(card_);
                }
                else
                {
                    unmatchedCards.Add(card_);
                }
            }

            cardsOnHand = unmatchedCards;

            // some cards token by other players
            // a notice to the current player
            if (matchedCards.Count > 0)
            {
                MessageBox.Show($"You have lost {matchedCards.Count} {targetCard.Rank}(s)");
            }

            return matchedCards;
        }

        #region PLAYER_SWITCH

        private void Play()
        {
            gofishBtn.IsEnabled = false;
            askforBtn.IsEnabled = true;
        }

        private void StandBy()
        {
            gofishBtn.IsEnabled = false;
            askforBtn.IsEnabled = false;
        }

        #endregion
    }
}
