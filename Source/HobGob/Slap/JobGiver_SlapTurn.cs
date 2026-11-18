using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Slap
{
    /// <summary>Gives the thump job only to the pawn whose turn it is. Everyone else falls through to spectating.</summary>
    public class JobGiver_SlapTurn : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            RitualBehaviorWorker_SlapSense worker = RitualBehaviorWorker_SlapSense.For(pawn, out LordJob_Ritual ritual);
            if (worker == null || worker.CurrentThumper != pawn)
            {
                return null;
            }
            Pawn target = ritual.PawnWithRole(RitualBehaviorWorker_SlapSense.TargetRoleId);
            if (target == null || !target.Spawned || target.Downed || !pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                return null;
            }
            return JobMaker.MakeJob(HobGobDefOf.MSSG_ThumpTarget, target);
        }
    }
}
