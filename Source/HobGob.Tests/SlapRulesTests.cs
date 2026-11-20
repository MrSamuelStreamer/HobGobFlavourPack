using System;
using System.Linq;
using HobGob.Slap;
using Xunit;

namespace HobGob.Tests
{
    public class SlapRulesTests
    {
        private static PassionSlot[] Mixed() => new[]
        {
            new PassionSlot(SlapRules.Major),
            new PassionSlot(SlapRules.Minor),
            new PassionSlot(SlapRules.Minor),
            new PassionSlot(SlapRules.None),
            new PassionSlot(SlapRules.None),
            new PassionSlot(SlapRules.None),
        };

        private static int Levels(int[] passions) => passions.Sum();

        [Theory]
        [InlineData(-2, -1)]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        public void PassionStepsFor_MapsOutcome(int positivity, int expected)
        {
            Assert.Equal(expected, SlapRules.PassionStepsFor(positivity));
        }

        [Fact]
        public void Reroll_Meh_KeepsMinorAndMajorTotals()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                int[] result = SlapRules.Reroll(Mixed(), -1, new Random(seed));
                Assert.Equal(1, result.Count(p => p == SlapRules.Major));
                Assert.Equal(2, result.Count(p => p == SlapRules.Minor));
            }
        }

        [Fact]
        public void Reroll_Meh_ActuallyMovesPassionsSometimes()
        {
            int[] before = Mixed().Select(s => s.Passion).ToArray();
            bool moved = Enumerable.Range(0, 50).Any(seed => !SlapRules.Reroll(Mixed(), -1, new Random(seed)).SequenceEqual(before));
            Assert.True(moved);
        }

        [Theory]
        [InlineData(-2, 3)]
        [InlineData(1, 5)]
        [InlineData(2, 6)]
        public void Reroll_QualityChangesTotalLevels(int positivity, int expectedLevels)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.Equal(expectedLevels, Levels(SlapRules.Reroll(Mixed(), positivity, new Random(seed))));
            }
        }

        [Theory]
        [InlineData(-1, 3)]
        [InlineData(1, 4)]
        [InlineData(2, 5)]
        public void Reroll_GoodOutcomesAddBrandNewPassions(int positivity, int expectedPassionateSkills)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                int[] result = SlapRules.Reroll(Mixed(), positivity, new Random(seed));
                Assert.Equal(expectedPassionateSkills, result.Count(p => p > SlapRules.None));
            }
        }

        [Fact]
        public void Reroll_BonusUpgradesWhenNoSkillIsLeftWithoutPassion()
        {
            var slots = new[] { new PassionSlot(SlapRules.Minor), new PassionSlot(SlapRules.Minor) };
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.Equal(4, Levels(SlapRules.Reroll(slots, 2, new Random(seed))));
            }
        }

        [Fact]
        public void ShuffleLevels_KeepsTheSameLevelsAndLeavesDisabledSkillsAlone()
        {
            int[] levels = { 0, 3, 8, 12, 20, 5 };
            bool[] disabled = { false, true, false, false, false, true };
            bool moved = false;
            for (int seed = 0; seed < 50; seed++)
            {
                int[] result = SlapRules.ShuffleLevels(levels, disabled, new Random(seed));
                Assert.Equal(3, result[1]);
                Assert.Equal(5, result[5]);
                Assert.Equal(levels.OrderBy(x => x), result.OrderBy(x => x));
                moved |= !result.SequenceEqual(levels);
            }
            Assert.True(moved);
        }

        [Fact]
        public void Reroll_LockedSkillsAreUntouched()
        {
            var slots = new[]
            {
                new PassionSlot(SlapRules.Major, locked: true),
                new PassionSlot(SlapRules.None, locked: true),
                new PassionSlot(SlapRules.Minor),
                new PassionSlot(SlapRules.None),
            };
            for (int seed = 0; seed < 50; seed++)
            {
                int[] result = SlapRules.Reroll(slots, 2, new Random(seed));
                Assert.Equal(SlapRules.Major, result[0]);
                Assert.Equal(SlapRules.None, result[1]);
            }
        }

        [Fact]
        public void Reroll_BlockedSkillsNeverGetPassion()
        {
            var slots = new[]
            {
                new PassionSlot(SlapRules.Major),
                new PassionSlot(SlapRules.None, blocked: true),
                new PassionSlot(SlapRules.None, blocked: true),
                new PassionSlot(SlapRules.None),
            };
            for (int seed = 0; seed < 50; seed++)
            {
                int[] result = SlapRules.Reroll(slots, 2, new Random(seed));
                Assert.Equal(SlapRules.None, result[1]);
                Assert.Equal(SlapRules.None, result[2]);
            }
        }

        [Fact]
        public void Reroll_ForcedSkillsKeepAtLeastMinorEvenWhenTerrible()
        {
            var slots = new[]
            {
                new PassionSlot(SlapRules.Minor, forced: true),
                new PassionSlot(SlapRules.None),
                new PassionSlot(SlapRules.None),
            };
            for (int seed = 0; seed < 50; seed++)
            {
                int[] result = SlapRules.Reroll(slots, -2, new Random(seed));
                Assert.True(result[0] >= SlapRules.Minor);
            }
        }

        [Fact]
        public void Reroll_CapsAtMajorWhenEverythingIsMaxed()
        {
            var slots = new[] { new PassionSlot(SlapRules.Major), new PassionSlot(SlapRules.Major) };
            int[] result = SlapRules.Reroll(slots, 2, new Random(1));
            Assert.All(result, p => Assert.Equal(SlapRules.Major, p));
        }

        [Fact]
        public void Reroll_TerribleWithNoPassionsDoesNothing()
        {
            var slots = new[] { new PassionSlot(SlapRules.None), new PassionSlot(SlapRules.None) };
            Assert.All(SlapRules.Reroll(slots, -2, new Random(1)), p => Assert.Equal(SlapRules.None, p));
        }

        [Theory]
        [InlineData(0, false, SlapRules.GapBetweenTurnsTicks, true)]
        [InlineData(0, false, 1000000, true)]
        [InlineData(1, false, 1000000, false)]
        [InlineData(0, true, 1000000, false)]
        [InlineData(0, false, SlapRules.GapBetweenTurnsTicks - 1, false)]
        public void TurnsDone_WhenNobbyWaitingAndLastThumpSettled(int waiting, bool inProgress, int sinceLast, bool expected)
        {
            Assert.Equal(expected, SlapRules.TurnsDone(waiting, inProgress, sinceLast));
        }

        [Theory]
        [InlineData(0f, 30f, 30f, 3f, true)]
        [InlineData(0.5f, 30f, 30f, 1f, false)]
        [InlineData(0.49f, 30f, 30f, 1f, true)]
        [InlineData(0f, 16f, 30f, 1f, true)]
        [InlineData(0f, 16f, 30f, 2f, false)]
        [InlineData(0f, 10f, 0f, 1f, false)]
        public void SafeToThump_RespectsPainAndPartHealth(float pain, float hp, float max, float damage, bool expected)
        {
            Assert.Equal(expected, SlapRules.SafeToThump(pain, hp, max, damage));
        }
    }
}
