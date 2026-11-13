using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HobGob
{
    public class HobGobMod : Mod
    {
        public static HobGobSettings Settings { get; private set; }

        public HobGobMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HobGobSettings>();
            // Pass the assembly explicitly: parameterless PatchAll() patches nothing on RimWorld 1.7.
            Harmony harmony = new Harmony("MrSamuelStreamer.GoblinFlavourPack");
            harmony.PatchAll(typeof(HobGobMod).Assembly);
            Log.Message($"[Goblin Flavour Pack] {harmony.GetPatchedMethods().Count()} Harmony patches applied.");
        }

        public override string SettingsCategory() => "MSSG_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.Label("MSSG_SettingsSeasonalHeader".Translate());
            list.Label("MSSG_SettingsSeasonalChance".Translate(Settings.seasonalKillChance.ToStringPercent()),
                tooltip: "MSSG_SettingsSeasonalChanceTip".Translate());
            Settings.seasonalKillChance = list.Slider(Settings.seasonalKillChance, 0f, 1f);
            list.Label("MSSG_SettingsSeasonalKills".Translate(Settings.seasonalKills.min, Settings.seasonalKills.max),
                tooltip: "MSSG_SettingsSeasonalKillsTip".Translate());
            Widgets.IntRange(list.GetRect(32f), "MSSG_SeasonalKills".GetHashCode(), ref Settings.seasonalKills,
                HobGobSettings.MinSeasonalKills, HobGobSettings.MaxSeasonalKills);
            list.End();
        }
    }

    public class HobGobSettings : ModSettings
    {
        public const int MinSeasonalKills = 1;
        public const int MaxSeasonalKills = 10;

        /// <summary>Chance (0-1) per goblin at each season start to gain free kills.</summary>
        public float seasonalKillChance = 0.5f;

        /// <summary>How many free kills a successful roll grants, picked at random from this range.</summary>
        public IntRange seasonalKills = new IntRange(1, 3);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref seasonalKillChance, "seasonalKillChance", 0.5f);
            Scribe_Values.Look(ref seasonalKills, "seasonalKills", new IntRange(1, 3));
        }
    }

    [RimWorld.DefOf]
    public static class HobGobDefOf
    {
        public static GeneDef MSSG_NoblinInstinct;
        public static XenotypeDef MSSG_Noblin;
        public static PawnKindDef MSSG_Pitborn;
        public static ThingDef MSSG_GoblinHole;
        public static HediffDef MSSG_PitFresh;
        public static HediffDef MSSG_PitTorpor;
        public static XenotypeDef MSSG_Hobnobgoblin;
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
        public static XenotypeDef MSSG_Hobgoblin;
        public static MentalStateDef MSSG_GoblinDuel;
        public static ThoughtDef MSSG_ChallengeVictor;
        public static ThoughtDef MSSG_ChampionShamed;
        public static ThoughtDef MSSG_HumiliatedInDuel;
        public static ThoughtDef MSSG_CowardlyChief;
        public static LetterDef MSSG_HobgoblinChallengeLetter;
        public static LetterDef MSSG_ChallengerJoinsLetter;
        public static Census.GoblinCensusTierDef MSSG_Census_Shed;
        public static RecipeDef MSSG_HarvestGoblinPart;
        public static HediffDef MSSG_BadlyAttached;
        public static XenotypeDef MSSG_Snoblin;
        public static HediffDef MSSG_RumbleFrenzy;
        public static MentalStateDef MSSG_NobbyRumble;

        static HobGobDefOf() => RimWorld.DefOfHelper.EnsureInitializedInCtor(typeof(HobGobDefOf));
    }
}
