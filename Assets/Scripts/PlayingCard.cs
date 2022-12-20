using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
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
    public struct PlayingCardInfo : INetworkSerializable, IEquatable<PlayingCardInfo>
    {
        public Suit Suit => _suit;
        public Rank Rank => _rank;

        private Rank _rank;
        private Suit _suit;

        public PlayingCardInfo(Suit suit, Rank rank)
        {
            _suit = suit;
            _rank = rank;
        }

        public PlayingCardInfo(IReadOnlyList<char> suitRank)
        {
            if (suitRank.Count != 2) throw new ArgumentException("String param has to consist of 2 characters");

            _suit = SuitFromChar(suitRank[0]);
            _rank = RankFromChar(suitRank[1]);
        }

        public override string ToString()
        {
            return $"{Suit} {Rank}";
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _suit);
            serializer.SerializeValue(ref _rank);
        }

        public bool Equals(PlayingCardInfo other)
        {
            return Suit == other.Suit && Rank == other.Rank;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayingCardInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Suit * 397) ^ (int)Rank;
            }
        }

        public int Worth => Rank switch
        {
            Rank.Sieben => 0,
            Rank.Acht => 0,
            Rank.Neun => 0,
            Rank.Unter => 2,
            Rank.Ober => 3,
            Rank.Koenig => 4,
            Rank.Zehn => 10,
            Rank.Ass => 11,
            _ => throw new ArgumentOutOfRangeException()
        };
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

    public static readonly Dictionary<PlayingCardInfo, Sprite> SpriteDict = new();
    public static readonly Sprite DefaultCardSprite = Resources.Load<Sprite>("Spielkarten/rueckseite");

    private static Sprite LoadSprite(PlayingCardInfo cardInfo)
    {
        var pathSuffix = "";
        pathSuffix += cardInfo.Suit.ToString().ToLower();
        pathSuffix += GetRankFileSuffix(cardInfo.Rank);
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