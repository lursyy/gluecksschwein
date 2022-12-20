using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.Assertions;
using Card = PlayingCard.PlayingCardInfo;

public static class Extensions
{
    private static readonly Random Rng = new();

    // TODO duplicate code...
    public static void Shuffle<T>(this NetworkList<T> list) where T : unmanaged, IEquatable<T>
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public static T CycleNext<T>(this IList<T> list, T current)
    {
        return list.Cycle(current, 1);
    }

    public static T CyclePrev<T>(this IList<T> list, T current)
    {
        return list.Cycle(current, -1);
    }

    public static T Cycle<T>(this IList<T> list, T current, int amount)
    {
        var elemIndex = list.IndexOf(current);
        if (elemIndex == -1) throw new InvalidOperationException($"{nameof(current)} not in list");
        if (amount < -list.Count) throw new InvalidOperationException("cannot cycle backwards this much on one step");
        return list[(elemIndex + list.Count + amount) % list.Count];
    }


    /// <summary>
    ///     Determines which card won the Stich.
    /// </summary>
    /// <param name="stich">The stich whose winner to determine</param>
    /// <param name="roundMode">The used round mode</param>
    /// <param name="roundSuit">The round suit (solo/wenz: additional trump suit, sauspiel: sought ace suit)</param>
    /// <returns>The winning card of the Stich</returns>
    /// <exception cref="ArgumentException"></exception>
    public static Card CalculateWinningCard(this PlayingCard.Stich stich,
        GameManager.RoundMode roundMode, PlayingCard.Suit roundSuit)
    {
        Assert.AreNotEqual(0, stich.CardCount);

        // base case: Stich contains a single card, which is the "winner"
        if (stich.CardCount == 1) return stich.Top;

        // otherwise we have to compare the topmost card...
        var topCard = stich.RemoveTop();

        // ...with the "winner" of the rest of the cards
        var lastWinner = stich.CalculateWinningCard(roundMode, roundSuit);
        var trumps = GameManager.GetTrumpList(roundMode, roundSuit);
        var currentWinner = CompareCards(lastWinner, topCard, trumps);

        // add the top card back so we don't change the Stich
        stich.AddCard(topCard);

        return currentWinner;
    }

    private static Card CompareCards(Card bottomCard, Card topCard, IList<Card> trumps)
    {
        var bottomCardPrecedence = trumps.IndexOf(bottomCard);
        var topCardPrecedence = trumps.IndexOf(topCard);

        if (bottomCardPrecedence == -1 && topCardPrecedence == -1)
            // Neither of the cards are trumps.
            // In order to win, the top card has to match the suit and have a higher rank
            return topCard.Suit == bottomCard.Suit && topCard.Rank > bottomCard.Rank ? topCard : bottomCard;

        // Both cards having the same precedence means that something is really wrong.
        Assert.AreNotEqual(bottomCardPrecedence, topCardPrecedence);

        // In all other cases, we can simply use the precedence to determine the winner.
        // This covers the following cases:
        // * one card is a trump (because non-trumps have precedence -1 due to indexOf)
        // * both cards are trumps, in which case the precedence also determines the winner
        return bottomCardPrecedence > topCardPrecedence ? bottomCard : topCard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ScoreBoardRow : INetworkSerializable, IEquatable<ScoreBoardRow>
    {
        // TODO this can't be right... how can I use an array here?
        private ScoreBoardEntry entry1;
        private ScoreBoardEntry entry2;
        private ScoreBoardEntry entry3;
        private ScoreBoardEntry entry4;

        public ScoreBoardEntry[] Entries
        {
            get { return new[] { entry1, entry2, entry3, entry4 }; }
        }

        public ScoreBoardRow(IReadOnlyList<ScoreBoardEntry> entries)
        {
            if (entries.Count != 4) throw new InvalidOperationException("Expecting 4 entries");
            entry1 = entries[0];
            entry2 = entries[1];
            entry3 = entries[2];
            entry4 = entries[3];
        }

        public bool Equals(ScoreBoardRow other)
        {
            return entry1.Equals(other.entry1) &&
                   entry2.Equals(other.entry2) &&
                   entry3.Equals(other.entry3) &&
                   entry4.Equals(other.entry4);
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref entry1);
            serializer.SerializeValue(ref entry2);
            serializer.SerializeValue(ref entry3);
            serializer.SerializeValue(ref entry4);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ScoreBoardEntry : INetworkSerializable, IEquatable<ScoreBoardEntry>
    {
        // TODO check if 32 bytes is enough
        public FixedString32Bytes name;
        public int score;

        public ScoreBoardEntry(string name, int score)
        {
            this.name = name;
            this.score = score;
        }

        public override string ToString()
        {
            return $"{name}:{score}";
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref score);
        }

        public bool Equals(ScoreBoardEntry other)
        {
            return name.Equals(other.name) && score == other.score;
        }

        public override bool Equals(object obj)
        {
            return obj is ScoreBoardEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (name.GetHashCode() * 397) ^ score;
            }
        }
    }
}