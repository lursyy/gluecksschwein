using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    public class SauspielStichTest
    {
        private GameManager.RoundMode _roundMode = GameManager.RoundMode.Sauspiel;
        private readonly PlayingCard.Suit _roundSuit = PlayingCard.Suit.Herz; // does not matter

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

        [Test]
        public void StichWinnerSauspiel()
        {
            TestStichScenario(new[]
                {
                    new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Eichel, PlayingCard.Rank.Koenig),
                    new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Blatt, PlayingCard.Rank.Zehn),
                    new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Herz, PlayingCard.Rank.Acht),
                    new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Schelln, PlayingCard.Rank.Unter)
                },
                new[]
                {
                    new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Eichel, PlayingCard.Rank.Koenig),
                    new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Herz, PlayingCard.Rank.Acht),
                    new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Schelln, PlayingCard.Rank.Unter)
                }
            );
            
            // TODO many more Tests

        }
    }
}