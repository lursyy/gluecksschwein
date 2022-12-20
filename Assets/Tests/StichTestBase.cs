using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    /// <summary>
    ///     This class provides methods to easily test Stich Winner results.
    ///     Simply extend this class, providing a constructor with the desired round mode and -suit.
    /// </summary>
    public class StichTestBase
    {
        private readonly GameManager.RoundMode _roundMode;
        protected PlayingCard.Suit roundSuit;

        protected StichTestBase(GameManager.RoundMode roundMode, PlayingCard.Suit roundSuit = PlayingCard.Suit.Herz)
        {
            _roundMode = roundMode;
            this.roundSuit = roundSuit;
        }

        /// <summary>
        ///     Compares the stich winner (and intermediate winners) against the given list of expected winners.
        /// </summary>
        /// <param name="cards">the 4 cards of the Stich</param>
        /// <param name="winners">
        ///     the 3 expected intermediate winners
        ///     (i.e. card1 vs card2, card2 vs card3, card3 vs card4)
        /// </param>
        protected void TestStichScenario(string[] cards, string[] winners)
        {
            Assert.AreEqual(4, cards.Length);
            Assert.AreEqual(3, winners.Length);

            var stichList =
                cards.Select(s => new PlayingCard.PlayingCardInfo(s.ToCharArray()));
            var winnerList =
                winners.Select(s => new PlayingCard.PlayingCardInfo(s.ToCharArray()));

            TestStichScenario(stichList, winnerList);
        }

        private void TestStichScenario(IEnumerable<PlayingCard.PlayingCardInfo> stichCards,
            IEnumerable<PlayingCard.PlayingCardInfo> expectedWinnerCards)
        {
            var stich = new PlayingCard.Stich();
            stich.AddAll(stichCards);

            var expectedWinners = new PlayingCard.Stich();
            expectedWinners.AddAll(expectedWinnerCards);

            Assert.That(stich.CardCount == 4 && expectedWinners.CardCount == 3);

            while (expectedWinners.CardCount > 0)
            {
                var winner = stich.CalculateWinningCard(_roundMode, roundSuit);
                Assert.AreEqual(expectedWinners.Top, winner,
                    $"Winner of Stich [{string.Join(", ", stich.Cards)}]");
                expectedWinners.RemoveTop();
                stich.RemoveTop();
            }

            Assert.AreEqual(1, stich.CardCount);
        }
    }
}