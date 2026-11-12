using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            // Runs constantly during pathing, and on background threads in 1.7: thread-safe set lookups only.
            if (__result && NoblinRegistry.IsManaged(p) && CompNoblinProof.IsNoblinProof(__instance))
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

    /// <summary>
    /// Vanilla Ideology Expanded - Memes and Structures: insect nests never need maintenance.
    /// The nest destroys itself when maintenance reaches 0, and its work giver reads the field,
    /// so the field is held at full instead of swapping the nest's class.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_VME_InsectNest_Tick
    {
        private const int FullMaintenance = 100;
        private static readonly Type NestType = AccessTools.TypeByName("VanillaMemesExpanded.Building_InsectNest");
        private static AccessTools.FieldRef<object, int> maintenance;

        public static bool Prepare() => NestType != null && AccessTools.Field(NestType, "maintenance") != null;

        public static MethodBase TargetMethod() => AccessTools.Method(NestType, "Tick");

        public static void Postfix(object __instance)
        {
            maintenance ??= AccessTools.FieldRefAccess<int>(NestType, "maintenance");
            maintenance(__instance) = FullMaintenance;
        }
    }

    /// <summary>Queens stored in a spawning pit are not in the Work and Schedule tabs (both use base.Pawns).</summary>
    [HarmonyPatch(typeof(MainTabWindow_PawnTable), "Pawns", MethodType.Getter)]
    public static class Patch_MainTabWindow_PawnTable_Pawns
    {
        public static void Postfix(ref IEnumerable<Pawn> __result)
        {
            __result = __result.Where(p => !CompQueenChamber.IsStoredQueen(p));
        }
    }

    /// <summary>The Assign tab lists pawns through PawnsFinder, not base.Pawns.</summary>
    [HarmonyPatch(typeof(MainTabWindow_Assign), "Pawns", MethodType.Getter)]
    public static class Patch_MainTabWindow_Assign_Pawns
    {
        public static void Postfix(ref IEnumerable<Pawn> __result)
        {
            __result = __result.Where(p => !CompQueenChamber.IsStoredQueen(p));
        }
    }

    /// <summary>Colonist bar: swaps its MapPawns.FreeColonists read for a list without stored queens.</summary>
    [HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
    public static class Patch_ColonistBar_CheckRecacheEntries
    {
        private static readonly MethodInfo FreeColonists = AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.FreeColonists));
        private static readonly MethodInfo Visible = AccessTools.Method(typeof(Patch_ColonistBar_CheckRecacheEntries), nameof(VisibleFreeColonists));
        private static readonly List<Pawn> Buffer = new List<Pawn>();

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(FreeColonists))
                {
                    replaced++;
                    yield return new CodeInstruction(OpCodes.Call, Visible).WithLabels(instruction.labels).WithBlocks(instruction.blocks);
                    continue;
                }
                yield return instruction;
            }
            if (replaced != 1)
            {
                Log.Warning($"[Goblin Flavour Pack] Colonist bar patch replaced {replaced} FreeColonists calls (expected 1). Stored queens may show in the bar.");
            }
        }

        /// <summary>Called only from the colonist bar, which copies the list straight away, so a shared buffer is safe.</summary>
        public static List<Pawn> VisibleFreeColonists(MapPawns mapPawns)
        {
            List<Pawn> all = mapPawns.FreeColonists;
            Buffer.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                if (!CompQueenChamber.IsStoredQueen(all[i]))
                {
                    Buffer.Add(all[i]);
                }
            }
            return Buffer;
        }
    }
}
