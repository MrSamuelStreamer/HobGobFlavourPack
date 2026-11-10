using HobGob.Census;
using Xunit;

namespace HobGob.Tests
{
    public class CensusRulesTests
    {
        private static readonly int[] Mins = { 0, 6, 11, 21 };

        [Theory]
        [InlineData(0, 0)]
        [InlineData(5, 0)]
        [InlineData(6, 1)]
        [InlineData(10, 1)]
        [InlineData(11, 2)]
        [InlineData(21, 3)]
        [InlineData(999, 3)]
        public void TierIndexFor_PicksHighestReachedTier(int count, int expected)
        {
            Assert.Equal(expected, CensusRules.TierIndexFor(count, Mins));
        }

        [Fact]
        public void TierIndexFor_BelowLowestTier_IsZero()
        {
            Assert.Equal(0, CensusRules.TierIndexFor(2, new[] { 5, 10 }));
        }

        [Theory]
        [InlineData(0, 0, 6)]
        [InlineData(5, 0, 1)]
        [InlineData(6, 1, 5)]
        public void MoreUntilNext_CountsToNextMinimum(int count, int tier, int expected)
        {
            Assert.Equal(expected, CensusRules.MoreUntilNext(count, tier, Mins));
        }

        [Fact]
        public void MoreUntilNext_TopTier_IsNull()
        {
            Assert.Null(CensusRules.MoreUntilNext(500, 3, Mins));
        }

        [Fact]
        public void MoreUntilNext_NeverBelowOne()
        {
            // Stale tier index (count already past the next minimum) still asks for at least one goblin.
            Assert.Equal(1, CensusRules.MoreUntilNext(15, 0, Mins));
        }

        [Theory]
        [InlineData(0, 1, CensusChange.Promoted)]
        [InlineData(0, 3, CensusChange.Promoted)]
        [InlineData(2, 1, CensusChange.Demoted)]
        [InlineData(2, 2, CensusChange.NoChange)]
        public void Compare_ReportsDirection(int previous, int next, CensusChange expected)
        {
            Assert.Equal(expected, CensusRules.Compare(previous, next));
        }

        [Fact]
        public void IsDue_WrongHour_False()
        {
            Assert.False(CensusRules.IsDue(10, 11, 5, 4));
        }

        [Fact]
        public void IsDue_SameDay_False()
        {
            Assert.False(CensusRules.IsDue(11, 11, 5, 5));
        }

        [Fact]
        public void IsDue_NewDayAtCensusHour_True()
        {
            Assert.True(CensusRules.IsDue(11, 11, 5, 4));
            Assert.True(CensusRules.IsDue(11, 11, 0, -1));
        }

        [Fact]
        public void DayStamp_UniqueAcrossYears()
        {
            Assert.NotEqual(CensusRules.DayStamp(5500, 59, 60), CensusRules.DayStamp(5501, 0, 60));
            Assert.Equal(CensusRules.DayStamp(5500, 59, 60) + 1, CensusRules.DayStamp(5501, 0, 60));
        }
    }
}
