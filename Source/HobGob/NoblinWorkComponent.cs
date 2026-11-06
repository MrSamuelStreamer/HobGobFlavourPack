using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob
{
    /// <summary>Every few seconds, sets each player Noblin's work priorities from what the map needs.</summary>
    public class NoblinWorkComponent : MapComponent
    {
        private const int Interval = 250;

        private static Dictionary<NoblinTask, WorkTypeDef> workTypes;
        private readonly List<Pawn> noblins = new List<Pawn>();

        public NoblinWorkComponent(Map map) : base(map)
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
            for (int i = 0; i < noblins.Count; i++)
            {
                Apply(noblins[i], signals, urgent, i < share);
            }
        }

        private void CollectNoblins()
        {
            noblins.Clear();
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                bool isNoblin = pawn.workSettings != null && pawn.workSettings.EverWork && NoblinRegistry.IsNoblin(pawn);
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
                    || map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.HarvestPlant));
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

        private static void Apply(Pawn pawn, MapSignals signals, NoblinTask? urgent, bool inUrgentShare)
        {
            foreach (KeyValuePair<NoblinTask, WorkTypeDef> pair in WorkTypes)
            {
                WorkTypeDef def = pair.Value;
                if (pawn.WorkTypeIsDisabled(def))
                {
                    continue;
                }
                int priority = NoblinWorkScorer.Priority(pair.Key, signals, urgent, inUrgentShare);
                // The pawn is registered, so GetPriority returns the stored value. SetPriority dirties the work giver cache: write only on change.
                if (pawn.workSettings.GetPriority(def) != priority)
                {
                    pawn.workSettings.SetPriority(def, priority);
                }
            }
        }

        private static Dictionary<NoblinTask, WorkTypeDef> WorkTypes
        {
            get
            {
                if (workTypes == null)
                {
                    workTypes = new Dictionary<NoblinTask, WorkTypeDef>();
                    foreach (NoblinTask task in NoblinWorkScorer.AllTasks)
                    {
                        WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(task.ToString());
                        if (def != null)
                        {
                            workTypes[task] = def;
                        }
                    }
                }
                return workTypes;
            }
        }
    }
}
