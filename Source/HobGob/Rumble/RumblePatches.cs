using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HobGob.Rumble
{
    /// <summary>
    /// Nobby Nobby Rumble deaths give no "colonist died" or "witnessed death" thoughts.
    /// Family and bloodlust thoughts stay. The victim is still a registered fighter here:
    /// thoughts fire inside Pawn.Kill, before the component's next check drops it.
    /// </summary>
    [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_ForHumanlike")]
    public static class Patch_RumbleDeathThoughts
    {
        public static void Postfix(Pawn victim, PawnDiedOrDownedThoughtsKind thoughtsKind, List<IndividualThoughtToAdd> outIndividualThoughts)
        {
            if (thoughtsKind != PawnDiedOrDownedThoughtsKind.Died || !NobbyRumbleComponent.IsFighter(victim))
            {
                return;
            }
            outIndividualThoughts.RemoveAll(t =>
            {
                ThoughtDef def = t.thought?.def;
                return def == ThoughtDefOf.KnowColonistDied || def == ThoughtDefOf.WitnessedDeathAlly || def == ThoughtDefOf.WitnessedDeathNonAlly;
            });
        }
    }

    /// <summary>No "Death: X" letter per fighter. The rumble sends one summary letter at the end instead.</summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.NotifyPlayerOfKilled))]
    public static class Patch_RumbleNoDeathLetter
    {
        public static bool Prefix(Pawn ___pawn) => !NobbyRumbleComponent.IsFighter(___pawn);
    }

    /// <summary>
    /// No funeral obligation (and its letter) per dead fighter. Runs inside Pawn.Kill, while the victim is still a fighter.
    /// Patches the trigger, not Precept_Ritual.AddObligation: other mods prefix AddObligation and send their own letters there.
    /// </summary>
    [HarmonyPatch(typeof(RitualObligationTrigger_MemberDied), nameof(RitualObligationTrigger_MemberDied.Notify_MemberDied))]
    public static class Patch_RumbleNoFuneral
    {
        public static bool Prefix(Pawn p) => !NobbyRumbleComponent.IsFighter(p);
    }

    /// <summary>Doctors do not run into the rumble to rescue fighters. A forced (player-ordered) rescue still works.</summary>
    [HarmonyPatch(typeof(WorkGiver_RescueDowned), nameof(WorkGiver_RescueDowned.HasJobOnThing))]
    public static class Patch_RumbleNoRescue
    {
        public static void Postfix(Thing t, bool forced, ref bool __result)
        {
            if (__result && !forced && t is Pawn pawn && NobbyRumbleComponent.IsFighter(pawn))
            {
                __result = false;
            }
        }
    }

    /// <summary>Doctors do not tend fighters mid-rumble. A forced (player-ordered) tend still works.</summary>
    [HarmonyPatch(typeof(WorkGiver_Tend), nameof(WorkGiver_Tend.HasJobOnThing))]
    public static class Patch_RumbleNoTend
    {
        public static void Postfix(Thing t, bool forced, ref bool __result)
        {
            if (__result && !forced && t is Pawn pawn && NobbyRumbleComponent.IsFighter(pawn))
            {
                __result = false;
            }
        }
    }
}
