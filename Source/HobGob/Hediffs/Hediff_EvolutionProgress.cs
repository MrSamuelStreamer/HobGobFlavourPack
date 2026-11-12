using System.Collections.Generic;
using Verse;

namespace HobGob
{
    /// <summary>
    /// Effect-free marker that shows evolution progress on the Health tab. Stores nothing:
    /// the label is read live from the pawn's <see cref="Gene_GoblinEvolution"/>.
    /// </summary>
    public class Hediff_EvolutionProgress : HediffWithComps
    {
        // Base ShouldRemove is Severity <= 0. Stay while any evolution gene is present, so an orphan
        // (gene def dropped on load, which skips Gene.PostRemove) still cleans itself up.
        public override bool ShouldRemove => FindTracker(pawn) == null;

        public override string LabelBase => FindTracker(pawn)?.def.label ?? base.LabelBase;

        public override string LabelInBrackets
        {
            get
            {
                Gene_GoblinEvolution tracker = FindTracker(pawn);
                if (tracker == null || tracker.KillsRequired <= 0)
                {
                    return base.LabelInBrackets;
                }
                if (tracker.IsManual && tracker.CanEvolveNow)
                {
                    return "MSSG_EvolutionReadyLabel".Translate();
                }
                return "MSSG_EvolutionProgressLabel".Translate(tracker.Kills, tracker.KillsRequired);
            }
        }

        /// <summary>First evolution gene with a next stage. Ignores Active on purpose: an overridden gene still owns the tracker.</summary>
        public static Gene_GoblinEvolution FindTracker(Pawn pawn, Gene exclude = null)
        {
            List<Gene> genes = pawn?.genes?.GenesListForReading;
            if (genes == null)
            {
                return null;
            }
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i] != exclude && genes[i] is Gene_GoblinEvolution tracker && tracker.HasNextStage)
                {
                    return tracker;
                }
            }
            return null;
        }
    }
}
