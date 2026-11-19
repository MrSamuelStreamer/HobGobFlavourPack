using HarmonyLib;
using HobGob.Grot;
using RimWorld;
using Verse;

namespace HobGob.HarmonyPatches
{
    /// <summary>Grot lovers enjoy filth and ugliness and dislike pretty things.</summary>
    [HarmonyPatch(typeof(Need_Beauty), nameof(Need_Beauty.CurrentInstantBeauty))]
    public static class Patch_NeedBeauty_CurrentInstantBeauty
    {
        public static void Postfix(Pawn ___pawn, ref float __result)
        {
            // Unspawned pawns get vanilla's flat 0.5. Leave that alone.
            if (___pawn != null && ___pawn.SpawnedOrAnyParentSpawned && GrotLover.Is(___pawn))
            {
                __result = GrotRules.FlipBeauty(__result);
            }
        }
    }

    /// <summary>
    /// A grot-loving surgeon works better in a filthy room and worse in a sterile one.
    /// Vanilla multiplies by the bed's SurgerySuccessChanceFactor, which includes the room's cleanliness
    /// factor. Take that room factor back out and put the grot factor in its place.
    /// </summary>
    [HarmonyPatch(typeof(SurgeryOutcomeComp_BedAndRoomQuality), nameof(SurgeryOutcomeComp_BedAndRoomQuality.AffectQuality))]
    public static class Patch_SurgeryBedAndRoomQuality_AffectQuality
    {
        private static RoomStatDef vanillaCleanlinessFactor;

        public static void Postfix(Pawn surgeon, Pawn patient, ref float quality)
        {
            if (!GrotLover.Is(surgeon))
            {
                return;
            }
            Room room = GrotLover.IndoorRoom(patient?.CurrentBed()?.GetRoom());
            if (room == null)
            {
                return;
            }
            vanillaCleanlinessFactor ??= DefDatabase<RoomStatDef>.GetNamedSilentFail("SurgerySuccessChanceCleanlinessFactor");
            if (vanillaCleanlinessFactor == null)
            {
                return;
            }
            float vanilla = room.GetStat(vanillaCleanlinessFactor);
            if (vanilla <= 0f)
            {
                return;
            }
            quality = quality / vanilla * GrotRules.SurgeryFactor(GrotLover.Cleanliness(room));
        }
    }

    /// <summary>A grot-loving patient tended in a filthy room is less likely to get infected, and more in a sterile one.</summary>
    [HarmonyPatch(typeof(HediffComp_Infecter), nameof(HediffComp_Infecter.CompTended))]
    public static class Patch_HediffCompInfecter_CompTended
    {
        public static void Postfix(HediffComp_Infecter __instance, ref float ___infectionChanceFactorFromTendRoom)
        {
            Pawn pawn = __instance.Pawn;
            if (pawn == null || !pawn.Spawned || !GrotLover.Is(pawn))
            {
                return;
            }
            Room room = GrotLover.IndoorRoom(pawn.GetRoom());
            if (room != null)
            {
                ___infectionChanceFactorFromTendRoom = GrotRules.InfectionFactor(GrotLover.Cleanliness(room));
            }
        }
    }
}
