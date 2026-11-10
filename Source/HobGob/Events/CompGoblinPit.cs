using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    public class CompProperties_GoblinPit : CompProperties
    {
        public CompProperties_GoblinPit()
        {
            compClass = typeof(CompGoblinPit);
        }
    }

    /// <summary>
    /// Goblin event state on a spawning pit. During a Noblin Strike it eats shinies left beside it as tribute.
    /// </summary>
    public class CompGoblinPit : ThingComp
    {
        private readonly List<Thing> candidates = new List<Thing>();

        public override void CompTickRare()
        {
            if (!parent.Spawned || parent.Faction != Faction.OfPlayer)
            {
                return;
            }
            NoblinStrikeComponent strike = parent.Map.GetComponent<NoblinStrikeComponent>();
            if (strike != null && strike.Active)
            {
                EatTribute(strike);
            }
        }

        public override string CompInspectStringExtra()
        {
            NoblinStrikeComponent strike = parent.Spawned ? parent.Map.GetComponent<NoblinStrikeComponent>() : null;
            if (strike == null || !strike.Active)
            {
                return null;
            }
            return "MSSG_TributeInspect".Translate(
                (int)System.Math.Floor(strike.TributePaid),
                (int)System.Math.Ceiling(strike.TributeOwed)).ToString();
        }

        private void EatTribute(NoblinStrikeComponent strike)
        {
            CollectCandidates();
            // Destroying while iterating a cell's thing list would throw, so work from the snapshot.
            foreach (Thing thing in candidates)
            {
                if (!strike.Active)
                {
                    break;
                }
                if (thing.Destroyed)
                {
                    continue;
                }
                float unitValue = thing.MarketValue;
                int units = EventRules.UnitsToTake(unitValue, thing.stackCount, strike.TributeRemaining);
                if (units <= 0)
                {
                    continue;
                }
                Thing taken = units >= thing.stackCount ? thing : thing.SplitOff(units);
                taken.Destroy();
                strike.Pay(units * unitValue);
            }
            candidates.Clear();
        }

        private void CollectCandidates()
        {
            candidates.Clear();
            Verse.Map map = parent.Map;
            CellRect pit = parent.OccupiedRect();
            foreach (IntVec3 cell in pit.ExpandedBy(EventRules.TributeRadius))
            {
                if (!cell.InBounds(map) || pit.Contains(cell))
                {
                    continue;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing.def.category == ThingCategory.Item && !thing.IsForbidden(Faction.OfPlayer)
                        && GoblinShinyDef.IsShiny(thing))
                    {
                        candidates.Add(thing);
                    }
                }
            }
        }
    }
}
