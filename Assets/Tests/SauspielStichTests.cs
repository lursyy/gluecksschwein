using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    public class SauspielStichTest : StichTestBase
    {
        public SauspielStichTest() : base(GameManager.RoundMode.Sauspiel, PlayingCard.Suit.Herz) { }

        [Test]
        public void StichWinnerSauspiel()
        {
            TestStichScenario(new[] {"EK", "B1", "HA", "SU"},
                new[] {"EK", "HA", "SU"});
            
            

            // TODO many more Tests
        }

    }
}