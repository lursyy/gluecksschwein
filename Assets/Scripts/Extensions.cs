using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = System.Random;

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


    public static PlayingCard.PlayingCardInfo CalculateWinningCard(this PlayingCard.Stich stich, PlayingCard.Suit trumpSuit)
    {
        if (!stich.IsComplete)
        {
            throw new InvalidOperationException("Cannot calculate winning card for incomplete stich");
        }
        
        Debug.LogWarning($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}:" +
                       "Dummy calculation, simply picking random card");

        // TODO actual implementation (add parameters that are necessary for calculation, e.g. current Trump Suit)
        int winningCardIndex = Rng.Next(4);
        PlayingCard.PlayingCardInfo winningCard = stich.GetCard(winningCardIndex);

        return winningCard;
    }

    public static int CalculateStichWorth(this PlayingCard.Stich stich, GameManager.RoundMode roundMode)
    {
        if (!stich.IsComplete)
        {
            throw new InvalidOperationException("Cannot calculate winning card for incomplete stich");
        }
        
        Debug.LogWarning($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}:" +
                       "Dummy calculation, simply returning random number");

        int stichWorth = 0;

        for (int i = 0; i < stich.CardCount; i++)
        {
            // TODO actual implementation (add parameters that are necessary for calculation, e.g. current Trump Suit)
            int cardWorth = Rng.Next(10);
            stichWorth += cardWorth;
        }
        
        return stichWorth;
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