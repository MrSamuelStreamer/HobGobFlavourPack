using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HobGob
{
    public class HobGobMod : Mod
    {
        public HobGobMod(ModContentPack content) : base(content)
        {
            // Pass the assembly explicitly: parameterless PatchAll() patches nothing on RimWorld 1.7.
            Harmony harmony = new Harmony("MrSamuelStreamer.GoblinFlavourPack");
            harmony.PatchAll(typeof(HobGobMod).Assembly);
            Log.Message($"[Goblin Flavour Pack] {harmony.GetPatchedMethods().Count()} Harmony patches applied.");
        }
    }

    [RimWorld.DefOf]
    public static class HobGobDefOf
    {
        public static GeneDef MSSG_NoblinInstinct;
        public static XenotypeDef MSSG_Noblin;
        public static ThingDef MSSG_GoblinHole;
        public static HediffDef MSSG_PitFresh;
        public static HediffDef MSSG_OnStrike;
        public static ThoughtDef MSSG_StrikeWon;
        public static ThoughtDef MSSG_StrikeBroken;
        public static Events.GoblinShinyDef MSSG_Shinies;
        public static HediffDef MSSG_ShinyFever;
        public static Verse.JobDef MSSG_StealShiny;
        public static Verse.JobDef MSSG_RummagePit;
        public static DesignationDef MSSG_RummageStash;
        public static MentalStateDef MSSG_NoblinStampede;
        public static ThoughtDef MSSG_Winded;

        static HobGobDefOf() => RimWorld.DefOfHelper.EnsureInitializedInCtor(typeof(HobGobDefOf));
    }
}
