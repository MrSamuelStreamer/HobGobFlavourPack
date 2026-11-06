using System;

namespace HobGob
{
    /// <summary>The dumb-labour work types a Noblin manages for itself.</summary>
    public enum NoblinTask
    {
        Firefighter,
        Hauling,
        Cleaning,
        PlantCutting,
        Mining,
        BasicWorker,
    }

    /// <summary>Snapshot of what the map needs, taken once per pass.</summary>
    public readonly struct MapSignals
    {
        public readonly bool FireInHomeArea;
        public readonly int Haulables;
        public readonly int FilthInHomeArea;
        public readonly bool MiningDesignated;
        public readonly bool CuttingDesignated;

        public MapSignals(bool fireInHomeArea, int haulables, int filthInHomeArea, bool miningDesignated, bool cuttingDesignated)
        {
            FireInHomeArea = fireInHomeArea;
            Haulables = haulables;
            FilthInHomeArea = filthInHomeArea;
            MiningDesignated = miningDesignated;
            CuttingDesignated = cuttingDesignated;
        }
    }

    /// <summary>
    /// Pure scoring rules. No game types, so it is unit-testable.
    /// Priorities follow RimWorld: 1 = highest, 4 = lowest. Managed tasks are never 0.
    /// </summary>
    public static class NoblinWorkScorer
    {
        public const int HaulablesForFullDemand = 60;
        public const int FilthForFullDemand = 100;
        public const float DesignatedDemand = 0.5f;
        public const float BasicWorkerDemand = 0.2f;
        public const float UrgentDemandThreshold = 0.5f;

        public static readonly NoblinTask[] AllTasks = (NoblinTask[])Enum.GetValues(typeof(NoblinTask));

        /// <summary>How much the map needs a task, from 0 (none) to 1 (badly).</summary>
        public static float Demand(NoblinTask task, MapSignals s)
        {
            switch (task)
            {
                case NoblinTask.Firefighter: return s.FireInHomeArea ? 1f : 0f;
                case NoblinTask.Hauling: return Ratio(s.Haulables, HaulablesForFullDemand);
                case NoblinTask.Cleaning: return Ratio(s.FilthInHomeArea, FilthForFullDemand);
                case NoblinTask.Mining: return s.MiningDesignated ? DesignatedDemand : 0f;
                case NoblinTask.PlantCutting: return s.CuttingDesignated ? DesignatedDemand : 0f;
                case NoblinTask.BasicWorker: return BasicWorkerDemand;
                default: return 0f;
            }
        }

        /// <summary>The non-fire task with the highest demand, if it is urgent enough. Ties go to the earlier task.</summary>
        public static NoblinTask? UrgentTask(MapSignals s)
        {
            NoblinTask? best = null;
            float bestDemand = UrgentDemandThreshold;
            foreach (NoblinTask task in AllTasks)
            {
                if (task == NoblinTask.Firefighter)
                {
                    continue;
                }
                float d = Demand(task, s);
                if (d >= bestDemand && (best == null || d > bestDemand))
                {
                    best = task;
                    bestDemand = d;
                }
            }
            return best;
        }

        /// <summary>How many of <paramref name="noblinCount"/> Noblins rush the urgent task. At least one when any exist.</summary>
        public static int UrgentShare(int noblinCount, float demand)
        {
            if (noblinCount <= 0)
            {
                return 0;
            }
            int share = (int)Math.Ceiling(noblinCount * Clamp01(demand));
            return Math.Max(1, Math.Min(noblinCount, share));
        }

        /// <summary>Priority (1-4) for one Noblin. <paramref name="inUrgentShare"/> marks it as one of the rushers.</summary>
        public static int Priority(NoblinTask task, MapSignals s, NoblinTask? urgent, bool inUrgentShare)
        {
            if (task == NoblinTask.Firefighter && s.FireInHomeArea)
            {
                return 1;
            }
            if (urgent == task && inUrgentShare)
            {
                return 1;
            }
            float d = Demand(task, s);
            if (d >= 0.66f)
            {
                return 2;
            }
            return d >= 0.33f ? 3 : 4;
        }

        private static float Ratio(int value, int full) => Clamp01(value / (float)full);

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>Pure evolution rules.</summary>
    public static class EvolutionRules
    {
        /// <param name="maxPerMap">0 or less means no cap.</param>
        public static bool ShouldEvolve(int kills, int killsRequired, int alreadyOnMap, int maxPerMap)
        {
            if (kills < killsRequired)
            {
                return false;
            }
            return maxPerMap <= 0 || alreadyOnMap < maxPerMap;
        }
    }
}
