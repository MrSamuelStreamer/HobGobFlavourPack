using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HobGob.Combat
{
    /// <summary>
    /// A Noblin drafted for a threat cannot be undrafted. Game-internal undrafts still pass
    /// (downed, dead, mental break, despawned), because <see cref="NoblinCombatComponent.CanFight"/> is false then.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
    public static class Patch_DraftController_SetDrafted
    {
        public static bool Prefix(Pawn_DraftController __instance, bool value)
        {
            Pawn pawn = __instance.pawn;
            return value || !NoblinCombatComponent.IsLocked(pawn) || !NoblinCombatComponent.CanFight(pawn);
        }
    }

    /// <summary>Greys out the Draft and Big and Small Auto-Combat buttons on a locked Noblin. Runs after Big and Small adds its buttons.</summary>
    [HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
    public static class Patch_DraftController_GetGizmos
    {
        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
        {
            bool locked = NoblinCombatComponent.IsLocked(__instance.pawn);
            foreach (Gizmo gizmo in __result)
            {
                if (locked && gizmo is Command_Toggle toggle
                    && (toggle.icon == TexCommand.Draft || toggle.groupKey == BigSmallAutoCombat.HuntGizmoGroupKey))
                {
                    toggle.Disable("MSSG_NoblinCombatLocked".Translate());
                }
                yield return gizmo;
            }
        }
    }
}
