using HobGob.Comps;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>
    /// Grubs in me 'ole: while active, every player pit on the map spawns a noblin every
    /// <see cref="EventRules.GrubSpawnIntervalTicks"/>. Started by <see cref="IncidentWorker_GrubInfestation"/>.
    /// </summary>
    public class GrubInfestationComponent : MapComponent
    {
        private const int CheckInterval = 250;

        private int endTick = -1;

        public GrubInfestationComponent(Map map) : base(map)
        {
        }

        public bool Active => endTick >= 0;

        public static GrubInfestationComponent For(Map map) => map?.GetComponent<GrubInfestationComponent>();

        public void Start(int durationTicks)
        {
            endTick = Find.TickManager.TicksGame + durationTicks;
            ResetPits();
        }

        public override void MapComponentTick()
        {
            if (Active && Find.TickManager.TicksGame % CheckInterval == 0 && Find.TickManager.TicksGame >= endTick)
            {
                endTick = -1;
                // Drop the short grub timers so no extra noblin turns up after the end.
                ResetPits();
                Messages.Message("MSSG_GrubsOver".Translate(), MessageTypeDefOf.NeutralEvent);
            }
        }

        /// <summary>Re-rolls every player pit's timer. The interval depends on <see cref="Active"/>.</summary>
        private void ResetPits()
        {
            foreach (Thing thing in map.listerThings.ThingsOfDef(HobGobDefOf.MSSG_GoblinHole))
            {
                if (thing.Faction == Faction.OfPlayer)
                {
                    thing.TryGetComp<CompSpawnerNoblin>()?.ResetSpawnTimer();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref endTick, "endTick", -1);
        }
    }
}
