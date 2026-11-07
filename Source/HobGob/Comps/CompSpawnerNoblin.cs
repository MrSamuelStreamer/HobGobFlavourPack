using RimWorld;
using Verse;

namespace HobGob.Comps
{
    /// <summary>
    /// Periodically spawns an age-zero Noblin of the pit's faction.
    /// Vanilla pawn spawners force an adult and put them in an aggressive lord.
    /// </summary>
    public class CompSpawnerNoblin : ThingComp
    {
        private int nextSpawnTick = -1;

        public CompProperties_SpawnerNoblin Props => (CompProperties_SpawnerNoblin)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (nextSpawnTick < 0)
            {
                RollNextSpawn();
            }
        }

        public override void CompTickRare()
        {
            if (!parent.Spawned || nextSpawnTick < 0)
            {
                return;
            }
            if (Find.TickManager.TicksGame < nextSpawnTick)
            {
                return;
            }
            if (TrySpawnNoblin())
            {
                RollNextSpawn();
            }
        }

        public override string CompInspectStringExtra()
        {
            if (nextSpawnTick < 0)
            {
                return null;
            }
            int left = nextSpawnTick - Find.TickManager.TicksGame;
            if (left > 0)
            {
                return "MSSG_NextNoblin".Translate(left.ToStringTicksToPeriod()).ToString();
            }
            return "MSSG_NextNoblinWaiting".Translate().ToString();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextSpawnTick, "nextSpawnTick", -1);
        }

        private void RollNextSpawn()
        {
            nextSpawnTick = Find.TickManager.TicksGame + Props.spawnIntervalTicks.RandomInRange;
        }

        private bool TrySpawnNoblin()
        {
            if (!TryFindSpawnCell(out IntVec3 cell))
            {
                return false;
            }
            // Newborn sets biological and chronological age to 0 and opens the naming window.
            // PawnGenerationRequest rejects Newborn combined with Baby and keeps only Newborn.
            // Age 0 is the baby life stage, so the noblin comes out a baby.
            PawnGenerationRequest request = new PawnGenerationRequest(
                PawnKindDefOf.Villager,
                parent.Faction,
                PawnGenerationContext.NonPlayer,
                parent.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                allowFood: false,
                allowAddictions: false,
                forcedXenotype: HobGobDefOf.MSSG_Noblin,
                developmentalStages: DevelopmentalStage.Newborn);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            if (pawn == null)
            {
                return false;
            }
            if (pawn.ageTracker.AgeBiologicalTicks != 0)
            {
                pawn.ageTracker.AgeBiologicalTicks = 0;
            }
            if (pawn.ageTracker.AgeChronologicalTicks != 0)
            {
                pawn.ageTracker.AgeChronologicalTicks = 0;
            }
            if (parent.Faction != null && pawn.Faction != parent.Faction)
            {
                pawn.SetFaction(parent.Faction);
            }
            GenSpawn.Spawn(pawn, cell, parent.Map);
            MessageTypeDef messageType = parent.Faction == Faction.OfPlayer
                ? MessageTypeDefOf.PositiveEvent
                : MessageTypeDefOf.NegativeEvent;
            Messages.Message("MSSG_NoblinSpawned".Translate(), pawn, messageType);
            return true;
        }

        /// <summary>A standable cell on the ring immediately outside the pit.</summary>
        private bool TryFindSpawnCell(out IntVec3 cell)
        {
            Verse.Map map = parent.Map;
            CellRect occupied = parent.OccupiedRect();
            foreach (IntVec3 candidate in occupied.ExpandedBy(1).EdgeCells.InRandomOrder())
            {
                if (candidate.InBounds(map) && candidate.Standable(map) && !candidate.Fogged(map))
                {
                    cell = candidate;
                    return true;
                }
            }
            cell = IntVec3.Invalid;
            return false;
        }
    }
}
