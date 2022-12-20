using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    public class TrumpListTests
    {
        private static List<PlayingCard.PlayingCardInfo> MakeCardList(IEnumerable<string> trumps)
        {
            return new List<PlayingCard.PlayingCardInfo>(
                trumps.Select(s => new PlayingCard.PlayingCardInfo(s.ToCharArray())));
        }

        [Test]
        public void TestTrumpListSauspiel()
        {
            var expectedTrumps = MakeCardList(new[]
            {
                "H7",
                "H8",
                "H9",
                "HK",
                "H1",
                "HA",
                "SU",
                "HU",
                "BU",
                "EU",
                "SO",
                "HO",
                "BO",
                "EO"
            });

            // the given suit should not matter
            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.Sauspiel, PlayingCard.Suit.Herz));

            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.Sauspiel, PlayingCard.Suit.Schelln));

            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.Sauspiel, PlayingCard.Suit.Eichel));
        }

        [Test]
        public void TestTrumpListSolo()
        {
            var expectedTrumps = MakeCardList(new[]
            {
                "H7",
                "H8",
                "H9",
                "HK",
                "H1",
                "HA",
                "SU",
                "HU",
                "BU",
                "EU",
                "SO",
                "HO",
                "BO",
                "EO"
            });

            // the given suit **should** matter here
            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.FarbSolo, PlayingCard.Suit.Herz));

            Assert.AreNotEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.FarbSolo, PlayingCard.Suit.Eichel));

            Assert.AreNotEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.FarbSolo, PlayingCard.Suit.Blatt));

            Assert.AreNotEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.FarbSolo, PlayingCard.Suit.Schelln));

            for (var i = 0; i < 6; i++)
                expectedTrumps[i] = new PlayingCard.PlayingCardInfo(PlayingCard.Suit.Schelln, expectedTrumps[i].rank);

            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.FarbSolo, PlayingCard.Suit.Schelln));
        }

        [Test]
        public void TestTrumpListWenz()
        {
            var expectedTrumps = MakeCardList(new[]
            {
                "SU",
                "HU",
                "BU",
                "EU"
            });

            // the given suit should not matter
            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.Wenz, PlayingCard.Suit.Herz));

            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.Wenz, PlayingCard.Suit.Schelln));

            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.Wenz, PlayingCard.Suit.Eichel));

            expectedTrumps.InsertRange(0, MakeCardList(new[]
            {
                "B7",
                "B8",
                "B9",
                "BO",
                "BK",
                "B1",
                "BA"
            }));

            Assert.AreEqual(expectedTrumps,
                GameManager.GetTrumpList(GameManager.RoundMode.FarbWenz, PlayingCard.Suit.Blatt));
        }
    }
}