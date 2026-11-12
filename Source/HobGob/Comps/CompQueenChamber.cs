using System.Collections.Generic;
using HobGob.Events;
using RimWorld;
using Verse;

namespace HobGob.Comps
{
    public class CompProperties_QueenChamber : CompProperties
    {
        public CompProperties_QueenChamber()
        {
            compClass = typeof(CompQueenChamber);
        }
    }

    /// <summary>
    /// Holds the queen who dug the spawning pit (issue #9). She is not on the map and never ticks while inside.
    /// When the pit goes (deconstructed, destroyed), she climbs out in torpor. Pits from older saves have no stored
    /// queen, so a fresh one climbs out instead.
    /// </summary>
    public class CompQueenChamber : ThingComp, IThingHolder
    {
        private ThingOwner<Pawn> chamber;

        /// <summary>Set when the pit is dug and cleared once the caster is stored, so a pit lost in between makes no second queen.</summary>
        private bool expectingQueen;

        public CompQueenChamber()
        {
            chamber = new ThingOwner<Pawn>(this);
        }

        public Pawn Queen => chamber.Count > 0 ? chamber[0] : null;

        public ThingOwner GetDirectlyHeldThings() => chamber;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        /// <summary>A pawn held inside a spawning pit. Hidden from the colonist bar and pawn tables.</summary>
        public static bool IsStoredQueen(Pawn pawn) => pawn?.ParentHolder is CompQueenChamber;

        public void ExpectQueen() => expectingQueen = true;

        /// <summary>Takes the queen off the map and into the pit. Returns false if she could not be stored.</summary>
        public bool Store(Pawn queen)
        {
            if (queen == null || queen.Destroyed || queen.Dead)
            {
                return false;
            }
            queen.jobs?.StopAll();
            if (queen.Spawned)
            {
                queen.DeSpawnOrDeselect();
            }
            if (!chamber.TryAddOrTransfer(queen))
            {
                return false;
            }
            expectingQueen = false;
            NotifyPawnListsChanged();
            return true;
        }

        public override void PostDeSpawn(Verse.Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            Release(map);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            Pawn queen = Queen;
            if (queen != null)
            {
                return "MSSG_QueenInside".Translate(queen.Named("PAWN")).Resolve();
            }
            return expectingQueen ? null : "MSSG_QueenUnknown".Translate().Resolve();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref chamber, "chamber", this);
            Scribe_Values.Look(ref expectingQueen, "expectingQueen", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && chamber == null)
            {
                chamber = new ThingOwner<Pawn>(this);
            }
        }

        private void Release(Verse.Map map)
        {
            if (map == null)
            {
                return;
            }
            IntVec3 centre = parent.OccupiedRect().CenterCell;
            Pawn queen = null;
            switch (EventRules.QueenRelease(Queen != null, expectingQueen))
            {
                case QueenReleaseAction.Release:
                    chamber.TryDrop(Queen, centre, map, ThingPlaceMode.Near, out queen);
                    break;
                case QueenReleaseAction.GenerateFresh:
                    queen = GenerateQueen(centre, map);
                    break;
            }
            if (queen == null)
            {
                return;
            }
            queen.health.AddHediff(HobGobDefOf.MSSG_PitTorpor);
            NotifyPawnListsChanged();
            if (queen.Faction == Faction.OfPlayerSilentFail)
            {
                Messages.Message("MSSG_QueenReleased".Translate(queen.Named("PAWN")), queen, MessageTypeDefOf.NeutralEvent);
            }
        }

        private Pawn GenerateQueen(IntVec3 centre, Verse.Map map)
        {
            if (parent.Faction == null)
            {
                return null;
            }
            PawnGenerationRequest request = new PawnGenerationRequest(
                PawnKindDefOf.Villager,
                parent.Faction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                allowFood: false,
                allowAddictions: false,
                forcedXenotype: HobGobDefOf.MSSG_Hobnobgoblin,
                developmentalStages: DevelopmentalStage.Adult,
                forceNoGear: true)
            {
                ForceNoIdeoGear = true
            };
            Pawn queen = PawnGenerator.GeneratePawn(request);
            if (queen == null)
            {
                return null;
            }
            if (queen.Faction != parent.Faction)
            {
                queen.SetFaction(parent.Faction);
            }
            return (Pawn)GenSpawn.Spawn(queen, CellFinder.StandableCellNear(centre, map, 5f), map);
        }

        private static void NotifyPawnListsChanged()
        {
            Find.ColonistBar?.MarkColonistsDirty();
            MainTabWindowUtility.NotifyAllPawnTables_PawnsChanged();
        }
    }
}
