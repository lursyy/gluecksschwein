using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static GameManager;
using static PlayingCard;
using Random = System.Random;

namespace Tests
{
    public class GameLogicTests
    {
        private readonly List<Player> _players = new List<Player>();
        private readonly List<PlayingCardInfo> _cardDeck = InitializeCardDeck();
        private readonly Random _rng = new Random();

        /// <summary>
        /// Creates 4 players with random cards
        /// </summary>
        [SetUp]
        public void Setup()
        {
            for (int i = 0; i < 4; i++)
            {
                _players.Add(new GameObject($"Player_{i}").AddComponent<Player>());
            }

            _cardDeck.Shuffle();

            int dealtCards = 0;
            foreach (var player in _players)
            {
                // I am not sure if this works
                player.handCards.InitializeBehaviour(player, player.GetHashCode());
                
                for (int i = dealtCards; i < dealtCards + 8; i++)
                {
                    player.handCards.Add(_cardDeck[i]);
                }

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
            Player schellnSauOwner =
                _players.Find(p => p.handCards.Contains(new PlayingCardInfo(Suit.Schelln, Rank.Ass)));

            // select one of the other three players to be the sau player
            var otherPlayers = _players.Except(new[] {schellnSauOwner}).ToList();
            otherPlayers.Shuffle();
            Player schellnSauSeeker = otherPlayers[0];
            otherPlayers.Remove(schellnSauSeeker);
            
            Assert.AreEqual(2, otherPlayers.Count);
            
            // everyone chooses "weiter" except the sau seeker
            Dictionary<Player, PreRoundChoice> playerChoices = new Dictionary<Player, PreRoundChoice>
            {
                [schellnSauOwner] = PreRoundChoice.Weiter,
                [otherPlayers[0]] = PreRoundChoice.Weiter,
                [schellnSauSeeker] = PreRoundChoice.SauspielSchelln,
                [otherPlayers[1]] = PreRoundChoice.Weiter
            };

            List<IEnumerable<Player>> actualRoundGroups =
                CalculateRoundGroups(playerChoices, _players, RoundMode.SauspielSchelln);

            var expectedRoundGroups = new List<IEnumerable<Player>>
            {
                new[] {schellnSauOwner, schellnSauSeeker},
                new[] {otherPlayers[0], otherPlayers[1]}
            };

            AssertAreEquivalent(expectedRoundGroups, actualRoundGroups);
        }

        private static void AssertAreEquivalent(List<IEnumerable<Player>> expectedGroups,
            List<IEnumerable<Player>> actualGroups)
        {
            Assert.AreEqual(expectedGroups.Count, actualGroups.Count);
            foreach (var expectedGroup in expectedGroups.ToList())
            {
                // find the expected Group matching the actual group and remove it
                IEnumerable<Player> matchingExpectedGroup =
                    expectedGroups.Find(g => g.SequenceEqual(expectedGroup));

                Assert.NotNull(matchingExpectedGroup);
                Assert.True(expectedGroups.Remove(matchingExpectedGroup));
            }
            
            // we should have removed every element from the expected groups
            Assert.AreEqual(0, expectedGroups.Count);
        }
    }
}