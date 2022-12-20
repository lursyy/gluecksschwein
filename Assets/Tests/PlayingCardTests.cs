using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    public class PlayingCardTests
    {
        private List<PlayingCard.PlayingCardInfo> _cardDeck;

        [SetUp]
        public void Setup()
        {
            _cardDeck = PlayingCard.InitializeCardDeck();
        }

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
            CollectionAssert.AllItemsAreNotNull(_cardDeck);
        }

        [Test]
        public void CheckTotalCardWorth()
        {
            var totalCardWorth = _cardDeck.Sum(c => c.Worth);
            Assert.AreEqual(120, totalCardWorth);
        }
    }
}