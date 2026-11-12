using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HobGob.Comps.Map
{
    /// <summary>
    /// At the start of each season on this map, every goblin that can still evolve rolls for one free kill.
    /// Keeps evolution moving in colonies that rarely fight.
    /// </summary>
    public class GoblinSeasonComponent : MapComponent
    {
        private int lastQuadrum = -1;

        public GoblinSeasonComponent(Verse.Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!map.IsHashIntervalTick(GenDate.TicksPerHour))
            {
                return;
            }
            // Quadrum, not Season: polar maps have permanent seasons that never change.
            int quadrum = (int)GenDate.Quadrum(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile).x);
            bool changed = EvolutionRules.SeasonChanged(lastQuadrum, quadrum);
            lastQuadrum = quadrum;
            if (changed)
            {
                GrantSeasonalKills();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastQuadrum, "lastQuadrum", -1);
        }

        private void GrantSeasonalKills()
        {
            HobGobSettings settings = HobGobMod.Settings;
            List<Pawn> winners = new List<Pawn>();
            List<string> lines = new List<string>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || pawn.records == null)
                {
                    continue;
                }
                Gene_GoblinEvolution tracker = Hediff_EvolutionProgress.FindTracker(pawn);
                if (tracker == null || !tracker.GetsSeasonalKills || !Rand.Chance(settings.seasonalKillChance))
                {
                    continue;
                }
                int kills = settings.seasonalKills.RandomInRange;
                pawn.records.AddTo(RecordDefOf.Kills, kills);
                if (pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    winners.Add(pawn);
                    lines.Add("MSSG_SeasonalKillsEntry".Translate(pawn.LabelShort, kills));
                }
            }
            if (winners.Count > 0)
            {
                Messages.Message("MSSG_SeasonalKills".Translate(lines.ToCommaList()),
                    new LookTargets(winners), MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
