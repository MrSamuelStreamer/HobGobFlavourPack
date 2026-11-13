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
            if (pawn?.records == null)
            {
                return;
            }
            int current = pawn.records.GetAsInt(RecordDefOf.Kills);
            Find.WindowStack.Add(new Dialog_Slider(
                kills => pawn.LabelShort + " kills: " + kills,
                0,
                UnityEngine.Mathf.Max(200, current),
                kills => ApplyKillCount(pawn, kills),
                current));
        }

        private static void ApplyKillCount(Pawn pawn, int kills)
        {
            if (pawn.Destroyed || pawn.records == null)
            {
                return;
            }
            float delta = kills - pawn.records.GetAsInt(RecordDefOf.Kills);
            if (delta != 0f)
            {
                pawn.records.AddTo(RecordDefOf.Kills, delta);
            }
            DebugActionsUtility.DustPuffFrom(pawn);
            Messages.Message(pawn.LabelShort + " now has " + pawn.records.GetAsInt(RecordDefOf.Kills) + " kills.", pawn, MessageTypeDefOf.NeutralEvent);
        }
    }
}
