using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Rumble
{
    /// <summary>Attack the nearest other fighter, and keep hitting it after it goes down.</summary>
    public class JobGiver_NobbyRumble : ThinkNode_JobGiver
    {
        private const int RetargetTicks = 600;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!NobbyRumbleComponent.IsFighter(pawn) || pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return null;
            }
            Pawn target = NobbyRumbleComponent.For(pawn.Map)?.NearestTarget(pawn);
            if (target == null)
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.killIncappedTarget = true;
            job.expiryInterval = RetargetTicks;
            job.checkOverrideOnExpire = true;
            job.collideWithPawns = true;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            return job;
        }
    }
}
