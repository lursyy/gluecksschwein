using NUnit.Framework;

namespace Tests
{
    public class SoloStichTests : StichTestBase
    {
        public SoloStichTests() : base(GameManager.RoundMode.FarbSolo)
        {
        }

        [Test]
        public void StichWinnerSolo()
        {
            roundSuit = PlayingCard.Suit.Blatt;
            TestStichScenario(
                new[] {"S7", "B1", "H9", "EO"},
                new[] {"B1", "B1", "EO"}
            );

            TestStichScenario(
                new[] {"B9", "B8", "SA", "EK"},
                new[] {"B9", "B9", "B9"}
            );

            roundSuit = PlayingCard.Suit.Eichel;
            TestStichScenario(
                new[] {"B9", "B8", "SA", "EK"},
                new[] {"B9", "B9", "EK"}
            );

            TestStichScenario(
                new[] {"E9", "EU", "EA", "BA"},
                new[] {"EU", "EU", "EU"}
            );

            roundSuit = PlayingCard.Suit.Schelln;
            TestStichScenario(
                new[] {"B9", "B1", "SA", "EK"},
                new[] {"B1", "SA", "SA"}
            );

            TestStichScenario(
                new[] {"H7", "B1", "H9", "SU"},
                new[] {"H7", "H9", "SU"}
            );

            TestStichScenario(
                new[] {"E7", "B1", "H9", "SU"},
                new[] {"E7", "E7", "SU"}
            );
        }
    }
}