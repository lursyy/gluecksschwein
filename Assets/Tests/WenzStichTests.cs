using NUnit.Framework;

namespace Tests
{
    public class FarbWenzStichTests : StichTestBase
    {
        public FarbWenzStichTests() : base(GameManager.RoundMode.Wenz)
        {
        }

        [Test]
        public void StichWinnerWenz()
        {
            TestStichScenario(
                new[] {"S7", "B1", "H9", "EO"},
                new[] {"S7", "S7", "S7"}
            );

            TestStichScenario(
                new[] {"B1", "BK", "SO", "HU"},
                new[] {"B1", "B1", "HU"}
            );

            TestStichScenario(
                new[] {"B9", "B1", "SA", "EK"},
                new[] {"B1", "B1", "B1"}
            );

            TestStichScenario(
                new[] {"EA", "EK", "EU", "HO"},
                new[] {"EA", "EU", "EU"}
            );
        }

        [Test]
        public void StichWinnerWenzOber()
        {
            TestStichScenario(
                new[] {"S9", "H1", "SO", "SK"},
                new[] {"S9", "SO", "SK"});

            TestStichScenario(
                new[] {"S9", "SK", "SO", "S1"},
                new[] {"SK", "SK", "S1"});

            TestStichScenario(
                new[] {"S9", "SA", "SO", "S1"},
                new[] {"SA", "SA", "SA"});

            TestStichScenario(
                new[] {"S9", "EO", "SO", "S1"},
                new[] {"S9", "SO", "S1"});
        }
    }
}