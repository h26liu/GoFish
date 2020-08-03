/*!	\file		Card.cs
	\author		Haohan Liu, Dmytro Liaska
	\date		2019-04-08

  Card class implementation
*/

using System.Runtime.Serialization;

namespace GoFishLibrary
{
    [DataContract]
    public class Card
    {
        // Constructor
        public Card(SuitID s, RankID r)
        {
            Suit = s;
            Rank = r;
        }

        // Enumerations
        public enum SuitID { Clubs, Diamonds, Hearts, Spades };
        public enum RankID { Ace, King, Queen, Jack, Ten, Nine, Eight, Seven, Six, Five, Four, Three, Two };

        [DataMember] public SuitID Suit { get; private set; }
        [DataMember] public RankID Rank { get; private set; }

        public override string ToString()
        {
            return Rank.ToString() + " of " + Suit.ToString();
        }
    }
}
