using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayingCard
{
    public Suit CardSuit { get; set; }
    public Rank CardRank { get; set; }
    public bool IsTrump { get; set; }
    public Sprite Image { get; set; }

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
        Koenig,
        Zehn,
        Unter,
        Ober,
        Ass
    }

    public Sprite GetSprite()
    {
        return GetSprite(CardSuit, CardRank);
    }
    

    
    
    
    
    public static Sprite GetSprite(Suit suit, Rank rank)
    {
        string pathSuffix = "";
        pathSuffix += suit.ToString().ToLower();
        pathSuffix += GetRankFileSuffix(rank);
        return Resources.Load<Sprite>($"Spielkarten/{pathSuffix}");
    }

    private static string GetRankFileSuffix(Rank rank)
    {
        switch (rank)
        {
            case Rank.Sieben:
                return "07";
            case Rank.Acht:
                return "08";
            case Rank.Neun:
                return "09";
            case Rank.Koenig:
                return "ko";
            case Rank.Zehn:
                return "10";
            case Rank.Unter:
                return "un";
            case Rank.Ober:
                return "ob";
            case Rank.Ass:
                return "as";
            default:
                throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
        }
    }
}
