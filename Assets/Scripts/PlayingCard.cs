using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayingCard
{
    #region Playing Card Definition
    // use struct for easy serialization in SyncList
    public readonly struct PlayingCardInfo
    {
        public readonly Suit suit;
        public readonly Rank rank;

        public PlayingCardInfo(Suit suit, Rank rank)
        {
            this.suit = suit;
            this.rank = rank;
        }

        public override string ToString()
        {
            return $"{suit} {rank}";
        }

        public override bool Equals(object otherObj)
        {
            if (otherObj is PlayingCardInfo other)
            {
                return suit == other.suit && rank == other.rank;
            }

            return base.Equals(otherObj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) suit * 397) ^ (int) rank;
            }
        }
    }

    public enum Suit
    {
        Blatt,
        Eichel,
        Schelln,
        Herz
    }

    /**
     * We order the ranks by their default precedence
     * (i.e. when no trumps are involved), so we can use
     * the enum order directly when comparing cards.
     *
     * This ordering will also be helpful if we decide to use
     * our saulib library for determining the stich winner later.
     * TODO test if this enum ordering produces problems
     */
    public enum Rank
    {
        Sieben = 7,
        Acht,
        Neun,
        Koenig,
        Zehn,
        Ass,
        Unter,
        Ober
    }
    #endregion

    #region Stich Definition

    public struct Stich
    {
        private PlayingCardInfo[] _cards;
        public int CardCount { get; private set; }

        public bool IsComplete => CardCount == 4;

        public void AddCard(PlayingCardInfo card)
        {
            if (IsComplete)
            {
                throw new InvalidOperationException("Stich already complete, cannot add card");
            }
            
            if (_cards == null)
            {
                _cards = new PlayingCardInfo[4];
            }
            
            _cards[CardCount] = card;
            CardCount++;
        }

        public void AddAll(PlayingCardInfo[] cards)
        {
            foreach (PlayingCardInfo card in cards)
            {
                AddCard(card);
            }
        }

        public PlayingCardInfo GetCard(int index)
        {
            return _cards[index];
        }
    }

    #endregion
    
    #region Static Variables and Helpers
    
    
    public static readonly Dictionary<PlayingCardInfo, Sprite> SpriteDict = new Dictionary<PlayingCardInfo, Sprite>();
    public static readonly Sprite DefaultCardSprite = Resources.Load<Sprite>("Spielkarten/rueckseite");
    
    private static Sprite LoadSprite(PlayingCardInfo cardInfo)
    {
        string pathSuffix = "";
        pathSuffix += cardInfo.suit.ToString().ToLower();
        pathSuffix += GetRankFileSuffix(cardInfo.rank);
        return Resources.Load<Sprite>($"Spielkarten/{pathSuffix}");
    }

    private static string GetRankFileSuffix(Rank rank)
    {
        switch (rank)
        {
            case Rank.Sieben:
            {
                return "07";
            }
            case Rank.Acht:
            {
                return "08";
            }
            case Rank.Neun:
            {
                return "09";
            }
            case Rank.Koenig:
            {
                return "ko";
            }
            case Rank.Zehn:
            {
                return "10";
            }
            case Rank.Unter:
            {
                return "un";
            }
            case Rank.Ober:
            {
                return "ob";
            }
            case Rank.Ass:
            {
                return "as";
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }
        }
    }

    /// <summary>
    /// Make sure to only call this method ONCE, because it loads all the sprites
    /// </summary>
    /// <returns></returns>
    public static List<PlayingCardInfo> InitializeCardDeck()
    {
        var deck = new List<PlayingCardInfo>();
        
        var ranks = Enum.GetValues(typeof(Rank));
        var suits = Enum.GetValues(typeof(Suit));
        foreach (Suit suit in suits)
        {
            foreach (Rank rank in ranks)
            {
                PlayingCardInfo cardInfo = new PlayingCardInfo(suit, rank);
                deck.Add(cardInfo);
                
                // also store the sprite in the dictionary for later access
                SpriteDict[cardInfo] = LoadSprite(cardInfo);
            }
        }

        // TODO disabled for debugging purposes
        // deck.Shuffle();

        return deck;
    }

    #endregion

}
