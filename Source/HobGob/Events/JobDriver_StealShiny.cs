using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>Shiny Fever: carry a shiny (A) to a goblin pit (B) and hide it in the stash.</summary>
    public class JobDriver_StealShiny : JobDriver
    {
        private const TargetIndex ShinyInd = TargetIndex.A;
        private const TargetIndex PitInd = TargetIndex.B;

        private Thing Pit => job.GetTarget(PitInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(ShinyInd), job, 1, job.count, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(PitInd);
            yield return Toils_Goto.GotoThing(ShinyInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedOrNull(ShinyInd).FailOnSomeonePhysicallyInteracting(ShinyInd);
            yield return Toils_Haul.StartCarryThing(ShinyInd, subtractNumTakenFromJobCount: true);
            yield return Toils_Goto.GotoThing(PitInd, PathEndMode.Touch);
            Toil hide = ToilMaker.MakeToil("HideInPit");
            hide.initAction = () =>
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                CompGoblinPit pit = Pit?.TryGetComp<CompGoblinPit>();
                if (carried == null || pit == null || !pit.Deposit(pawn, carried))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                Hediff fever = pawn.health.hediffSet.GetFirstHediffOfDef(HobGobDefOf.MSSG_ShinyFever);
                if (fever != null)
                {
                    pawn.health.RemoveHediff(fever);
                }
            };
            hide.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return hide;
        }
    }
}
