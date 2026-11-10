using HobGob.Events;
using Xunit;

namespace HobGob.Tests
{
    public class EventRulesTests
    {
        [Theory]
        [InlineData(3, 1, 28, 3)]
        [InlineData(8, 1, 28, 8)]
        [InlineData(5, 2, 28, 7)]
        [InlineData(5, 4, 60, 11)]
        public void OverflowCount_AddsTwoPerExtraPit(int roll, int pits, int cells, int expected)
        {
            Assert.Equal(expected, EventRules.OverflowCount(roll, pits, cells));
        }

        [Fact]
        public void OverflowCount_CappedByFreeCells()
        {
            Assert.Equal(4, EventRules.OverflowCount(8, 3, 4));
        }

        [Fact]
        public void OverflowCount_NoCells_IsZero()
        {
            Assert.Equal(0, EventRules.OverflowCount(8, 1, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void OverflowCount_NoPits_NeverSubtracts(int pits)
        {
            Assert.Equal(5, EventRules.OverflowCount(5, pits, 28));
        }

        [Theory]
        [InlineData(true, 0.29f, true)]
        [InlineData(true, 0.0f, true)]
        [InlineData(true, 0.3f, false)]
        [InlineData(true, 0.9f, false)]
        [InlineData(false, 0.1f, false)]
        public void IsPitStarving_NeedsHediffAndLowFood(bool fresh, float food, bool expected)
        {
            Assert.Equal(expected, EventRules.IsPitStarving(fresh, food));
        }

        [Theory]
        [InlineData(2, 50)]
        [InlineData(3, 50)]
        [InlineData(4, 60)]
        [InlineData(10, 150)]
        public void StrikeTribute_FifteenPerNoblinMinFifty(int noblins, int expected)
        {
            Assert.Equal(expected, EventRules.StrikeTribute(noblins));
        }

        [Theory]
        [InlineData(1f, 200, 30f, 30)]   // partial stack of silver
        [InlineData(1f, 30, 30f, 30)]    // exact
        [InlineData(1f, 10, 30f, 10)]    // stack smaller than needed
        [InlineData(7f, 20, 30f, 5)]     // 5 x 7 = 35, overpays by one unit's rounding
        [InlineData(400f, 1, 30f, 1)]    // one sculpture covers it
        [InlineData(1f, 200, 0f, 0)]     // nothing owed
        [InlineData(1f, 200, -5f, 0)]
        [InlineData(0f, 200, 30f, 0)]    // worthless
        [InlineData(1f, 0, 30f, 0)]
        public void UnitsToTake_CoversRemainder(float unitValue, int stack, float remaining, int expected)
        {
            Assert.Equal(expected, EventRules.UnitsToTake(unitValue, stack, remaining));
        }

        [Theory]
        [InlineData(10, 75, 10)]   // whole stack fits
        [InlineData(500, 75, 75)]  // one carry load
        [InlineData(10, 0, 0)]     // hands full
        [InlineData(0, 75, 0)]
        [InlineData(10, -3, 0)]
        public void CarryCount_AtMostOneLoad(int stack, int space, int expected)
        {
            Assert.Equal(expected, EventRules.CarryCount(stack, space));
        }

        [Theory]
        [InlineData(0, 1000, 11000)]      // first pass
        [InlineData(11000, 5000, 15000)]  // passed again later: extends
        [InlineData(30000, 5000, 30000)]  // never shortens
        public void HoldExpiry_ExtendsNeverShortens(int existing, int now, int expected)
        {
            Assert.Equal(expected, EventRules.HoldExpiry(existing, now));
        }

        [Theory]
        [InlineData(9999, 10000, false)]
        [InlineData(10000, 10000, true)]
        [InlineData(10001, 10000, true)]
        public void HoldExpired_AtOrAfterExpiry(int now, int expiry, bool expected)
        {
            Assert.Equal(expected, EventRules.HoldExpired(now, expiry));
        }

        [Theory]
        [InlineData(100, 0, 40f, false, true)]   // no target yet
        [InlineData(100, 0, 5f, true, true)]     // arrived
        [InlineData(2600, 0, 40f, true, true)]   // leg too old
        [InlineData(100, 0, 40f, true, false)]   // still running
        public void NeedsNewLeg_Cases(int now, int start, float distance, bool valid, bool expected)
        {
            Assert.Equal(expected, EventRules.NeedsNewLeg(now, start, distance, valid));
        }
    }
}
