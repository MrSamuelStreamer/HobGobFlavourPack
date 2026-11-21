using HarmonyLib;
using RimWorld;
using Verse;

namespace HobGob.HarmonyPatches
{
    /// <summary>
    /// Fings is fings: everything the carrier makes comes out Normal quality.
    /// Crafting, construction (Frame) and art all roll quality here, and so do the inspiration and
    /// production-specialist bonuses, so one postfix covers them all.
    /// </summary>
    [HarmonyPatch(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn),
        new[] { typeof(Pawn), typeof(SkillDef), typeof(bool) })]
    public static class Patch_QualityUtility_GenerateQualityCreatedByPawn
    {
        // Other mods (Vanilla Traits Expanded, Vanilla Memes Expanded, ...) postfix this method too. Run last so
        // their quality shifts cannot lift a fings-is-fings carrier off Normal.
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Pawn pawn, ref QualityCategory __result)
        {
            if (pawn?.genes != null && pawn.genes.HasActiveGene(HobGobDefOf.MSSG_FingsIsFings))
            {
                __result = QualityCategory.Normal;
            }
        }
    }
}
