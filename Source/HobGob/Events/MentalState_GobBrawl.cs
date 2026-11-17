using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>
    /// Knob Gob Gobbo Gone Wild: noblins and goblins scrap with social-fight blows.
    /// No forced hostility, same as <see cref="MentalState_GoblinDuel"/>: hostile pawns trigger vanilla's lethal
    /// melee-threat responses, which would bypass the social-fight rules. Never recovers on its own:
    /// <see cref="GobBrawlComponent"/> ends it.
    /// </summary>
    public class MentalState_GobBrawl : MentalState
    {
        public override void PostEnd()
        {
            base.PostEnd();
            pawn.jobs?.StopAll();
            pawn.mindState.meleeThreat = null;
        }

        public override RandomSocialMode SocialModeMax() => RandomSocialMode.Off;
    }
}
