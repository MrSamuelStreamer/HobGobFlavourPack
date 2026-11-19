using RimWorld;
using Verse;

namespace HobGob.Grot
{
    /// <summary>Game-side helpers for the grot lover gene.</summary>
    public static class GrotLover
    {
        public static bool Is(Pawn pawn) =>
            pawn?.genes != null && pawn.genes.HasActiveGene(HobGobDefOf.MSSG_GrotLover);

        /// <summary>
        /// The room whose cleanliness counts, or null outdoors. Vanilla uses a flat "roomless" value there,
        /// so the grot rules leave outdoor cases alone.
        /// </summary>
        public static Room IndoorRoom(Room room) =>
            room != null && room.ProperRoom && !room.PsychologicallyOutdoors ? room : null;

        public static float Cleanliness(Room room) => room.GetStat(RoomStatDefOf.Cleanliness);
    }

    /// <summary>A grot lover's feelings about the room their bed is in (bedroom or barracks).</summary>
    public class ThoughtWorker_GrotRoom : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!GrotLover.Is(p))
            {
                return ThoughtState.Inactive;
            }
            Room room = GrotLover.IndoorRoom(p.ownership?.OwnedBed?.GetRoom());
            if (room == null)
            {
                return ThoughtState.Inactive;
            }
            int stage = GrotRules.RoomThoughtStage(GrotLover.Cleanliness(room));
            return stage == GrotRules.NoRoomThought ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(stage);
        }
    }
}
