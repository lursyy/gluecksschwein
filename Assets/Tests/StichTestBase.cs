using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    public class StichTestBase
    {
        private readonly GameManager.RoundMode _roundMode;
        private readonly PlayingCard.Suit _roundSuit;

        protected StichTestBase(GameManager.RoundMode roundMode, PlayingCard.Suit roundSuit)
        {
            _roundMode = roundMode;
            _roundSuit = roundSuit;
        }
        
        protected void TestStichScenario(string[] stichCards, string[] expectedWinners)
        {
            Assert.AreEqual(4, stichCards.Length);
            Assert.AreEqual(3, expectedWinners.Length);
            
            var stichList = 
                stichCards.Select(s => new PlayingCard.PlayingCardInfo(s.ToCharArray()));
            var winnerList = 
                expectedWinners.Select(s => new PlayingCard.PlayingCardInfo(s.ToCharArray()));
            
            TestStichScenario(stichList, winnerList);
        }

        private void TestStichScenario(IEnumerable<PlayingCard.PlayingCardInfo> stichCards,
            IEnumerable<PlayingCard.PlayingCardInfo> expectedWinnerCards)
        {
            PlayingCard.Stich stich = new PlayingCard.Stich();
            stich.AddAll(stichCards);

            PlayingCard.Stich expectedWinners = new PlayingCard.Stich();
            expectedWinners.AddAll(expectedWinnerCards);

            Assert.That(stich.CardCount == 4 && expectedWinners.CardCount == 3);

            while (expectedWinners.CardCount > 0)
            {
                PlayingCard.PlayingCardInfo winner = stich.CalculateWinningCard(_roundMode, _roundSuit);
                Assert.AreEqual(expectedWinners.Top, winner);
                expectedWinners.RemoveTop();
                stich.RemoveTop();
            }

            Assert.AreEqual(1, stich.CardCount);
        }
        
        
    }
}