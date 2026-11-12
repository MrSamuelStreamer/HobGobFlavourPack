using HobGob.Rumble;
using Xunit;

namespace HobGob.Tests
{
    public class RumbleRulesTests
    {
        [Theory]
        [InlineData(24, false)]
        [InlineData(25, true)]
        [InlineData(100, true)]
        [InlineData(0, false)]
        public void EnoughNoblins_NeedsTwentyFive(int eligible, bool expected)
        {
            Assert.Equal(expected, RumbleRules.EnoughNoblins(eligible));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, RumbleOutcome.Wiped)]
        [InlineData(1, 1, 1, 0, RumbleOutcome.LastOneLeft)]
        [InlineData(1, 0, 0, 0, RumbleOutcome.LastOneLeft)]
        [InlineData(3, 0, 0, 0, RumbleOutcome.AllDown)]
        [InlineData(5, 4, 0, 0, RumbleOutcome.OnlyHidersLeft)]
        [InlineData(2, 1, 1, 0, RumbleOutcome.Continue)]
        [InlineData(50, 40, 30, 59999, RumbleOutcome.Continue)]
        [InlineData(50, 40, 30, 60000, RumbleOutcome.TimedOut)]
        public void Resolve_EndStates(int alive, int standing, int ableStanding, int elapsed, RumbleOutcome expected)
        {
            Assert.Equal(expected, RumbleRules.Resolve(alive, standing, ableStanding, elapsed));
        }

        [Theory]
        [InlineData(RumbleOutcome.LastOneLeft, true)]
        [InlineData(RumbleOutcome.OnlyHidersLeft, true)]
        [InlineData(RumbleOutcome.AllDown, false)]
        [InlineData(RumbleOutcome.TimedOut, false)]
        [InlineData(RumbleOutcome.Wiped, false)]
        public void PunishesSurvivors_OnlyForHiders(RumbleOutcome outcome, bool expected)
        {
            Assert.Equal(expected, RumbleRules.PunishesSurvivors(outcome));
        }

        [Fact]
        public void Resolve_LastOneBeatsTimeout()
        {
            Assert.Equal(RumbleOutcome.LastOneLeft, RumbleRules.Resolve(1, 1, 1, RumbleRules.MaxDurationTicks * 2));
        }
    }
}
