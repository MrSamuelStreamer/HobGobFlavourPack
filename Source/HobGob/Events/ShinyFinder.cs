using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>Finds shinies to steal and pits to hide them in (E4 Shiny Fever).</summary>
    public static class ShinyFinder
    {
        /// <summary>Spawned shiny items standing in the home area.</summary>
        public static IEnumerable<Thing> ShiniesInHome(Verse.Map map)
        {
            Area_Home home = map.areaManager.Home;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver))
            {
                if (thing.Spawned && home[thing.Position] && GoblinShinyDef.IsShiny(thing))
                {
                    yield return thing;
                }
            }
        }

        public static int CarryCountFor(Pawn pawn, Thing thing)
        {
            return EventRules.CarryCount(thing.stackCount, pawn.carryTracker.AvailableStackSpace(thing.def));
        }

        /// <summary>
        /// The most valuable shiny the pawn can carry, reserve and reach. Forbidden items count:
        /// Noblins ignore forbids. Noblin-proof doors still block the path.
        /// </summary>
        public static Thing BestTargetFor(Pawn pawn)
        {
            IEnumerable<Thing> ordered = ShiniesInHome(pawn.Map)
                .Select(t => (thing: t, value: t.MarketValue * CarryCountFor(pawn, t)))
                .Where(e => e.value > 0f)
                .OrderByDescending(e => e.value)
                .Select(e => e.thing);
            foreach (Thing thing in ordered)
            {
                if (pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Some))
                {
                    return thing;
                }
            }
            return null;
        }

        /// <summary>The nearest player pit the pawn can reach.</summary>
        public static CompGoblinPit NearestPitFor(Pawn pawn)
        {
            CompGoblinPit best = null;
            float bestDistance = float.MaxValue;
            foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(HobGobDefOf.MSSG_GoblinHole))
            {
                if (thing.Faction != Faction.OfPlayer)
                {
                    continue;
                }
                CompGoblinPit comp = thing.TryGetComp<CompGoblinPit>();
                float distance = thing.Position.DistanceToSquared(pawn.Position);
                if (comp != null && distance < bestDistance && pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
                {
                    best = comp;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
