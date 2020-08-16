using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

using Card = PlayingCard.PlayingCardInfo;

public static class Extensions
{
    private static readonly Random Rng = new Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static T CycleNext<T>(this IList<T> list, T current) => list.Cycle(current, 1);

    public static T CyclePrev<T>(this IList<T> list, T current) => list.Cycle(current, -1);

    public static T Cycle<T>(this IList<T> list, T current, int amount)
    {
        int elemIndex = list.IndexOf(current);
        if (elemIndex == -1)
        {
            throw new InvalidOperationException($"{nameof(current)} not in list");
        }
        if (amount < -list.Count)
        {
            throw new InvalidOperationException("cannot cycle backwards this much on one step");
        }
        return list[(elemIndex + list.Count + amount) % list.Count];
    }


    /// <summary>
    /// Determines which card won the Stich.
    /// </summary>
    /// <param name="stich">The stich whose winner to determine</param>
    /// <param name="roundMode">The used round mode</param>
    /// <param name="roundSuit">The round suit (solo/wenz: additional trump suit, sauspiel: sought ace suit)</param>
    /// <returns>The winning card of the Stich</returns>
    /// <exception cref="ArgumentException"></exception>
    public static Card CalculateWinningCard(this PlayingCard.Stich stich,
        GameManager.RoundMode roundMode, PlayingCard.Suit roundSuit)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}:" + 
                  "Calulating Winning card");
        Assert.AreNotEqual(0, stich.CardCount);
        
        // base case: Stich contains a single card, which is the "winner"
        if (stich.CardCount == 1) return stich.Top;

        // otherwise we have to compare the topmost card...
        var topCard = stich.RemoveTop();
        
        // ...with the "winner" of the rest of the cards
        var lastWinner = stich.CalculateWinningCard(roundMode, roundSuit);
        List<Card> trumps = GameManager.GetTrumpList(roundMode, roundSuit);
        var currentWinner = CompareCards(lastWinner, topCard, trumps);
        
        // add the top card back so we don't change the Stich
        stich.AddCard(topCard);
        
        return currentWinner;
    }

    private static Card CompareCards(Card bottomCard, Card topCard, List<Card> trumps)
    {
        int bottomCardPrecedence = trumps.IndexOf(bottomCard);
        int topCardPrecedence = trumps.IndexOf(topCard);

        if (bottomCardPrecedence == -1 && topCardPrecedence == -1)
        {
            // Neither of the cards are trumps.
            // In order to win, the top card has to match the suit and have a higher rank
            return topCard.suit == bottomCard.suit && topCard.rank > bottomCard.rank ? 
                topCard : bottomCard;
        }
        
        // Both cards having the same precedence means that something is really wrong.
        Assert.AreNotEqual(bottomCardPrecedence, topCardPrecedence);
        
        // In all other cases, we can simply use the precedence to determine the winner.
        // This covers the following cases:
        // * one card is a trump (because non-trumps have precedence -1 due to indexOf)
        // * both cards are trumps, in which case the precedence also determines the winner
        return bottomCardPrecedence > topCardPrecedence ? bottomCard : topCard;
    }

    public struct ScoreBoardRow
    {
        public int EntryCount { get; private set; }

        public ScoreBoardEntry[] entries;

        public void AddEntry(string name, int score)
        {
            if (entries == null)
            {
                entries = new ScoreBoardEntry[4];
            }

            var existingEntry = entries.Take(EntryCount).ToList().Find(entry => entry.name.Equals(name));
            
            // we have to do a separate check for the name because we cannot do a null check on a struct
            bool nameExists = existingEntry.name != null && existingEntry.name.Equals(name);
            
            if (nameExists)
            {
                // add score to the existing entry (existingEntry is just a copy, apparently) 
                int entryIndex = Array.IndexOf(entries, existingEntry);
                entries[entryIndex].score+=score;
            }
            else if (EntryCount == 4)
            {
                throw new InvalidOperationException("Already 4 distinct names in this row");
            }
            else // we have space for adding the new name
            {
                entries[EntryCount] = new ScoreBoardEntry(name, score);
                EntryCount++;
            }
        }

        public override string ToString()
        {
            return string.Join(" | ", entries.Take(EntryCount));
        }
    }

    public struct ScoreBoardEntry
    {
        public readonly string name;
        public int score;

        internal ScoreBoardEntry(string name, int score)
        {
            this.name = name;
            this.score = score;
        }
            
        public override string ToString()
        {
            return $"{name}:{score}";
        }
    }
}