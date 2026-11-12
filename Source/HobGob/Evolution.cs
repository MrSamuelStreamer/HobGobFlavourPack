using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HobGob
{
    public class EvolutionExtension : DefModExtension
    {
        public XenotypeDef nextXenotype;
        public int totalKillsRequired;

        /// <summary>Most pawns of <see cref="nextXenotype"/> allowed on one map. 0 = no cap.</summary>
        public int maxPerMap;

        /// <summary>Optional rare outcome: evolve into this instead of <see cref="nextXenotype"/> with chance <see cref="rareChance"/> (0-1).</summary>
        public XenotypeDef rareXenotype;
        public float rareChance;

        /// <summary>Player pawns evolve only when the player clicks the evolve gizmo. Other factions still evolve on their own.</summary>
        public bool manualEvolution;

        /// <summary>Icon for the evolve gizmo. Falls back to a vanilla icon if missing.</summary>
        public string gizmoIconPath;

        /// <summary>This stage rolls for free kills at each season start. Chance and amount are mod settings.</summary>
        public bool seasonalKills;

        public XenotypeDef RollTarget() =>
            rareXenotype != null && EvolutionRules.RollsRare(Rand.Value, rareChance) ? rareXenotype : nextXenotype;
    }

    /// <summary>
    /// Checks the kill count now and then and asks for evolution. Never changes genes itself.
    /// Keeps a <see cref="Hediff_EvolutionProgress"/> on the pawn so players can see the progress.
    /// </summary>
    public class Gene_GoblinEvolution : Gene
    {
        private const int CheckInterval = GenDate.TicksPerHour;
        private const string FallbackIconPath = "UI/Icons/Genes/Gene_Hobnobgoblin";

        private static readonly Dictionary<string, Texture2D> Icons = new Dictionary<string, Texture2D>();

        private static HediffDef progressHediff;
        private static HediffDef ProgressHediff => progressHediff ??= DefDatabase<HediffDef>.GetNamed("MSSG_EvolutionProgress");

        private EvolutionExtension Extension => def.GetModExtension<EvolutionExtension>();

        /// <summary>False for a final-stage gene (no EvolutionExtension), which gets no progress tracker.</summary>
        public bool HasNextStage => Extension?.nextXenotype != null;

        public int KillsRequired => Extension?.totalKillsRequired ?? 0;

        public int Kills => pawn.records?.GetAsInt(RecordDefOf.Kills) ?? 0;

        public bool GetsSeasonalKills => Extension?.seasonalKills ?? false;

        /// <summary>True when this pawn waits for the player to press the evolve gizmo.</summary>
        public bool IsManual => Extension != null && !EvolutionRules.EvolvesAutomatically(Extension.manualEvolution, pawn.Faction == Faction.OfPlayerSilentFail);

        /// <summary>Has the kills and room on the map to evolve now.</summary>
        public bool CanEvolveNow
        {
            get
            {
                EvolutionExtension ext = Extension;
                return ext?.nextXenotype != null && pawn.records != null
                    && EvolutionRules.ShouldEvolve(Kills, ext.totalKillsRequired, GoblinEvolutionComponent.CountOnMap(pawn.MapHeld, ext.nextXenotype), ext.maxPerMap);
            }
        }

        public override void PostAdd()
        {
            base.PostAdd();
            EnsureProgressHediff();
        }

        public override void PostRemove()
        {
            base.PostRemove();
            if (Hediff_EvolutionProgress.FindTracker(pawn, exclude: this) != null)
            {
                return;
            }
            Hediff hediff = pawn.health?.hediffSet.GetFirstHediffOfDef(ProgressHediff);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!pawn.IsHashIntervalTick(CheckInterval, delta) || pawn.Dead)
            {
                return;
            }
            // Re-adds the tracker if something removed it, and back-fills goblins from older saves.
            EnsureProgressHediff();
            if (!IsManual && CanEvolveNow)
            {
                Current.Game.GetComponent<GoblinEvolutionComponent>()?.Enqueue(pawn, Extension);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Vanilla Gene.GetGizmos returns null, not an empty sequence.
            IEnumerable<Gizmo> baseGizmos = base.GetGizmos();
            if (baseGizmos != null)
            {
                foreach (Gizmo gizmo in baseGizmos)
                {
                    yield return gizmo;
                }
            }
            EvolutionExtension ext = Extension;
            if (ext?.nextXenotype == null || !IsManual || !pawn.IsColonistPlayerControlled)
            {
                yield break;
            }
            Command_Action command = new Command_Action
            {
                defaultLabel = "MSSG_EvolveGizmo".Translate(ext.nextXenotype.label),
                defaultDesc = "MSSG_EvolveGizmoDesc".Translate(pawn.Named("PAWN"), ext.nextXenotype.label),
                icon = Icon(ext.gizmoIconPath),
                action = () => Current.Game.GetComponent<GoblinEvolutionComponent>()?.Enqueue(pawn, ext),
            };
            if (Kills < ext.totalKillsRequired)
            {
                command.Disable("MSSG_EvolveNotEnoughKills".Translate(Kills, ext.totalKillsRequired));
            }
            else if (!CanEvolveNow)
            {
                command.Disable("MSSG_EvolveCapReached".Translate(ext.nextXenotype.label));
            }
            yield return command;
        }

        private static Texture2D Icon(string path)
        {
            path = string.IsNullOrEmpty(path) ? FallbackIconPath : path;
            if (!Icons.TryGetValue(path, out Texture2D tex))
            {
                tex = ContentFinder<Texture2D>.Get(path, false) ?? ContentFinder<Texture2D>.Get(FallbackIconPath, false) ?? BaseContent.BadTex;
                Icons[path] = tex;
            }
            return tex;
        }

        private void EnsureProgressHediff()
        {
            if (!HasNextStage || pawn.Dead || pawn.health?.hediffSet == null || pawn.health.hediffSet.HasHediff(ProgressHediff))
            {
                return;
            }
            pawn.health.AddHediff(ProgressHediff);
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
            Comps.CompNoblinProof.ClearCache();
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
                Evolve(pawn, ext.RollTarget());
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
            if (old == HobGobDefOf.MSSG_Noblin)
            {
                NoblinPortraitHiding.Show(pawn);
            }
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
