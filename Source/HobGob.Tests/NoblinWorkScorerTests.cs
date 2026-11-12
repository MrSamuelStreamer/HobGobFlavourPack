using HobGob;
using Xunit;

namespace HobGob.Tests
{
    public class NoblinWorkScorerTests
    {
        private static MapSignals Quiet() => new MapSignals(false, 0, 0, false, false);

        [Fact]
        public void QuietMap_NoUrgentTask_AllLowPriority()
        {
            MapSignals s = Quiet();
            Assert.Null(NoblinWorkScorer.UrgentTask(s));
            foreach (NoblinTask t in NoblinWorkScorer.AllTasks)
            {
                int expected = t == NoblinTask.Growing ? 3 : 4;
                Assert.Equal(expected, NoblinWorkScorer.Priority(t, s, null, false));
            }
        }

        [Fact]
        public void FoodLow_GrowingIsUrgent_EvenOverFullFilth()
        {
            MapSignals s = new MapSignals(false, 0, 1000, false, false, foodLow: true);
            Assert.Equal(NoblinTask.Growing, NoblinWorkScorer.UrgentTask(s));
            Assert.Equal(1, NoblinWorkScorer.Priority(NoblinTask.Growing, s, NoblinTask.Growing, true));
        }

        [Fact]
        public void EndlessFilth_RushesAtMostHalf()
        {
            MapSignals s = new MapSignals(false, 0, 1000, false, false);
            Assert.Equal(NoblinTask.Cleaning, NoblinWorkScorer.UrgentTask(s));
            Assert.Equal(4, NoblinWorkScorer.UrgentShare(8, NoblinWorkScorer.Demand(NoblinTask.Cleaning, s)));
        }

        [Fact]
        public void Fire_EveryNoblinFirefightsFirst()
        {
            MapSignals s = new MapSignals(true, 0, 0, false, false);
            Assert.Equal(1, NoblinWorkScorer.Priority(NoblinTask.Firefighter, s, null, false));
        }

        [Fact]
        public void Fire_IsNeverTheUrgentTask()
        {
            MapSignals s = new MapSignals(true, 0, 0, false, false);
            Assert.Null(NoblinWorkScorer.UrgentTask(s));
        }

        [Fact]
        public void LotsOfHaulables_HaulingIsUrgent()
        {
            MapSignals s = new MapSignals(false, 500, 10, true, false);
            Assert.Equal(NoblinTask.Hauling, NoblinWorkScorer.UrgentTask(s));
        }

        [Fact]
        public void UrgentTask_TiesGoToEarlierTask()
        {
            MapSignals s = new MapSignals(false, 0, 0, true, true);
            Assert.Equal(NoblinTask.PlantCutting, NoblinWorkScorer.UrgentTask(s));
        }

        [Fact]
        public void UrgentShare_GetsPriorityOne_OthersDoNot()
        {
            MapSignals s = new MapSignals(false, 60, 0, false, false);
            Assert.Equal(1, NoblinWorkScorer.Priority(NoblinTask.Hauling, s, NoblinTask.Hauling, true));
            Assert.Equal(2, NoblinWorkScorer.Priority(NoblinTask.Hauling, s, NoblinTask.Hauling, false));
        }

        [Theory]
        [InlineData(0, 1f, 0)]
        [InlineData(1, 0.1f, 1)]
        [InlineData(10, 0.5f, 5)]
        [InlineData(10, 0.51f, 5)]
        [InlineData(3, 2f, 2)]
        [InlineData(8, 1f, 4)]
        [InlineData(5, -1f, 1)]
        public void UrgentShare_RoundsUpAndClamps(int count, float demand, int expected)
        {
            Assert.Equal(expected, NoblinWorkScorer.UrgentShare(count, demand));
        }

        [Theory]
        [InlineData(0, 4)]
        [InlineData(20, 3)]
        [InlineData(40, 2)]
        [InlineData(1000, 2)]
        public void HaulingPriority_ScalesWithHaulables(int haulables, int expected)
        {
            MapSignals s = new MapSignals(false, haulables, 0, false, false);
            Assert.Equal(expected, NoblinWorkScorer.Priority(NoblinTask.Hauling, s, null, false));
        }

        [Fact]
        public void ManagedTasks_AreNeverDisabled()
        {
            MapSignals s = new MapSignals(true, 1000, 1000, true, true);
            foreach (NoblinTask t in NoblinWorkScorer.AllTasks)
            {
                int p = NoblinWorkScorer.Priority(t, s, NoblinWorkScorer.UrgentTask(s), true);
                Assert.InRange(p, 1, 4);
            }
        }
    }

    public class EvolutionRulesTests
    {
        [Theory]
        [InlineData(24, 25, 0, 0, false)]
        [InlineData(25, 25, 0, 0, true)]
        [InlineData(100, 75, 5, 0, true)]
        [InlineData(75, 75, 0, 1, true)]
        [InlineData(75, 75, 1, 1, false)]
        [InlineData(75, 75, 2, 1, false)]
        public void ShouldEvolve(int kills, int required, int onMap, int cap, bool expected)
        {
            Assert.Equal(expected, EvolutionRules.ShouldEvolve(kills, required, onMap, cap));
        }

        [Theory]
        [InlineData(0f, 0f, false)]
        [InlineData(0.999f, 0f, false)]
        [InlineData(0f, 1f, true)]
        [InlineData(0.999f, 1f, true)]
        [InlineData(0.049f, 0.05f, true)]
        [InlineData(0.05f, 0.05f, false)]
        [InlineData(0.5f, 0.05f, false)]
        public void RollsRare(float roll, float chance, bool expected)
        {
            Assert.Equal(expected, EvolutionRules.RollsRare(roll, chance));
        }

        [Theory]
        [InlineData("Hauling", NoblinTask.Hauling)]
        [InlineData("PlantCutting", NoblinTask.PlantCutting)]
        [InlineData("BasicWorker", NoblinTask.BasicWorker)]
        [InlineData("Firefighter", NoblinTask.Firefighter)]
        [InlineData("Growing", NoblinTask.Growing)]
        public void VanillaWorkType_MatchesByName(string defName, NoblinTask expected)
        {
            Assert.Equal(expected, NoblinWorkScorer.TaskForWorkType(defName, false, false, false, false));
        }

        [Fact]
        public void ComplexJobsHaulingSplit_MatchesByTag()
        {
            // FSFDeliver, FSFRearming, FSFTransport, FSFCremating: ManualDumb + Hauling.
            Assert.Equal(NoblinTask.Hauling, NoblinWorkScorer.TaskForWorkType("FSFDeliver", false, false, true, false));
        }

        [Fact]
        public void ComplexJobsDrilling_MatchesMiningByTag()
        {
            Assert.Equal(NoblinTask.Mining, NoblinWorkScorer.TaskForWorkType("FSFDrilling", false, false, false, true));
        }

        [Fact]
        public void ComplexJobsHarvesting_MatchesGrowingByTag()
        {
            // FSFHarvesting carries PlantWork: crop harvesting moved out of Growing.
            Assert.Equal(NoblinTask.Growing, NoblinWorkScorer.TaskForWorkType("FSFHarvesting", false, false, false, false, plantWork: true));
        }

        [Fact]
        public void SkilledModdedWorkType_StaysOff()
        {
            Assert.Null(NoblinWorkScorer.TaskForWorkType("Cooking", false, false, false, false));
        }
    }
}
