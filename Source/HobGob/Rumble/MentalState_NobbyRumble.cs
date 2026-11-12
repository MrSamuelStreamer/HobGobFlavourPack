using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Rumble
{
    /// <summary>
    /// Fighters see each other as enemies and nobody else. Never ends on its own:
    /// <see cref="NobbyRumbleComponent"/> starts and ends it.
    /// </summary>
    public class MentalState_NobbyRumble : MentalState
    {
        public override bool ForceHostileTo(Thing t) =>
            t is Pawn other && other != pawn && NobbyRumbleComponent.IsFighter(other) && NobbyRumbleComponent.IsFighter(pawn);

        public override bool ForceHostileTo(Faction f) => false;

        public override RandomSocialMode SocialModeMax() => RandomSocialMode.Off;
    }
}
