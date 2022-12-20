using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static GameManager;
using static PlayingCard;

namespace Tests
{
    public class GameLogicTests
    {
        private readonly List<PlayingCardInfo> _cardDeck = InitializeCardDeck();
        private readonly List<Player> _players = new List<Player>();

        /// <summary>
        ///     Creates 4 players with random cards
        /// </summary>
        [SetUp]
        public void Setup()
        {
            for (var i = 0; i < 4; i++) _players.Add(new GameObject($"Player_{i}").AddComponent<Player>());

            _cardDeck.Shuffle();

            var dealtCards = 0;
            foreach (var player in _players)
            {
                // I am not sure why this is necessary, but it works...
                player.handCards.InitializeBehaviour(player, player.GetHashCode());

                for (var i = dealtCards; i < dealtCards + 8; i++) player.handCards.Add(_cardDeck[i]);

                dealtCards += 8;
            }
        }

        [TearDown]
        public void Teardown()
        {
            _players.Clear();
        }

        [Test]
        public void CalculateRoundGroupsSau()
        {
            // find the player who has the schelln sau
            var schellnSauOwner =
                _players.Find(p => p.handCards.Contains(new PlayingCardInfo(Suit.Schelln, Rank.Ass)));

            // select one of the other three players to be the sau player
            var otherPlayers = _players.Except(new[] {schellnSauOwner}).ToList();
            otherPlayers.Shuffle();
            var schellnSauSeeker = otherPlayers[0];
            otherPlayers.Remove(schellnSauSeeker);

            Assert.AreEqual(2, otherPlayers.Count);

            var actualRoundGroups =
                CalculateRoundGroups(_players, schellnSauSeeker, RoundMode.Sauspiel, Suit.Schelln);

            var expectedRoundGroups = new List<List<Player>>
            {
                new List<Player> {schellnSauOwner, schellnSauSeeker},
                new List<Player> {otherPlayers[0], otherPlayers[1]}
            };

            AssertAreEquivalent(expectedRoundGroups, actualRoundGroups);
        }

        private static void AssertAreEquivalent(List<List<Player>> expectedGroups,
            List<List<Player>> actualGroups)
        {
            Assert.AreEqual(expectedGroups.Count, actualGroups.Count);
            foreach (var expectedGroup in expectedGroups.ToList())
            {
                // find the expected Group matching the actual group and remove it
                var matchingExpectedGroup =
                    expectedGroups.Find(g => g.SequenceEqual(expectedGroup));

                Assert.NotNull(matchingExpectedGroup);
                Assert.True(expectedGroups.Remove(matchingExpectedGroup));
            }

            // we should have removed every element from the expected groups
            Assert.AreEqual(0, expectedGroups.Count);
        }

        [Test]
        public void ScoreBoardRow_AddSingleEntry()
        {
            var row = new Extensions.ScoreBoardRow();
            Assert.AreEqual(0, row.EntryCount);

            row.AddEntry("Luis", 42);

            Assert.AreEqual(1, row.EntryCount);
        }

        [Test]
        public void ScoreBoardRow_Add4Entries()
        {
            var row = new Extensions.ScoreBoardRow();
            Assert.AreEqual(0, row.EntryCount);

            row.AddEntry("Lukian", 42);
            Assert.AreEqual(1, row.EntryCount);

            row.AddEntry("Nuria", 26);
            Assert.AreEqual(2, row.EntryCount);

            row.AddEntry("Lena", 13);
            Assert.AreEqual(3, row.EntryCount);

            row.AddEntry("Meo", 99);
            Assert.AreEqual(4, row.EntryCount);
        }

        [Test]
        public void ScoreBoardRow_AddTooMuchEntries()
        {
            var row = new Extensions.ScoreBoardRow();
            Assert.AreEqual(0, row.EntryCount);

            row.AddEntry("Lukian", 51);
            row.AddEntry("Nuria", 26);
            row.AddEntry("Lena", 13);
            row.AddEntry("Meo", 99);

            Assert.AreEqual(4, row.EntryCount);

            Assert.Catch<InvalidOperationException>(() => row.AddEntry("Luis", 42));

            Assert.AreEqual(4, row.EntryCount);
        }

        [Test]
        public void ScoreBoardRow_AmendExistingEntry()
        {
            var row = new Extensions.ScoreBoardRow();
            Assert.AreEqual(0, row.EntryCount);

            row.AddEntry("Luis", 15);
            Assert.AreEqual(1, row.EntryCount);
            Assert.AreEqual(15, row.entries[0].score);

            row.AddEntry("Luis", 27);
            Assert.AreEqual(1, row.EntryCount);
            Assert.AreEqual(42, row.entries[0].score);
        }
    }
}