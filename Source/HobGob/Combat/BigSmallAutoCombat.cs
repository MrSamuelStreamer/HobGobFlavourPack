using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace HobGob.Combat
{
    /// <summary>
    /// Soft link to the Auto-Combat toggle in Big and Small - Framework (RedMattis.BetterPrerequisites).
    /// Reflection only, so the mod builds and runs without it. Per-pawn state is
    /// <c>BigAndSmall.DraftedActionHolder.GetData(pawn)</c>, a <c>DraftedActionData</c> with a public <c>hunt</c> flag.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class BigSmallAutoCombat
    {
        /// <summary>groupKey of the Big and Small Auto-Combat gizmo (BigAndSmall.DraftGizmos.AddHuntGizmo).</summary>
        public const int HuntGizmoGroupKey = 6173613;

        private static readonly Func<Pawn, object> GetData;
        private static readonly AccessTools.FieldRef<object, bool> Hunt;
        private static readonly MethodInfo ToggleHuntMode;
        private static bool failed;

        static BigSmallAutoCombat()
        {
            Type holder = AccessTools.TypeByName("BigAndSmall.DraftedActionHolder");
            Type data = AccessTools.TypeByName("BigAndSmall.DraftedActionData");
            if (holder == null || data == null)
            {
                return;
            }
            try
            {
                MethodInfo getData = AccessTools.Method(holder, "GetData", new[] { typeof(Pawn) });
                ToggleHuntMode = AccessTools.Method(data, "ToggleHuntMode");
                if (getData == null || !getData.IsStatic || ToggleHuntMode == null)
                {
                    Log.Warning("[HobGob] Big and Small found, but its Auto-Combat API has changed. Noblins will not auto-combat.");
                    return;
                }
                GetData = (Func<Pawn, object>)Delegate.CreateDelegate(typeof(Func<Pawn, object>), getData);
                Hunt = AccessTools.FieldRefAccess<bool>(data, "hunt");
            }
            catch (Exception e)
            {
                Log.Warning($"[HobGob] Could not link to Big and Small Auto-Combat: {e.Message}");
                GetData = null;
            }
        }

        public static bool Available => GetData != null && Hunt != null && !failed;

        /// <summary>Turns Auto-Combat on for a drafted pawn. Does nothing when it is already on or Big and Small is absent.</summary>
        public static void ForceHunt(Pawn pawn)
        {
            if (!Available)
            {
                return;
            }
            try
            {
                object data = GetData(pawn);
                if (data != null && !Hunt(data))
                {
                    // ToggleHuntMode also ends the current job, so the new mode applies at once.
                    ToggleHuntMode.Invoke(data, null);
                }
            }
            catch (Exception e)
            {
                failed = true;
                Log.Warning($"[HobGob] Big and Small Auto-Combat call failed; turning the link off: {e}");
            }
        }
    }
}
