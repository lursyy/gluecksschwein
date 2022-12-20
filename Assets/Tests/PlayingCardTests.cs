using NUnit.Framework;

namespace Tests
{
    public class PlayingCardTests
    {
        [Test]
        public void PlayingCardEquals()
        {
            var eichelOber = new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Eichel, PlayingCard.Rank.Ober);
            var herzNeun = new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Herz, PlayingCard.Rank.Neun);
            var eichelAss = new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Eichel, PlayingCard.Rank.Ass);
            var eichelOber2 = new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Eichel, PlayingCard.Rank.Ober);
            Assert.AreNotEqual(eichelOber, herzNeun);
            Assert.AreNotEqual(eichelOber, eichelAss);
            Assert.AreEqual(eichelOber, eichelOber2);
            Assert.AreNotSame(eichelOber, eichelOber2);
        }

        [Test]
        public void CardSpritesLoadingCorrectly()
        {
            var cardDeck = PlayingCard.InitializeCardDeck();
            CollectionAssert.AllItemsAreNotNull(cardDeck);
        }

    }
}
