using LudeonTK;
using RimWorld;
using Verse;

namespace HobGob
{
    /// <summary>Dev-mode tools. Setting kills changes the record only: it does not count as a real kill, so the Queenie roll does not fire.</summary>
    public static class DebugActions
    {
        [DebugAction("Goblins", "Set kill count", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetKillCount(Pawn pawn)
        {
            Gene_GoblinEvolution tracker = Hediff_EvolutionProgress.FindTracker(pawn);
            if (tracker == null)
            {
                return;
            }
            int current = tracker.Kills;
            Find.WindowStack.Add(new Dialog_Slider(
                kills => pawn.LabelShort + " evolution kills: " + kills,
                0,
                UnityEngine.Mathf.Max(200, current),
                kills => ApplyKillCount(pawn, kills),
                current));
        }

        private static void ApplyKillCount(Pawn pawn, int kills)
        {
            Gene_GoblinEvolution tracker = Hediff_EvolutionProgress.FindTracker(pawn);
            if (pawn.Destroyed || tracker == null)
            {
                return;
            }
            tracker.SetKills(kills);
            DebugActionsUtility.DustPuffFrom(pawn);
            Messages.Message(pawn.LabelShort + " now has " + tracker.Kills + " evolution kills.", pawn, MessageTypeDefOf.NeutralEvent);
        }
    }
}
