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
            if (managed)
            {
                Managed.Add(pawn);
            }
            else
            {
                Managed.Remove(pawn);
            }
        }

        public static void Clear() => Managed.Clear();

        public static bool IsNoblin(Pawn pawn) =>
            pawn.genes != null && pawn.genes.HasActiveGene(HobGobDefOf.MSSG_NoblinInstinct);
    }
}
