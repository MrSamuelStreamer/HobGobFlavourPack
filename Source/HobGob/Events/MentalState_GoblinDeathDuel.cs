using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>
    /// Death duel. Like <see cref="MentalState_GoblinDuel"/>, but a downed partner is not the end:
    /// the fighter keeps going until that partner is dead. <see cref="GoblinDeathDuelComponent"/> decides
    /// when a draw is over.
    /// </summary>
    public class MentalState_GoblinDeathDuel : MentalState
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
            if (otherPawn == null || otherPawn.Dead || !otherPawn.Spawned || otherPawn.Map != pawn.Map)
            {
                RecoverFromState();
            }
        }

        public override bool ForceHostileTo(Thing t) => t is Pawn other && other == otherPawn;

        public override bool ForceHostileTo(Faction f) => false;

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
