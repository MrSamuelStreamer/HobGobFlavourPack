using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob
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
        private bool noblinProof;

        public bool NoblinProof => noblinProof;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref noblinProof, "noblinProof", false);
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
