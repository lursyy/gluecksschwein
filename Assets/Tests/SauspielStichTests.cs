using NUnit.Framework;

namespace Tests
{
    public class SauspielStichTest : StichTestBase
    {
        public SauspielStichTest() : base(GameManager.RoundMode.Sauspiel, PlayingCard.Suit.Herz)
        {
        }

        [Test]
        public void StichWinnerSauspiel()
        {
            TestStichScenario(
                new[] {"EK", "B1", "HA", "SU"},
                new[] {"EK", "HA", "SU"});

            TestStichScenario(
                new[] {"S7", "B1", "H9", "EO"},
                new[] {"S7", "H9", "EO"}
            );

            TestStichScenario(
                new[] {"B9", "B8", "SA", "EK"},
                new[] {"B9", "B9", "B9"}
            );

            TestStichScenario(
                new[] {"B9", "B1", "SA", "EK"},
                new[] {"B1", "B1", "B1"}
            );

            TestStichScenario(
                new[] {"EA", "EK", "EU", "BA"},
                new[] {"EA", "EU", "EU"}
            );

            TestStichScenario(
                new[] {"H7", "SU", "B1", "H9"},
                new[] {"SU", "SU", "SU"}
            );

            TestStichScenario(
                new[] {"H7", "SU", "EO", "H9"},
                new[] {"SU", "EO", "EO"}
            );
            TestStichScenario(
                new[] {"B8", "B9", "B1", "BK"},
                new[] {"B9", "B1", "B1"}
            );
        }
    }
}