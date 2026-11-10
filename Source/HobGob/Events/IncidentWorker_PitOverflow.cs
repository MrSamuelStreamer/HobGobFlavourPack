using System.Collections.Generic;
using System.Linq;
using HobGob.Comps;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>
    /// E1 Pit Overflow: several hungry Noblins crawl out of the player's pits at once.
    /// </summary>
    public class IncidentWorker_PitOverflow : IncidentWorker
    {
        private static readonly FloatRange StartingFood = new FloatRange(0.1f, 0.25f);

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            return PlayerPits(map).Any(pit => pit.FreeSpawnCells().Any());
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            List<CompSpawnerNoblin> pits = PlayerPits(map).ToList();
            if (pits.Count == 0)
            {
                return false;
            }

            // Adjacent pits share ring cells, so keep the first pit that offers each cell.
            Dictionary<IntVec3, CompSpawnerNoblin> cellOwner = new Dictionary<IntVec3, CompSpawnerNoblin>();
            foreach (CompSpawnerNoblin pit in pits)
            {
                foreach (IntVec3 cell in pit.FreeSpawnCells())
                {
                    cellOwner.TryAdd(cell, pit);
                }
            }

            int roll = Rand.RangeInclusive(EventRules.OverflowMin, EventRules.OverflowMax);
            int count = EventRules.OverflowCount(roll, pits.Count, cellOwner.Count);
            List<Pawn> spawned = new List<Pawn>();
            foreach (KeyValuePair<IntVec3, CompSpawnerNoblin> entry in cellOwner.InRandomOrder().Take(count))
            {
                Pawn pawn = entry.Value.SpawnNoblinAt(entry.Key, announce: false);
                if (pawn != null)
                {
                    MakePitFresh(pawn);
                    spawned.Add(pawn);
                }
            }
            if (spawned.Count == 0)
            {
                return false;
            }

            // The overflow replaces the next regular spawn.
            foreach (CompSpawnerNoblin pit in pits)
            {
                pit.ResetSpawnTimer();
            }

            SendStandardLetter(
                def.letterLabel,
                def.letterText.Formatted(spawned.Count.Named("COUNT")),
                LetterDefOf.PositiveEvent,
                parms,
                new LookTargets(spawned));
            return true;
        }

        private static void MakePitFresh(Pawn pawn)
        {
            pawn.health.AddHediff(HobGobDefOf.MSSG_PitFresh);
            if (pawn.needs?.food != null)
            {
                pawn.needs.food.CurLevelPercentage = StartingFood.RandomInRange;
            }
        }

        private static IEnumerable<CompSpawnerNoblin> PlayerPits(Verse.Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsOfDef(HobGobDefOf.MSSG_GoblinHole))
            {
                if (thing.Faction != Faction.OfPlayer)
                {
                    continue;
                }
                CompSpawnerNoblin comp = thing.TryGetComp<CompSpawnerNoblin>();
                if (comp != null)
                {
                    yield return comp;
                }
            }
        }
    }
}
