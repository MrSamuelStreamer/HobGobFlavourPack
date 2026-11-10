using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Comps
{
    /// <summary>
    /// Periodically spawns a 13-year-old Noblin of the pit's faction.
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

        /// <summary>Pushes the regular spawn back by a fresh random interval.</summary>
        public void ResetSpawnTimer()
        {
            RollNextSpawn();
        }

        private void RollNextSpawn()
        {
            nextSpawnTick = Find.TickManager.TicksGame + Props.spawnIntervalTicks.RandomInRange;
        }

        private bool TrySpawnNoblin()
        {
            IntVec3 cell = FreeSpawnCells().RandomElementWithFallback(IntVec3.Invalid);
            return cell.IsValid && SpawnNoblinAt(cell, announce: true) != null;
        }

        /// <summary>
        /// Makes a naked 13-year-old Noblin of the pit's faction at <paramref name="cell"/>.
        /// Returns null if generation fails.
        /// </summary>
        public Pawn SpawnNoblinAt(IntVec3 cell, bool announce)
        {
            // Age 13 is the teenage life stage. That stage counts as an adult.
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
                fixedBiologicalAge: 13f,
                fixedChronologicalAge: 13f,
                developmentalStages: DevelopmentalStage.Adult,
                forceNoGear: true);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            if (pawn == null)
            {
                return null;
            }
            if (parent.Faction != null && pawn.Faction != parent.Faction)
            {
                pawn.SetFaction(parent.Faction);
            }
            GenSpawn.Spawn(pawn, cell, parent.Map);
            NoblinPortraitHiding.Hide(pawn);
            if (announce)
            {
                MessageTypeDef messageType = parent.Faction == Faction.OfPlayer
                    ? MessageTypeDefOf.PositiveEvent
                    : MessageTypeDefOf.NegativeEvent;
                Messages.Message("MSSG_NoblinSpawned".Translate(), pawn, messageType);
            }
            return pawn;
        }

        /// <summary>Standable, unfogged cells on the ring immediately outside the pit.</summary>
        public IEnumerable<IntVec3> FreeSpawnCells()
        {
            Verse.Map map = parent.Map;
            foreach (IntVec3 candidate in parent.OccupiedRect().ExpandedBy(1).EdgeCells)
            {
                if (candidate.InBounds(map) && candidate.Standable(map) && !candidate.Fogged(map))
                {
                    yield return candidate;
                }
            }
        }
    }
}
