using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>Melee the duel partner, one swing per job, like vanilla social fighting.</summary>
    public class JobGiver_GoblinDuel : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn.MentalState is MentalState_GoblinDuel duel) || pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return null;
            }
            Pawn otherPawn = duel.otherPawn;
            if (otherPawn == null || !otherPawn.Spawned || otherPawn.Map != pawn.Map)
            {
                return null;
            }
            if (!SocialInteractionUtility.TryGetRandomVerbForSocialFight(pawn, out Verb verb))
            {
                return null;
            }
            // SocialFight keeps vanilla's "no destroyed body parts" rule for the blows.
            Job job = JobMaker.MakeJob(JobDefOf.SocialFight, otherPawn);
            job.maxNumMeleeAttacks = 1;
            job.verbToUse = verb;
            return job;
        }
    }
}
