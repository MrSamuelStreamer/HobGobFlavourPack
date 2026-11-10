using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>A Noblin fresh out of the pit and short of food.</summary>
    public class ThoughtWorker_PitStarving : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            Need_Food food = p.needs?.food;
            if (food == null)
            {
                return ThoughtState.Inactive;
            }
            bool fresh = p.health.hediffSet.HasHediff(HobGobDefOf.MSSG_PitFresh);
            return EventRules.IsPitStarving(fresh, food.CurLevelPercentage)
                ? ThoughtState.ActiveDefault
                : ThoughtState.Inactive;
        }
    }
}
