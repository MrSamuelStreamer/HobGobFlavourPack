using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>Hands out rummage jobs for pits marked "Rummage through stash". Noblins never rummage.</summary>
    public class WorkGiver_RummagePit : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Designation designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(HobGobDefOf.MSSG_RummageStash))
            {
                yield return designation.target.Thing;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return NoblinRegistry.IsNoblin(pawn)
                || !pawn.Map.designationManager.AnySpawnedDesignationOfDef(HobGobDefOf.MSSG_RummageStash);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (NoblinRegistry.IsNoblin(pawn) || t.Map.designationManager.DesignationOn(t, HobGobDefOf.MSSG_RummageStash) == null)
            {
                return false;
            }
            CompGoblinPit pit = t.TryGetComp<CompGoblinPit>();
            return pit != null && pit.Stash.Count > 0 && pawn.CanReserve(t, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(HobGobDefOf.MSSG_RummagePit, t);
        }
    }

    /// <summary>Work beside the pit for 2 hours, then drop the whole stash.</summary>
    public class JobDriver_RummagePit : JobDriver
    {
        private const TargetIndex PitInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(PitInd), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(PitInd);
            this.FailOnThingMissingDesignation(PitInd, HobGobDefOf.MSSG_RummageStash);
            yield return Toils_Goto.GotoThing(PitInd, PathEndMode.Touch);
            yield return Toils_General.WaitWith(PitInd, EventRules.RummageTicks, useProgressBar: true, face: PitInd);
            Toil empty = ToilMaker.MakeToil("EmptyStash");
            empty.initAction = () =>
            {
                Thing pit = job.GetTarget(PitInd).Thing;
                pit.TryGetComp<CompGoblinPit>()?.DropStash(pit.Map);
                Designation designation = pit.Map.designationManager.DesignationOn(pit, HobGobDefOf.MSSG_RummageStash);
                if (designation != null)
                {
                    pit.Map.designationManager.RemoveDesignation(designation);
                }
            };
            empty.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return empty;
        }
    }
}
