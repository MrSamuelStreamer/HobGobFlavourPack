using HarmonyLib;
using HobGob.Grot;
using RimWorld;
using Verse;

namespace HobGob.HarmonyPatches
{
    /// <summary>
    /// Bedroom, barracks and dining room thoughts for grot lovers use grot impressiveness instead of the vanilla score.
    /// Vanilla picks the stage just before calling TryGainMemory, so the stage is re-picked here.
    /// </summary>
    [HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory), typeof(Thought_Memory), typeof(Pawn))]
    public static class Patch_MemoryThoughtHandler_TryGainMemory
    {
        /// <returns>False to drop the thought when the grot stage has no vanilla stage defined.</returns>
        public static bool Prefix(MemoryThoughtHandler __instance, Thought_Memory newThought)
        {
            ThoughtDef def = newThought?.def;
            if (def != ThoughtDefOf.SleptInBedroom && def != ThoughtDefOf.SleptInBarracks && def != ThoughtDefOf.AteInImpressiveDiningRoom)
            {
                return true;
            }
            Pawn pawn = __instance.pawn;
            if (!GrotLover.Is(pawn))
            {
                return true;
            }
            Room room = GrotLover.IndoorRoom(pawn.GetRoom());
            if (room == null)
            {
                return true;
            }
            float score = GrotRules.GrotImpressiveness(
                room.GetStat(RoomStatDefOf.Wealth),
                room.GetStat(RoomStatDefOf.Beauty),
                room.GetStat(RoomStatDefOf.Space),
                GrotLover.Cleanliness(room));
            int stage = RoomStatDefOf.Impressiveness.GetScoreStageIndex(score);
            if (stage < 0 || stage >= def.stages.Count || def.stages[stage] == null)
            {
                return false;
            }
            newThought.SetForcedStage(stage);
            return true;
        }
    }

    /// <summary>A grot lover resents every mess they have to clean.</summary>
    [HarmonyPatch(typeof(Pawn_RecordsTracker), nameof(Pawn_RecordsTracker.Increment))]
    public static class Patch_RecordsTracker_Increment
    {
        public static void Postfix(Pawn_RecordsTracker __instance, RecordDef def)
        {
            if (def != RecordDefOf.MessesCleaned)
            {
                return;
            }
            Pawn pawn = __instance.pawn;
            if (GrotLover.Is(pawn) && pawn.needs?.mood != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(HobGobDefOf.MSSG_WastedMuck);
            }
        }
    }

    /// <summary>To a grot lover, ugly pawns look pretty.</summary>
    [HarmonyPatch(typeof(ThoughtWorker_Pretty), "CurrentSocialStateInternal")]
    public static class Patch_ThoughtWorkerPretty
    {
        public static bool Prefix(Pawn pawn, Pawn other, ref ThoughtState __result)
        {
            if (!GrotLover.Is(pawn))
            {
                return true;
            }
            __result = GrotSocial.State(pawn, other, false, GrotRules.GrotPrettyStage);
            return false;
        }
    }

    /// <summary>To a grot lover, pretty pawns look ugly.</summary>
    [HarmonyPatch(typeof(ThoughtWorker_Ugly), "CurrentSocialStateInternal")]
    public static class Patch_ThoughtWorkerUgly
    {
        public static bool Prefix(Pawn pawn, Pawn other, ref ThoughtState __result)
        {
            if (!GrotLover.Is(pawn))
            {
                return true;
            }
            __result = GrotSocial.State(pawn, other, true, GrotRules.GrotUglyStage);
            return false;
        }
    }

    internal static class GrotSocial
    {
        /// <summary>Vanilla's guards for the pretty/ugly social thoughts, then the grot stage for the other pawn's beauty.</summary>
        public static ThoughtState State(Pawn pawn, Pawn other, bool needsCaresAboutLooks, System.Func<float, int> stageFor)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (PawnUtility.IsBiologicallyOrArtificiallyBlind(pawn))
            {
                return false;
            }
            if (needsCaresAboutLooks && !pawn.story.CaresAboutOthersAppearance)
            {
                return false;
            }
            int stage = stageFor(other.GetStatValue(StatDefOf.PawnBeauty));
            return stage < 0 ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(stage);
        }
    }
}
