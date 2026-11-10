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
    }
}
