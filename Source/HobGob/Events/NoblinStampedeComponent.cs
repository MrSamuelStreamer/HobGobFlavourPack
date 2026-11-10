using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>One door held open by a stampede, and what to restore when the hold ends.</summary>
    public class DoorHold : IExposable
    {
        public Building_Door door;
        public bool previous;
        public int expiry;

        public void ExposeData()
        {
            Scribe_References.Look(ref door, "door");
            Scribe_Values.Look(ref previous, "previous");
            Scribe_Values.Look(ref expiry, "expiry");
        }
    }

    /// <summary>
    /// E5 Noblin Stampede state for one map: the pack's shared run target, and doors the
    /// stampede has left held open.
    /// </summary>
    public class NoblinStampedeComponent : MapComponent
    {
        private const int CheckInterval = 250;

        private static readonly AccessTools.FieldRef<Building_Door, bool> HoldOpenRef =
            AccessTools.FieldRefAccess<Building_Door, bool>("holdOpenInt");

        private IntVec3 target = IntVec3.Invalid;
        private int legStartTick;
        private List<DoorHold> holds = new List<DoorHold>();

        public NoblinStampedeComponent(Verse.Map map) : base(map)
        {
        }

        public IReadOnlyList<DoorHold> Holds => holds;

        /// <summary>The pack's current target, rolling a new leg when it is reached or stale.</summary>
        public IntVec3 PackTargetFor(Pawn pawn)
        {
            int now = Find.TickManager.TicksGame;
            float distance = target.IsValid ? pawn.Position.DistanceTo(target) : float.MaxValue;
            if (EventRules.NeedsNewLeg(now, legStartTick, distance, target.IsValid))
            {
                target = FarCellFor(pawn);
                legStartTick = now;
            }
            return target;
        }

        /// <summary>A reachable cell far from the pawn, preferring the home area.</summary>
        public IntVec3 FarCellFor(Pawn pawn)
        {
            Area_Home home = map.areaManager.Home;
            IntVec3 from = pawn.Position;
            bool Far(IntVec3 c) => c.DistanceTo(from) >= EventRules.StampedeLegMinDistance
                && c.Standable(map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly);
            // Sample a bounded number of home cells: big home areas have thousands.
            foreach (IntVec3 cell in home.ActiveCells.InRandomOrder().Take(300))
            {
                if (Far(cell))
                {
                    return cell;
                }
            }
            if (CellFinder.TryFindRandomCellNear(from, map, EventRules.StampedeLegMinDistance * 2, Far, out IntVec3 found))
            {
                return found;
            }
            return RCellFinder.RandomWanderDestFor(pawn, from, 20f, (p, c, r) => true, Danger.Deadly);
        }

        /// <summary>A stampeding Noblin is about to pass this door: hold it open for a while.</summary>
        public void NoteDoorPassed(Building_Door door)
        {
            int now = Find.TickManager.TicksGame;
            DoorHold hold = holds.Find(h => h.door == door);
            if (hold == null)
            {
                hold = new DoorHold { door = door, previous = door.HoldOpen, expiry = now };
                holds.Add(hold);
            }
            hold.expiry = EventRules.HoldExpiry(hold.expiry, now);
            HoldOpenRef(door) = true;
        }

        public override void MapComponentTick()
        {
            if (holds.Count == 0 || !map.IsHashIntervalTick(CheckInterval))
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            for (int i = holds.Count - 1; i >= 0; i--)
            {
                DoorHold hold = holds[i];
                if (hold.door == null || hold.door.Destroyed)
                {
                    holds.RemoveAt(i);
                    continue;
                }
                if (!EventRules.HoldExpired(now, hold.expiry))
                {
                    continue;
                }
                // Only undo our own change: if the player switched hold-open off meanwhile, leave it.
                if (hold.door.HoldOpen)
                {
                    HoldOpenRef(hold.door) = hold.previous;
                }
                holds.RemoveAt(i);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref target, "target", IntVec3.Invalid);
            Scribe_Values.Look(ref legStartTick, "legStartTick");
            Scribe_Collections.Look(ref holds, "holds", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                holds ??= new List<DoorHold>();
                holds.RemoveAll(h => h == null || h.door == null);
            }
        }
    }
}
