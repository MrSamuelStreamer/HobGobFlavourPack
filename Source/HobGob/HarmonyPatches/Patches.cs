using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HobGob.Comps;
using RimWorld;
using Verse;

namespace HobGob.HarmonyPatches
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

    /// <summary>
    /// Noblins pick their own work, so the Work tab does not list them.
    /// Checks the gene, not the registry: the registry is empty after a load until the first work pass,
    /// and skips Noblins whose work settings are not set up yet.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Work), "Pawns", MethodType.Getter)]
    public static class Patch_MainTabWindow_Work_Pawns
    {
        public static void Postfix(ref IEnumerable<Pawn> __result)
        {
            __result = __result.Where(p => !NoblinRegistry.IsManaged(p) && !NoblinRegistry.IsNoblin(p));
        }
    }

    /// <summary>Managed Noblins cannot open doors marked noblin-proof.</summary>
    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.PawnCanOpen))]
    public static class Patch_Building_Door_PawnCanOpen
    {
        public static void Postfix(Building_Door __instance, Pawn p, ref bool __result)
        {
            // Registry lookup first: this runs constantly during pathing.
            if (__result && NoblinRegistry.IsManaged(p) && __instance.GetComp<CompNoblinProof>()?.NoblinProof == true)
            {
                __result = false;
            }
        }
    }

    /// <summary>A stampeding Noblin about to pass a door leaves it held open (E5 Noblin Stampede).</summary>
    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.Notify_PawnApproaching))]
    public static class Patch_Building_Door_Notify_PawnApproaching
    {
        public static void Postfix(Building_Door __instance, Pawn p)
        {
            // Cheap check first: this runs for every pawn at every door.
            if (p.MentalStateDef == HobGobDefOf.MSSG_NoblinStampede && __instance.Spawned)
            {
                __instance.Map.GetComponent<Events.NoblinStampedeComponent>()?.NoteDoorPassed(__instance);
            }
        }
    }
}
