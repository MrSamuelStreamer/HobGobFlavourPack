using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>Melee the death-duel partner, and keep hitting after they go down.</summary>
    public class JobGiver_GoblinDeathDuel : ThinkNode_JobGiver
    {
        private const int RetargetTicks = 600;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn.MentalState is MentalState_GoblinDeathDuel duel) || pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return null;
            }
            Pawn otherPawn = duel.otherPawn;
            if (otherPawn == null || otherPawn.Dead || !otherPawn.Spawned || otherPawn.Map != pawn.Map)
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, otherPawn);
            job.killIncappedTarget = true;
            job.expiryInterval = RetargetTicks;
            job.checkOverrideOnExpire = true;
            job.collideWithPawns = true;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            return job;
        }
    }
}
