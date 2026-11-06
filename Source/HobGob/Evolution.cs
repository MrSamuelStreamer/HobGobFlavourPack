using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob
{
    public class EvolutionExtension : DefModExtension
    {
        public XenotypeDef nextXenotype;
        public int totalKillsRequired;

        /// <summary>Most pawns of <see cref="nextXenotype"/> allowed on one map. 0 = no cap.</summary>
        public int maxPerMap;
    }

    /// <summary>Checks the kill count now and then and asks for evolution. Never changes genes itself.</summary>
    public class Gene_GoblinEvolution : Gene
    {
        private const int CheckInterval = GenDate.TicksPerHour;

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!pawn.IsHashIntervalTick(CheckInterval, delta) || pawn.Dead)
            {
                return;
            }
            EvolutionExtension ext = def.GetModExtension<EvolutionExtension>();
            if (ext?.nextXenotype == null || pawn.records == null)
            {
                return;
            }
            int kills = pawn.records.GetAsInt(RecordDefOf.Kills);
            if (EvolutionRules.ShouldEvolve(kills, ext.totalKillsRequired, GoblinEvolutionComponent.CountOnMap(pawn.MapHeld, ext.nextXenotype), ext.maxPerMap))
            {
                Current.Game.GetComponent<GoblinEvolutionComponent>()?.Enqueue(pawn, ext);
            }
        }
    }

    /// <summary>
    /// Runs queued evolutions outside the gene tick (genes cannot be swapped while the tracker iterates them),
    /// and back-fills new genes onto goblins from older saves.
    /// </summary>
    public class GoblinEvolutionComponent : GameComponent
    {
        private readonly Dictionary<Pawn, EvolutionExtension> queue = new Dictionary<Pawn, EvolutionExtension>();

        public GoblinEvolutionComponent(Game game)
        {
            NoblinRegistry.Clear();
        }

        public void Enqueue(Pawn pawn, EvolutionExtension ext) => queue[pawn] = ext;

        public override void LoadedGame() => BackfillGenes();

        public override void GameComponentTick()
        {
            if (queue.Count == 0)
            {
                return;
            }
            List<KeyValuePair<Pawn, EvolutionExtension>> pending = new List<KeyValuePair<Pawn, EvolutionExtension>>(queue);
            queue.Clear();
            foreach (KeyValuePair<Pawn, EvolutionExtension> entry in pending)
            {
                Pawn pawn = entry.Key;
                EvolutionExtension ext = entry.Value;
                // Re-check the cap: several pawns may have queued in the same pass.
                if (pawn.Dead || pawn.genes == null
                    || !EvolutionRules.ShouldEvolve(pawn.records.GetAsInt(RecordDefOf.Kills), ext.totalKillsRequired, CountOnMap(pawn.MapHeld, ext.nextXenotype), ext.maxPerMap))
                {
                    continue;
                }
                Evolve(pawn, ext.nextXenotype);
            }
        }

        public static int CountOnMap(Map map, XenotypeDef xenotype)
        {
            if (map == null)
            {
                return 0;
            }
            int count = 0;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p.genes != null && p.genes.Xenotype == xenotype)
                {
                    count++;
                }
            }
            return count;
        }

        private static void Evolve(Pawn pawn, XenotypeDef next)
        {
            XenotypeDef old = pawn.genes.Xenotype;
            // SetXenotype only clears xenogenes; goblin xenotypes are inheritable, so their genes are endogenes.
            RemoveEndogenes(pawn, old);
            pawn.genes.SetXenotype(next);
            if (pawn.Faction == Faction.OfPlayer)
            {
                Messages.Message("MSSG_GoblinEvolved".Translate(pawn.Named("PAWN"), old.label, next.label),
                    pawn, MessageTypeDefOf.PositiveEvent);
            }
            if (pawn.Spawned)
            {
                FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 2f, UnityEngine.Color.green);
            }
        }

        private static void RemoveEndogenes(Pawn pawn, XenotypeDef xenotype)
        {
            List<Gene> endo = pawn.genes.Endogenes;
            for (int i = endo.Count - 1; i >= 0; i--)
            {
                if (xenotype.genes.Contains(endo[i].def))
                {
                    pawn.genes.RemoveGene(endo[i]);
                }
            }
        }

        private static readonly HashSet<string> GoblinXenotypes = new HashSet<string> { "MSSG_Noblin", "MSSG_Goblin", "MSSG_Hobgoblin" };

        /// <summary>Pawns made before this version lack the new genes; add the missing ones and drop the ones they replace.</summary>
        private static void BackfillGenes()
        {
            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive)
            {
                XenotypeDef xeno = pawn.genes?.Xenotype;
                if (xeno == null || !GoblinXenotypes.Contains(xeno.defName))
                {
                    continue;
                }
                foreach (GeneDef geneDef in xeno.genes)
                {
                    if (pawn.genes.GetGene(geneDef) != null)
                    {
                        continue;
                    }
                    RemoveConflicting(pawn, geneDef);
                    pawn.genes.AddGene(geneDef, !xeno.inheritable);
                }
            }
        }

        private static void RemoveConflicting(Pawn pawn, GeneDef incoming)
        {
            List<Gene> genes = pawn.genes.GenesListForReading;
            for (int i = genes.Count - 1; i >= 0; i--)
            {
                if (genes[i].def != incoming && genes[i].def.ConflictsWith(incoming))
                {
                    pawn.genes.RemoveGene(genes[i]);
                }
            }
        }
    }
}
