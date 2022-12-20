using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public static class PlayingCard
{
    #region Stich Definition

    public struct Stich
    {
        private Stack<PlayingCardInfo> _cards;
        public int CardCount => _cards.Count;

        public bool IsComplete => CardCount == 4;

        public void AddCard(PlayingCardInfo card)
        {
            if (_cards == null) _cards = new Stack<PlayingCardInfo>(4);

            if (IsComplete) throw new InvalidOperationException("Stich already complete, cannot add card");

            _cards.Push(card);
        }

        public void AddAll(IEnumerable<PlayingCardInfo> cards)
        {
            foreach (var card in cards) AddCard(card);
        }

        // hopefully we never need this
        public PlayingCardInfo[] Cards => _cards.ToArray();

        public PlayingCardInfo RemoveTop()
        {
            return _cards.Pop();
        }

        public PlayingCardInfo Top => _cards.Peek();

        public int Worth
        {
            get
            {
                // Currently, there shouldn't be any reason to query the worth of an an incomplete stich.
                Assert.IsTrue(IsComplete);
                return _cards.Sum(card => card.Worth);
            }
        }
    }

    #endregion

    #region Playing Card Definition

    // use struct for easy serialization in SyncList
    public readonly struct PlayingCardInfo : IEquatable<PlayingCardInfo>
    {
        public readonly Suit suit;
        public readonly Rank rank;

        public PlayingCardInfo(Suit suit, Rank rank)
        {
            this.suit = suit;
            this.rank = rank;
        }

        public PlayingCardInfo(IReadOnlyList<char> suitRank)
        {
            if (suitRank.Count != 2) throw new ArgumentException("String param has to consist of 2 characters");

            suit = SuitFromChar(suitRank[0]);
            rank = RankFromChar(suitRank[1]);
        }

        public override string ToString()
        {
            return $"{suit} {rank}";
        }

        public bool Equals(PlayingCardInfo other)
        {
            return suit == other.suit && rank == other.rank;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayingCardInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)suit * 397) ^ (int)rank;
            }
        }

        public int Worth
        {
            get
            {
                switch (rank)
                {
                    case Rank.Sieben:
                    case Rank.Acht:
                    case Rank.Neun: return 0;

                    case Rank.Unter: return 2;
                    case Rank.Ober: return 3;
                    case Rank.Koenig: return 4;
                    case Rank.Zehn: return 10;
                    case Rank.Ass: return 11;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    /**
     * The Suit Enum is ordered by increasing trump-precedence so we can use it to compare two trump cards
     * see also GameManager.GetTrumpList
     */
    public enum Suit
    {
        Schelln,
        Herz,
        Blatt,
        Eichel
    }

    /**
     * We order the ranks by their default precedence
     * (i.e. when no trumps are involved), so we can use
     * the enum order directly when comparing cards.
     * (The trumps are evaluated separately, so we can "mix them in" here)
     */
    public enum Rank
    {
        Sieben = 7,
        Acht,
        Neun,
        Unter,
        Ober,
        Koenig,
        Zehn,
        Ass
    }


    private static Suit SuitFromChar(char charSuit)
    {
        switch (charSuit)
        {
            case 'S': return Suit.Schelln;
            case 'H': return Suit.Herz;
            case 'B': return Suit.Blatt;
            case 'E': return Suit.Eichel;
            default: throw new ArgumentException("Suit has to be one of {S, H, B, E}");
        }
    }

    private static Rank RankFromChar(char charRank)
    {
        switch (charRank)
        {
            case '7':
                return Rank.Sieben;
            case '8':
                return Rank.Acht;
            case '9':
                return Rank.Neun;
            case '1':
                return Rank.Zehn;
            case 'U':
                return Rank.Unter;
            case 'O':
                return Rank.Ober;
            case 'K':
                return Rank.Koenig;
            case 'A':
                return Rank.Ass;
            default:
                throw new ArgumentException("Rank has to be one of {7, 8, 9, 1, U, O, K, A}, " +
                                            $"but was {charRank}");
        }
    }

    #endregion

    #region Static Variables and Helpers

    public static readonly Dictionary<PlayingCardInfo, Sprite> SpriteDict = new Dictionary<PlayingCardInfo, Sprite>();
    public static readonly Sprite DefaultCardSprite = Resources.Load<Sprite>("Spielkarten/rueckseite");

    private static Sprite LoadSprite(PlayingCardInfo cardInfo)
    {
        var pathSuffix = "";
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
    ///     Make sure to only call this method ONCE, because it loads all the sprites
    /// </summary>
    /// <returns></returns>
    public static List<PlayingCardInfo> InitializeCardDeck()
    {
        var deck = new List<PlayingCardInfo>();

        var ranks = Enum.GetValues(typeof(Rank));
        var suits = Enum.GetValues(typeof(Suit));
        foreach (Suit suit in suits)
        foreach (Rank rank in ranks)
        {
            var cardInfo = new PlayingCardInfo(suit, rank);
            deck.Add(cardInfo);

            // also store the sprite in the dictionary for later access
            SpriteDict[cardInfo] = LoadSprite(cardInfo);
        }

        return deck;
    }

    #endregion
}