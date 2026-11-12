using Verse;
using Verse.AI;

namespace HobGob.Rumble
{
    /// <summary>
    /// Backup for fighters that cannot enter the rumble mental state (some mods block mental states for
    /// certain traits or genes). Sits first in the Humanlike tree, so those fighters still fight.
    /// Skips fighters already in the state: the mental state tree drives them.
    /// </summary>
    public class ThinkNode_ConditionalRumbleFighter : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn) =>
            NobbyRumbleComponent.IsFighter(pawn) && pawn.Spawned && !pawn.Downed && !pawn.Drafted
            && pawn.MentalStateDef != HobGobDefOf.MSSG_NobbyRumble;
    }
}
