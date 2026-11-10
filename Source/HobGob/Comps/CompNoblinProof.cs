using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Comps
{
    public class CompProperties_NoblinProof : CompProperties
    {
        public CompProperties_NoblinProof()
        {
            compClass = typeof(CompNoblinProof);
        }
    }

    /// <summary>Per-door switch: when on, managed Noblins cannot open the door.</summary>
    public class CompNoblinProof : ThingComp
    {
        /// <summary>
        /// Spawned doors with the switch on. Read by the PawnCanOpen patch, which 1.7 also runs on
        /// background pathing threads, so it avoids GetComp and uses a thread-safe snapshot set.
        /// Cleared when a game starts or loads (see GoblinEvolutionComponent).
        /// </summary>
        private static readonly SnapshotSet<Thing> ProofDoors = new SnapshotSet<Thing>();

        private bool noblinProof;

        public bool NoblinProof => noblinProof;

        public static bool IsNoblinProof(Thing door) => ProofDoors.Contains(door);

        public static void ClearCache() => ProofDoors.Clear();

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref noblinProof, "noblinProof", false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (noblinProof)
            {
                ProofDoors.Add(parent);
            }
        }

        public override void PostDeSpawn(Verse.Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            ProofDoors.Remove(parent);
            base.PostDeSpawn(map, mode);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            yield return new Command_Toggle
            {
                defaultLabel = "MSSG_NoblinProof".Translate(),
                defaultDesc = "MSSG_NoblinProofDesc".Translate(),
                icon = TexCommand.ForbidOn,
                isActive = () => noblinProof,
                toggleAction = Toggle,
            };
        }

        public override string CompInspectStringExtra() =>
            noblinProof ? "MSSG_NoblinProofInspect".Translate().ToString() : null;

        private void Toggle()
        {
            noblinProof = !noblinProof;
            if (noblinProof && parent.Spawned)
            {
                ProofDoors.Add(parent);
            }
            else
            {
                ProofDoors.Remove(parent);
            }
            // Reachability results for Noblins are cached; drop them so the change applies at once.
            parent.MapHeld?.reachability.ClearCache();
        }
    }

    /// <summary>Adds the switch to every door type, including modded ones.</summary>
    [StaticConstructorOnStartup]
    public static class NoblinProofInjector
    {
        static NoblinProofInjector()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.IsDoor && !def.HasComp(typeof(CompNoblinProof)))
                {
                    def.comps.Add(new CompProperties_NoblinProof());
                }
            }
        }
    }
}
