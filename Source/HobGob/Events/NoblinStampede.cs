using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>
    /// A spooked Noblin sprinting with the pack. In a mental state it ignores forbidden doors (vanilla rule);
    /// noblin-proof doors still stop it.
    /// </summary>
    public class MentalState_NoblinStampede : MentalState
    {
        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            // Door access changes with the mental state, so cached reachability is stale.
            pawn.MapHeld?.reachability.ClearCacheFor(pawn);
        }

        public override void PostEnd()
        {
            base.PostEnd();
            pawn.MapHeld?.reachability.ClearCacheFor(pawn);
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(HobGobDefOf.MSSG_Winded);
        }
    }

    /// <summary>Sprint to the pack's target, or to a far cell of its own if the pack target is out of reach.</summary>
    public class JobGiver_NoblinStampede : ThinkNode_JobGiver
    {
        private const int LegJobExpiry = 600;

        protected override Job TryGiveJob(Pawn pawn)
        {
            NoblinStampedeComponent stampede = pawn.Map?.GetComponent<NoblinStampedeComponent>();
            if (stampede == null)
            {
                return null;
            }
            IntVec3 dest = stampede.PackTargetFor(pawn);
            if (!dest.IsValid || !pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly))
            {
                dest = stampede.FarCellFor(pawn);
            }
            if (!dest.IsValid || dest == pawn.Position)
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.expiryInterval = LegJobExpiry;
            job.checkOverrideOnExpire = true;
            return job;
        }
    }
}
