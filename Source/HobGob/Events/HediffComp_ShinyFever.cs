using Verse;
using Verse.AI;

namespace HobGob.Events
{
    public class HediffCompProperties_ShinyFever : HediffCompProperties
    {
        public HediffCompProperties_ShinyFever()
        {
            compClass = typeof(HediffComp_ShinyFever);
        }
    }

    /// <summary>
    /// While a Noblin has Shiny Fever it keeps trying to steal one shiny for the pit.
    /// The steal job removes the hediff once the shiny is in the stash.
    /// </summary>
    public class HediffComp_ShinyFever : HediffComp
    {
        private int ticksSinceCheck;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            // Pawn hediffs tick on intervals in 1.7, so count elapsed ticks instead of matching a tick number.
            ticksSinceCheck += delta;
            if (ticksSinceCheck < EventRules.ShinyFeverCheckTicks)
            {
                return;
            }
            ticksSinceCheck = 0;
            TryStartStealing(Pawn);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksSinceCheck, "ticksSinceCheck");
        }

        private static void TryStartStealing(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Downed || pawn.Drafted || pawn.InMentalState || !NoblinRegistry.IsManaged(pawn))
            {
                return;
            }
            if (pawn.CurJobDef == HobGobDefOf.MSSG_StealShiny || pawn.carryTracker.CarriedThing != null)
            {
                return;
            }
            Thing target = ShinyFinder.BestTargetFor(pawn);
            if (target == null)
            {
                return;
            }
            CompGoblinPit pit = ShinyFinder.NearestPitFor(pawn);
            if (pit == null)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HobGobDefOf.MSSG_StealShiny, target, pit.parent);
            job.count = ShinyFinder.CarryCountFor(pawn, target);
            job.ignoreForbidden = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true);
        }
    }
}
