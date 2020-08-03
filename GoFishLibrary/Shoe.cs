/*!	\file		Shoe.cs
	\author		Haohan Liu, Dmytro Liaska
	\date		2019-04-08

  Shoe class implementation
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace GoFishLibrary
{
    // Define a callback contract for the client to implement
    [ServiceContract]
    public interface ICallback
    {
        [OperationContract(IsOneWay = true)]
        void UpdateGui(CallbackInfo info);

        [OperationContract(IsOneWay = true)]
        void RemoveMyCards(int count, Card card_);

        [OperationContract]
        List<Card> AskTargetPlayer(Card tCard);
    }

    // Define a WCF service contract for the Shoe "service"
    [ServiceContract(CallbackContract = typeof(ICallback))]
    public interface IShoe
    {
        [OperationContract]
        List<Card> AskFor(string targetPlayerName, Card targetCard, string senderName);

        [OperationContract]
        Card Draw(bool fishing = false);

        [OperationContract]
        Card BookCheck(int callbackId_, List<Card> cards_);

        [OperationContract]
        void NextPlayer();

        [OperationContract]
        void ResetGame();

        List<string> PlayerNames { [OperationContract] get; }

        int NumCards { [OperationContract] get; }

        [OperationContract]
        int RegisterForCallbacks(string playerName);

        [OperationContract]
        void UnregisterForCallbacks(int callbackId_);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Shoe : IShoe
    {
        //************************************
        // member variables
        //************************************
        private bool hasGameStarted = false;
        private int callBackID = 1;
        // an index for determining players' turns
        // private int playerIdx = 1;

        // all cards in SHOE
        private List<Card> cards;
        private int cardIdx;

        // all players
        // Dictionary<callbackID, Dictionary<playerName, countOfBooks>>
        private Dictionary<int, Tuple<string, int>> players;

        // Member variables related to the callbacks
        private Dictionary<int, ICallback> clientCallBacks = null;

        // Return the number of available cards in the shoe (i.e. cards that haven't already been drawn)
        public int NumCards { get { return cards.Count - cardIdx; } }
        // Return Game Logs as a string
        public string GameLogs { get; private set; } = string.Empty;
        // The Callback ID/Idx for the current player
        public int PlayerIdx { get; private set; } = 1;
        // Return true if Shoe is empty
        public bool EmptyShoe { get { return cardIdx == cards.Count; } }
        // Winners <name>
        public List<string> GameWinners { get; private set; } = null;
        // bool GameOver
        public bool GameOver { get; private set; } = false;

        // Return the player list
        public List<string> PlayerNames
        {
            get
            {
                List<string> players_ = new List<string>();

                foreach (var p_ in players)
                {
                    players_.Add(p_.Value.Item1.ToCapital());
                }

                return players_;
            }
        }

        // ctor
        public Shoe()
        {
            // Initialize member variables
            cards = new List<Card>();
            players = new Dictionary<int, Tuple<string, int>>();
            clientCallBacks = new Dictionary<int, ICallback>();

            // populate cards to cards list
            PopulateCards();
        }

        public int RegisterForCallbacks(string playerName)
        {
            if (!hasGameStarted)
            {
                ICallback callback = OperationContext.Current.GetCallbackChannel<ICallback>();
                clientCallBacks.Add(callBackID, callback);

                // add new player information
                players.Add(callBackID, new Tuple<string, int>(playerName.ToLower(), 0));

                Console.WriteLine($"{playerName.ToCapital()} has joined the game.");
                GameLogs += $">{playerName.ToCapital()} has joined the game\n";

                return callBackID++;
            }
            return -1;
        }

        public void UnregisterForCallbacks(int callbackId_)
        {
            ICallback cb = OperationContext.Current.GetCallbackChannel<ICallback>();

            if (callbackId_ != -1)
            {
                GameOver = true;

                clientCallBacks.Remove(callbackId_);
                // remove player from player list
                Console.WriteLine($"{players[callbackId_].Item1.ToCapital()} has left the game.");
                GameLogs += $">{players[callbackId_].Item1.ToCapital()} has left the game\n";

                players.Remove(callbackId_);

                CheckWinner();

                //updateAllClients();
            }
        }

        public List<Card> AskFor(string targetPlayerName, Card targetCard, string senderName)
        {
            hasGameStarted = true;

            if (senderName.ToCapital() == players[1].Item1.ToCapital())
                GameLogs += $"---- {senderName.ToCapital()} is Playing ----\n";

            // find the CallBack info for target player from clientCallBacks
            ICallback targetCb = null;

            List<Card> matchedCards = null;

            foreach (var player_ in players)
            {
                if (player_.Value.Item1.ToLower() == targetPlayerName.ToLower())
                {
                    foreach (var cb_ in clientCallBacks)
                    {
                        if (cb_.Key == player_.Key)
                        {
                            targetCb = cb_.Value;
                        }
                    }
                }
            }

            if (targetCb != null)
            {
                // add message to game log
                GameLogs += $">{senderName.ToCapital()} asks {targetPlayerName.ToCapital()} for {targetCard.Rank.ToString().ToCapital()}s\n";

                matchedCards = targetCb.AskTargetPlayer(targetCard);

                // check if it is time to go fish
                if (matchedCards == null || matchedCards.Count == 0)
                    GameLogs += $">{targetPlayerName.ToCapital()} tells {senderName.ToCapital()} to go fish...\n";
                else
                    GameLogs += $">{targetPlayerName.ToCapital()} gives {senderName.ToCapital()} {matchedCards.Count} {targetCard.Rank.ToString().ToCapital()}(s)\n";
                
                // the last round
                // EmptyShoe is true && matchedCards is null
                if (EmptyShoe && (matchedCards == null || matchedCards.Count == 0))
                {
                    if (PlayerIdx == clientCallBacks.Count)
                    {
                        // check winner HERE !!!!!!!!!!!!!!!!!!!!
                        CheckWinner();

                        return matchedCards;
                    }
                    else
                        PlayerIdx += 1;
                }

                // update Guis
                updateAllClients();
            }
            else
            {
                Console.WriteLine($"Target player {targetPlayerName.ToCapital()} is not in game room.");
            }

            return matchedCards;
        }

        public Card Draw(bool fishing = false)
        {
            if (cardIdx >= cards.Count)
            {
                // No cards remaining to be drawn
                throw new System.IndexOutOfRangeException("The Shoe is empty.");
            }

            Card card = cards[cardIdx++];

            // No cards remaining to be drawn
            // set flag
            //if (cardIdx == cards.Count)
            //{

            //}

            // switch player
            if (fishing)
            {
                if (PlayerIdx == clientCallBacks.Count)
                    PlayerIdx = 1;
                else
                    PlayerIdx += 1;

                GameLogs += $"---- {players[PlayerIdx].Item1.ToCapital()} is Playing ----\n";
            }

            updateAllClients();

            return card;
        }

        public void Shuffle()
        {
            // Console.WriteLine("[Player #" + playerNum + "] Shuffling");

            // Randomize the cards in the collection using the List object's 
            // OrderBy() method and a simple Lambda expression
            // The technique uses a sort algorithm which, instead of commparing
            // a common key field for two objects, compares random integers 
            Random rng = new Random();
            cards = cards.OrderBy(number => rng.Next()).ToList();

            // Reset the cards index
            cardIdx = 0;
        }

        // check if there is book
        public Card BookCheck(int callbackId_, List<Card> cards_)
        {
            foreach (var c in cards_)
            {
                int rankCount = 1;

                foreach (var c_ in cards_)
                {
                    if (c.Suit != c_.Suit && c.Rank == c_.Rank)
                    {
                        rankCount += 1;
                    }
                }

                if (rankCount == 4)
                {
                    int originalBookCount = players[callbackId_].Item2 + 1;
                    Tuple<string, int> tempTuple = new Tuple<string, int>(players[callbackId_].Item1, originalBookCount);
                    players[callbackId_] = tempTuple;

                    updateAllClients();

                    return c;
                }
            }

            return null;
        }

        // switch to the next player
        public void NextPlayer()
        {
            GameLogs += $"---- {players[PlayerIdx].Item1.ToCapital()} is Playing ----\n";
            GameLogs += $">{players[PlayerIdx].Item1.ToCapital()} doesn't have any card left\n";

            if (PlayerIdx != clientCallBacks.Count)
            {
                PlayerIdx += 1;
                GameLogs += $"---- {players[PlayerIdx].Item1.ToCapital()} is Playing ----\n";

                updateAllClients();
            }
            else
            {
                CheckWinner();
            }

            //updateAllClients();
        }

        public void ResetGame()
        {
            // reset program
            cards = new List<Card>();
            PopulateCards();

            cardIdx = 0;
            callBackID = 1;
            PlayerIdx = 1;
            GameLogs = string.Empty;
            GameWinners = null;
            GameOver = false;
            hasGameStarted = false;
        }

        // Helper methods
        // populate cards to shoe
        private void PopulateCards()
        {
            // Remove "old" cards
            cards.Clear();

            // For each suit
            foreach (Card.SuitID s in Enum.GetValues(typeof(Card.SuitID)))
            {
                // For each rank
                foreach (Card.RankID r in Enum.GetValues(typeof(Card.RankID)))
                {
                    cards.Add(new Card(s, r));
                }
            }

            // Randomize the collection
            Shuffle();
        }

        // check if we got a winner
        private void CheckWinner()
        {
            List<int> countOfBooks = new List<int>();
            foreach (var p_ in players)
            {
                countOfBooks.Add(p_.Value.Item2);
            }
            int highestCountOfBooks = countOfBooks.Max();

            // dict<callbackID, name> winners
            List<string> winners = new List<string>();
            foreach (var p_ in players)
            {
                if (p_.Value.Item2 == highestCountOfBooks)
                {
                    winners.Add(p_.Value.Item1);
                }
            }

            GameWinners = winners;

            updateAllClients();

            //if (GameWinners != null || GameOver)
            //{
            //    // reset program
            //    //cards = new List<Card>();
            //    //PopulateCards();

            //    //cardIdx = 0;
            //    //callBackID = 1;
            //    //PlayerIdx = 1;
            //    //GameLogs = string.Empty;
            //    //GameWinners = null;
            //    //GameOver = false;
            //    //hasGameStarted = false;
            //}

            // reset winner
            //GameWinners = null;
        }

        // update all registered clients' gui
        private void updateAllClients()
        {
            CallbackInfo info = new CallbackInfo(NumCards, PlayerNames, GameLogs, PlayerIdx, EmptyShoe, GameWinners, GameOver);

            foreach (ICallback cb in clientCallBacks.Values)
                cb.UpdateGui(info);
        }
    }

    public static class MyExtensions
    {
        public static string ToCapital(this string word)
        {
            return word.First().ToString().ToUpper() + word.Substring(1).ToLower();
        }
    }
}
