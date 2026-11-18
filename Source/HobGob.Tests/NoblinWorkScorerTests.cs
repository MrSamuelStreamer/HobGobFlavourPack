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

        [Theory]
        [InlineData(NoblinTask.Mining)]
        [InlineData(NoblinTask.PlantCutting)]
        public void DesignatedWork_IsPriorityTwo_EvenWhenCleaningIsUrgent(NoblinTask task)
        {
            MapSignals s = Designated(task, filth: 100);
            NoblinTask? urgent = NoblinWorkScorer.UrgentTask(s);
            Assert.Equal(NoblinTask.Cleaning, urgent);
            Assert.Equal(2, NoblinWorkScorer.Priority(task, s, urgent, false));
            Assert.Equal(2, NoblinWorkScorer.Priority(NoblinTask.Cleaning, s, urgent, false));
        }

        [Theory]
        [InlineData(NoblinTask.Mining)]
        [InlineData(NoblinTask.PlantCutting)]
        public void DesignatedWork_BeatsGrowing(NoblinTask task)
        {
            MapSignals s = Designated(task);
            NoblinTask? urgent = NoblinWorkScorer.UrgentTask(s);
            Assert.True(NoblinWorkScorer.Priority(task, s, urgent, false)
                < NoblinWorkScorer.Priority(NoblinTask.Growing, s, urgent, false));
        }

        [Theory]
        [InlineData(NoblinTask.Mining)]
        [InlineData(NoblinTask.PlantCutting)]
        public void DesignatedWork_IsUrgent_WhenNothingElseIs(NoblinTask task)
        {
            Assert.Equal(task, NoblinWorkScorer.UrgentTask(Designated(task)));
        }

        [Theory]
        [InlineData(NoblinTask.Mining)]
        [InlineData(NoblinTask.PlantCutting)]
        public void NothingDesignated_IsPriorityFour(NoblinTask task)
        {
            MapSignals s = new MapSignals(false, 0, 0, false, false);
            Assert.Equal(4, NoblinWorkScorer.Priority(task, s, null, false));
        }

        private static MapSignals Designated(NoblinTask task, int filth = 0) =>
            new MapSignals(false, 0, filth, task == NoblinTask.Mining, task == NoblinTask.PlantCutting);

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
        [InlineData(true, false, false, true)]   // gene the xenotype lists
        [InlineData(false, false, false, false)] // stale gene from an older xenotype version
        [InlineData(false, false, true, false)]
        [InlineData(false, true, false, true)]   // natural skin colour, xenotype brings none
        [InlineData(false, true, true, false)]   // natural skin colour, xenotype brings its own
        [InlineData(true, true, true, true)]
        public void KeepsEndogene(bool inXenotype, bool isColour, bool xenoHasColour, bool expected)
        {
            Assert.Equal(expected, EvolutionRules.KeepsEndogene(inXenotype, isColour, xenoHasColour));
        }

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
        [InlineData(true, true, true, false, true)]    // psylinked noblin with the kills
        [InlineData(true, true, false, false, false)]  // psylinked noblin without the kills (ritual mass grant)
        [InlineData(true, false, true, false, false)]  // kills but no psylink: normal evolution
        [InlineData(false, true, true, false, false)]  // not a noblin
        [InlineData(true, true, true, true, false)]    // already queued
        [InlineData(false, false, false, false, false)]
        public void PsylinkMakesSnoblin_OnlyUnqueuedPsylinkedNoblinsWithKills(bool noblin, bool psylink, bool kills, bool queued, bool expected)
        {
            Assert.Equal(expected, EvolutionRules.PsylinkMakesSnoblin(noblin, psylink, kills, queued));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(40, 3)]
        [InlineData(-1, 0)]
        public void RitualPsylinkCount_CapsAtMax(int eligible, int expected)
        {
            Assert.Equal(expected, EvolutionRules.RitualPsylinkCount(eligible));
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(false, false, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void EvolvesAutomatically_OnlyManualPlayerPawnsWait(bool manual, bool player, bool expected)
        {
            Assert.Equal(expected, EvolutionRules.EvolvesAutomatically(manual, player));
        }

        [Theory]
        [InlineData(false, 51, 50, false)]
        [InlineData(true, 49, 50, false)]
        [InlineData(true, 50, 50, false)]
        [InlineData(true, 51, 50, true)]
        [InlineData(true, 100, 50, true)]
        public void EligibleForKillSurge_OnlyHobgoblinsPastTheLine(bool hobgoblin, int kills, int required, bool expected)
        {
            Assert.Equal(expected, EvolutionRules.EligibleForKillSurge(hobgoblin, kills, required));
        }

        [Theory]
        [InlineData(0.2f, 0f, true)]
        [InlineData(0.2f, 0.6f, false)]
        [InlineData(0.6f, 0.6f, true)]
        [InlineData(0.9f, 1f, false)]
        [InlineData(1f, 1f, true)]
        [InlineData(2.5f, 1f, true)]
        public void KillCounts_OnlyVictimsAtOrAboveTheMinimumSize(float victim, float min, bool expected)
        {
            Assert.Equal(expected, EvolutionRules.KillCounts(victim, min));
        }

        [Theory]
        [InlineData(0f, KillSurge.Queen)]
        [InlineData(0.009f, KillSurge.Queen)]
        [InlineData(0.01f, KillSurge.Explode)]
        [InlineData(0.019f, KillSurge.Explode)]
        [InlineData(0.02f, KillSurge.None)]
        [InlineData(0.5f, KillSurge.None)]
        public void RollKillSurge_QueenThenExplode(float roll, KillSurge expected)
        {
            Assert.Equal(expected, EvolutionRules.RollKillSurge(roll, EvolutionRules.KillQueenChance, EvolutionRules.KillExplodeChance));
        }

        [Theory]
        [InlineData(-1, 0, false)]
        [InlineData(-1, 3, false)]
        [InlineData(0, 0, false)]
        [InlineData(0, 1, true)]
        [InlineData(3, 0, true)]
        public void SeasonChanged_IgnoresFirstReading(int last, int now, bool expected)
        {
            Assert.Equal(expected, EvolutionRules.SeasonChanged(last, now));
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
