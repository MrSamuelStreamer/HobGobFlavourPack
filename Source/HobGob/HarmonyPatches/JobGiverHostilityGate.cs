using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.HarmonyPatches
{
    /// <summary>
    /// Every colonist on "Attack" hostility runs a full attack-target search every 30 ticks. The search only
    /// ever returns targets inside the pawn's attack range, which is never more than 20 cells. When no potential
    /// target is within that range plus a margin, the search cannot find anything, so skip it.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse), "TryGetAttackNearbyEnemyJob")]
    public static class Patch_ConfigurableHostilityResponse_AttackGate
    {
        /// <summary>Widest range the vanilla job giver searches (20) plus one cell of margin.</summary>
        private const float GateRadius = 21f;

        /// <returns>False when no potential target is near, leaving the result as no job; true to run the original.</returns>
        public static bool Prefix(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null || !pawn.Spawned)
            {
                return true;
            }

            var targets = map.attackTargetsCache.GetPotentialTargetsFor(pawn);
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Thing.Position.InHorDistOf(pawn.Position, GateRadius))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
