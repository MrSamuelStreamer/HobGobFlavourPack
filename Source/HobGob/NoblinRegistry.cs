using System.Collections.Generic;
using Verse;

namespace HobGob
{
    /// <summary>
    /// Pawns whose work is currently managed. Read by the Harmony patches, so lookups must be O(1).
    /// Cleared when a game starts or loads (see <see cref="GoblinEvolutionComponent"/>).
    /// </summary>
    public static class NoblinRegistry
    {
        private static readonly HashSet<Pawn> Managed = new HashSet<Pawn>();

        public static bool IsManaged(Pawn pawn) => pawn != null && Managed.Contains(pawn);

        public static void Set(Pawn pawn, bool managed)
        {
            bool changed = managed ? Managed.Add(pawn) : Managed.Remove(pawn);
            if (changed)
            {
                // Door access depends on membership (noblin-proof doors), so cached reachability is stale.
                pawn.MapHeld?.reachability.ClearCacheFor(pawn);
            }
        }

        public static void Clear() => Managed.Clear();

        /// <summary>A pawn whose work the mod manages: a working Noblin. Callers check it is a free colonist.</summary>
        public static bool ShouldManage(Pawn pawn) =>
            pawn.workSettings != null && pawn.workSettings.EverWork && IsNoblin(pawn);

        public static bool IsNoblin(Pawn pawn) =>
            pawn.genes != null && pawn.genes.HasActiveGene(HobGobDefOf.MSSG_NoblinInstinct);
    }
}
