using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>
    /// E2 duel. Like a vanilla social fight, but it does not end the partner's state when it ends, and it does not
    /// recover on its own. <see cref="HobgoblinChallengeComponent"/> decides when the duel is over.
    /// </summary>
    public class MentalState_GoblinDuel : MentalState
    {
        public Pawn otherPawn;

        public override void PreStart()
        {
            base.PreStart();
            // TryStartMentalState only fills otherPawn for vanilla social fights; it gives us causedByPawn.
            otherPawn ??= causedByPawn;
        }

        public override void MentalStateTick(int delta)
        {
            // Only stop for a partner who can no longer fight; the component turns that into a result.
            if (otherPawn == null || otherPawn.Dead || otherPawn.Downed || !otherPawn.Spawned || otherPawn.Map != pawn.Map)
            {
                RecoverFromState();
            }
        }

        public override void PostEnd()
        {
            base.PostEnd();
            pawn.jobs?.StopAll();
            pawn.mindState.meleeThreat = null;
        }

        public override RandomSocialMode SocialModeMax() => RandomSocialMode.Off;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref otherPawn, "otherPawn");
        }
    }
}
