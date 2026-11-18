using RimWorld;
using Verse;

namespace HobGob.Slap
{
    /// <summary>The pawn who gets the sense slapped into them: any colonist, slave or prisoner of the colony with skills.</summary>
    public class RitualRole_SlapTarget : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            if (!AppliesIfChild(p, out reason, skipReason))
            {
                return false;
            }
            bool ours = p.IsColonist || p.IsSlaveOfColony || p.IsPrisonerOfColony;
            if (!ours || p.skills == null || p.Downed)
            {
                if (!skipReason)
                {
                    reason = "MSSG_SlapTargetInvalid".Translate(LabelCap);
                }
                return false;
            }
            // Cooldown so passions cannot be farmed by slapping the same pawn over and over.
            if (p.health.hediffSet.HasHediff(HobGobDefOf.MSSG_RecentlySlapped))
            {
                if (!skipReason)
                {
                    reason = "MSSG_SlapTooSoon".Translate(p.Named("PAWN"));
                }
                return false;
            }
            return true;
        }

        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn p = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }
    }
}
