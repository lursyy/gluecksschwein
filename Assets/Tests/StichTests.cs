using NUnit.Framework;

namespace Tests
{
    public class StichTests
    {
        #region Stich Worth Tests
        
        [Test]
        public void TODO_CalculateStichWorthSau()
        {
            var stich = new PlayingCard.Stich();
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Eichel, PlayingCard.Rank.Koenig));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Blatt, PlayingCard.Rank.Zehn));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Herz, PlayingCard.Rank.Acht));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Schelln, PlayingCard.Rank.Unter));
            
            int stichWorth = stich.Worth;
            // TODO this is going to fail because I don't know how to calculate the stich worth, so I cannot come up with a test
            Assert.AreEqual(42, stichWorth);
        }
        
        [Test]
        public void TODO_CalculateStichWorthSolo()
        {
            var stich = new PlayingCard.Stich();
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Schelln, PlayingCard.Rank.Koenig));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Herz, PlayingCard.Rank.Koenig));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Blatt, PlayingCard.Rank.Ober));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Schelln, PlayingCard.Rank.Sieben));
            
            int stichWorth = stich.Worth;
            // TODO this is going to fail because I don't know how to calculate the stich worth, so I cannot come up with a test
            Assert.AreEqual(-5, stichWorth);
        }
        
        #endregion

        #region Stich Winner Tests
        
        [Test]
        public void TODO_CalculateStichWinnerSau()
        {
            var stich = new PlayingCard.Stich();
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Eichel, PlayingCard.Rank.Koenig));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Blatt, PlayingCard.Rank.Zehn));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Herz, PlayingCard.Rank.Acht));
            stich.AddCard(new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Schelln, PlayingCard.Rank.Unter));

            stich.CalculateWinningCard(GameManager.RoundMode.Sauspiel, PlayingCard.Suit.Eichel);
            
            int stichWorth = stich.Worth;
            // TODO this is going to fail because I don't know how to calculate the stich worth, so I cannot come up with a test
            Assert.AreEqual(42, stichWorth);
        }

        #endregion
        
    }
}
