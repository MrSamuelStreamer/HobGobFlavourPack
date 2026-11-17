using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>One social-fight blow at the nearest standing fighter from the other side.</summary>
    public class JobGiver_GobBrawl : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.MentalStateDef != HobGobDefOf.MSSG_GobBrawl || pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return null;
            }
            Pawn target = GobBrawlComponent.For(pawn.Map)?.NearestEnemy(pawn);
            if (target == null || !SocialInteractionUtility.TryGetRandomVerbForSocialFight(pawn, out Verb verb))
            {
                return null;
            }
            // SocialFight keeps vanilla's "no destroyed body parts" rule for the blows.
            Job job = JobMaker.MakeJob(JobDefOf.SocialFight, target);
            job.maxNumMeleeAttacks = 1;
            job.verbToUse = verb;
            return job;
        }
    }
}
