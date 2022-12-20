using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public static class PlayingCard
{
    #region Playing Card Definition
    // use struct for easy serialization in SyncList
    public struct PlayingCardInfo
    {
        public Rank Rank;
        public Suit Suit;
        public override string ToString()
        {
            return $"{Suit} {Rank}";
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum Suit
    {
        Blatt,
        Eichel,
        Schelln,
        Herz
    }

    public enum Rank
    {
        Sieben,
        Acht,
        Neun,
        Zehn,
        Koenig,
        Unter,
        Ober,
        Ass
    }
    #endregion
    
    #region Static Variables and Helpers
    
    
    public static readonly Dictionary<PlayingCardInfo, Sprite> SpriteDict = new Dictionary<PlayingCardInfo, Sprite>();
    public static readonly Sprite DefaultCardSprite = Resources.Load<Sprite>($"Spielkarten/rueckseite");
    
    private static Sprite LoadSprite(PlayingCardInfo cardInfo)
    {
        string pathSuffix = "";
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
                PlayingCardInfo cardInfo = new PlayingCardInfo
                    {
                        Suit = suit,
                        Rank = rank,
                    };
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
