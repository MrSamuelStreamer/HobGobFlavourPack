using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Comps.Map
{
    /// <summary>Every few seconds, sets each player Noblin's work priorities from what the map needs.</summary>
    public class NoblinWorkComponent : MapComponent
    {
        private const int Interval = 250;
        /// <summary>Below this many days of stored food, Noblins rush the fields.</summary>
        private const float FoodLowDays = 5f;
        /// <summary>Rough daily nutrition one colonist eats.</summary>
        private const float NutritionPerColonistPerDay = 1.6f;

        private static Dictionary<WorkTypeDef, NoblinTask> taskFor;
        private readonly List<Pawn> noblins = new List<Pawn>();

        public NoblinWorkComponent(Verse.Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (map.IsHashIntervalTick(Interval))
            {
                UpdatePriorities();
            }
        }

        private void UpdatePriorities()
        {
            CollectNoblins();
            if (noblins.Count == 0)
            {
                return;
            }
            MapSignals signals = ReadSignals();
            NoblinTask? urgent = NoblinWorkScorer.UrgentTask(signals);
            int share = urgent.HasValue
                ? NoblinWorkScorer.UrgentShare(noblins.Count, NoblinWorkScorer.Demand(urgent.Value, signals))
                : 0;
            bool striking = map.GetComponent<Events.NoblinStrikeComponent>()?.Active ?? false;
            for (int i = 0; i < noblins.Count; i++)
            {
                Apply(noblins[i], signals, urgent, i < share, striking);
            }
        }

        private void CollectNoblins()
        {
            noblins.Clear();
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                bool isNoblin = NoblinRegistry.ShouldManage(pawn);
                NoblinRegistry.Set(pawn, isNoblin);
                if (isNoblin)
                {
                    noblins.Add(pawn);
                }
            }
            // Stable order, so the same Noblins keep rushing the urgent task between passes.
            noblins.SortBy(p => p.thingIDNumber);
        }

        private MapSignals ReadSignals()
        {
            return new MapSignals(
                fireInHomeArea: AnyFireInHomeArea(),
                haulables: map.listerHaulables.ThingsPotentiallyNeedingHauling().Count,
                filthInHomeArea: map.listerFilthInHomeArea.FilthInHomeArea.Count,
                miningDesignated: map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Mine),
                cuttingDesignated: map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.CutPlant)
                    || map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.HarvestPlant),
                foodLow: FoodLow());
        }

        private bool FoodLow()
        {
            int colonists = map.mapPawns.FreeColonistsSpawnedCount;
            return colonists > 0
                && map.resourceCounter.TotalHumanEdibleNutrition < colonists * NutritionPerColonistPerDay * FoodLowDays;
        }

        private bool AnyFireInHomeArea()
        {
            if (map.fireWatcher.FireDanger <= 0f)
            {
                return false;
            }
            Area_Home home = map.areaManager.Home;
            foreach (Thing fire in map.listerThings.ThingsOfDef(ThingDefOf.Fire))
            {
                if (home[fire.Position])
                {
                    return true;
                }
            }
            return false;
        }

        private static void Apply(Pawn pawn, MapSignals signals, NoblinTask? urgent, bool inUrgentShare, bool striking)
        {
            List<WorkTypeDef> all = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                WorkTypeDef def = all[i];
                if (pawn.WorkTypeIsDisabled(def) || def.defName == "Patient" || def.defName == "PatientBedRest")
                {
                    continue;
                }
                // Anything outside the Noblin task list (skilled work) stays off. On strike, everything is off.
                int priority = !striking && TaskFor.TryGetValue(def, out NoblinTask task)
                    ? NoblinWorkScorer.Priority(task, signals, urgent, inUrgentShare)
                    : 0;
                // The pawn is registered, so GetPriority returns the stored value. SetPriority dirties the work giver cache: write only on change.
                if (pawn.workSettings.GetPriority(def) != priority)
                {
                    pawn.workSettings.SetPriority(def, priority);
                }
            }
        }

        private static Dictionary<WorkTypeDef, NoblinTask> TaskFor
        {
            get
            {
                if (taskFor == null)
                {
                    taskFor = new Dictionary<WorkTypeDef, NoblinTask>();
                    foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    {
                        NoblinTask? task = NoblinWorkScorer.TaskForWorkType(
                            def.defName,
                            firefighting: (def.workTags & WorkTags.Firefighting) != 0,
                            cleaning: (def.workTags & WorkTags.Cleaning) != 0,
                            hauling: (def.workTags & WorkTags.Hauling) != 0,
                            mining: (def.workTags & WorkTags.Mining) != 0,
                            plantWork: (def.workTags & WorkTags.PlantWork) != 0);
                        if (task.HasValue)
                        {
                            taskFor[def] = task.Value;
                        }
                    }
                }
                return taskFor;
            }
        }
    }
}
