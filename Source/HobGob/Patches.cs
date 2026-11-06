using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HobGob
{
    /// <summary>
    /// With manual priorities off, vanilla reports every enabled work type as 3.
    /// Managed Noblins keep their real priorities so they still pick the urgent job first.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.GetPriority))]
    public static class Patch_WorkSettings_GetPriority
    {
        public static void Postfix(WorkTypeDef w, ref int __result, Pawn ___pawn, DefMap<WorkTypeDef, int> ___priorities)
        {
            if (__result == Pawn_WorkSettings.DefaultPriority
                && !Find.PlaySettings.useWorkPriorities
                && NoblinRegistry.IsManaged(___pawn))
            {
                __result = ___priorities[w];
            }
        }
    }

    /// <summary>Noblins pick their own work, so the Work tab does not list them.</summary>
    [HarmonyPatch(typeof(MainTabWindow_Work), "Pawns", MethodType.Getter)]
    public static class Patch_MainTabWindow_Work_Pawns
    {
        public static void Postfix(ref IEnumerable<Pawn> __result)
        {
            __result = __result.Where(p => !NoblinRegistry.IsManaged(p));
        }
    }
}
