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
    /// During Shiny Fever, Noblins hide stolen shinies in its stash.
    /// </summary>
    public class CompGoblinPit : ThingComp, IThingHolder
    {
        private readonly List<Thing> candidates = new List<Thing>();
        private ThingOwner<Thing> stash;

        public CompGoblinPit()
        {
            stash = new ThingOwner<Thing>(this);
        }

        public ThingOwner Stash => stash;

        public ThingOwner GetDirectlyHeldThings() => stash;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        /// <summary>Moves a carried thing into the stash. Returns false if it could not be moved.</summary>
        public bool Deposit(Pawn carrier, Thing carried)
        {
            return carrier.carryTracker.innerContainer.TryTransferToContainer(carried, stash, carried.stackCount) > 0;
        }

        /// <summary>Drops everything in the stash on the ground beside the pit.</summary>
        public void DropStash(Verse.Map map)
        {
            if (stash.Count > 0 && map != null)
            {
                stash.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
            }
        }

        public override void PostDeSpawn(Verse.Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            // Covers deconstruct, destroy and dev removal: the stash never vanishes with the pit.
            DropStash(map);
            base.PostDeSpawn(map, mode);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref stash, "stash", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && stash == null)
            {
                stash = new ThingOwner<Thing>(this);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (parent.Faction != Faction.OfPlayer || stash.Count == 0 || !parent.Spawned)
            {
                yield break;
            }
            DesignationManager designations = parent.Map.designationManager;
            bool marked = designations.DesignationOn(parent, HobGobDefOf.MSSG_RummageStash) != null;
            yield return new Command_Action
            {
                defaultLabel = (marked ? "MSSG_RummageCancel" : "MSSG_Rummage").Translate(),
                defaultDesc = "MSSG_RummageDesc".Translate(),
                icon = GoblinPitTextures.Rummage,
                action = () =>
                {
                    Designation existing = designations.DesignationOn(parent, HobGobDefOf.MSSG_RummageStash);
                    if (existing != null)
                    {
                        designations.RemoveDesignation(existing);
                    }
                    else
                    {
                        designations.AddDesignation(new Designation(parent, HobGobDefOf.MSSG_RummageStash));
                    }
                }
            };
        }

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
            List<string> lines = new List<string>();
            NoblinStrikeComponent strike = parent.Spawned ? parent.Map.GetComponent<NoblinStrikeComponent>() : null;
            if (strike != null && strike.Active)
            {
                lines.Add("MSSG_TributeInspect".Translate(
                    (int)System.Math.Floor(strike.TributePaid),
                    (int)System.Math.Ceiling(strike.TributeOwed)).ToString());
            }
            if (stash.Count > 0)
            {
                float value = 0f;
                foreach (Thing thing in stash)
                {
                    value += thing.MarketValue * thing.stackCount;
                }
                lines.Add("MSSG_StashInspect".Translate(stash.Count, (int)value).ToString());
            }
            return lines.Count > 0 ? string.Join("\n", lines) : null;
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
