using NUnit.Framework;

namespace Tests
{
    public class FarbWenzStichTests : StichTestBase
    {
        public FarbWenzStichTests() : base(GameManager.RoundMode.Wenz)
        {
        }

        [Test]
        public void StichWinnerWenzHerz()
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
    }
}