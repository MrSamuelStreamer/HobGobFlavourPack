using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HobGob.Slap
{
    /// <summary>
    /// Runs the turns. In the main stage, one participant at a time is the thumper. The duty think tree
    /// (JobGiver_SlapTurn) gives that pawn the thump job. Each eligible participant gets exactly one turn;
    /// StageEndTrigger_AllThumped ends the stage when nobody is left.
    /// </summary>
    public class RitualBehaviorWorker_SlapSense : RitualBehaviorWorker
    {
        public const string TargetRoleId = "target";

        /// <summary>A thumper who has not thumped by now loses the turn (for example, they cannot reach).</summary>
        private const int TurnTimeoutTicks = 600;

        private Pawn currentThumper;
        private DutyDef thumperPreviousDuty;
        private List<Pawn> hadTurn = new List<Pawn>();
        private int turnStartedTick = -1;
        private int turnEndedTick = -1;

        public RitualBehaviorWorker_SlapSense()
        {
        }

        public RitualBehaviorWorker_SlapSense(RitualBehaviorDef def) : base(def)
        {
        }

        public Pawn CurrentThumper => currentThumper;

        public static RitualBehaviorWorker_SlapSense For(Pawn pawn, out LordJob_Ritual ritual)
        {
            ritual = pawn.GetLord()?.LordJob as LordJob_Ritual;
            return ritual?.Ritual?.behavior as RitualBehaviorWorker_SlapSense;
        }

        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            // Turn state lives on this worker, one per precept, so only one run at a time.
            bool running = Find.Maps.Any(m => m.lordManager.lords.Any(l => l.LordJob is LordJob_Ritual r && r.Ritual == ritual));
            if (running)
            {
                return "MSSG_SlapAlreadyOn".Translate();
            }
            return base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
        }

        public void EndTurn(Pawn pawn)
        {
            if (pawn != currentThumper)
            {
                return;
            }
            currentThumper = null;
            turnEndedTick = Find.TickManager.TicksGame;
            RestoreDuty(pawn);
        }

        /// <summary>
        /// Puts the thumper on the slap duty for their turn only. The duty is swapped on the pawn, not set as the
        /// stage default, because mods such as Please Use The Pews overwrite every ritual stage's default duty.
        /// </summary>
        private void GiveSlapDuty(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState.duty;
            if (duty == null)
            {
                return;
            }
            thumperPreviousDuty = duty.def;
            duty.def = HobGobDefOf.MSSG_SlapCircle;
        }

        private void RestoreDuty(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState?.duty;
            if (duty != null && duty.def == HobGobDefOf.MSSG_SlapCircle)
            {
                duty.def = thumperPreviousDuty ?? DutyDefOf.Spectate;
            }
            thumperPreviousDuty = null;
        }

        /// <summary>True once every eligible participant has had a turn, or the target can no longer be thumped.</summary>
        public bool AllThumped(LordJob_Ritual ritual)
        {
            Pawn target = ritual.PawnWithRole(TargetRoleId);
            bool targetGone = target == null || !target.Spawned || target.Downed || target.Dead;
            int waiting = targetGone ? 0 : ritual.assignments.SpectatorsForReading.Count(p => !hadTurn.Contains(p) && CanThump(p, ritual, target));
            return SlapRules.TurnsDone(waiting, currentThumper != null, Find.TickManager.TicksGame - turnEndedTick);
        }

        public override void Tick(LordJob_Ritual ritual)
        {
            base.Tick(ritual);
            if (ritual.StageIndex == 0)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (currentThumper != null)
            {
                if (now - turnStartedTick < TurnTimeoutTicks && StillInRitual(currentThumper, ritual))
                {
                    return;
                }
                EndTurn(currentThumper);
            }
            if (now - turnEndedTick < SlapRules.GapBetweenTurnsTicks)
            {
                return;
            }
            Pawn target = ritual.PawnWithRole(TargetRoleId);
            if (target == null || !target.Spawned || target.Downed)
            {
                return;
            }
            Pawn next = ritual.assignments.SpectatorsForReading
                .Where(p => !hadTurn.Contains(p) && CanThump(p, ritual, target))
                .RandomElementWithFallback();
            if (next == null)
            {
                return;
            }
            hadTurn.Add(next);
            currentThumper = next;
            turnStartedTick = now;
            GiveSlapDuty(next);
            if (next.CurJob != null)
            {
                next.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        /// <summary>Pacifists watch and still count for quality, but never throw a punch.</summary>
        private static bool CanThump(Pawn pawn, LordJob_Ritual ritual, Pawn target)
        {
            return StillInRitual(pawn, ritual)
                && !pawn.Downed
                && !pawn.WorkTagIsDisabled(WorkTags.Violent)
                && pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly);
        }

        private static bool StillInRitual(Pawn pawn, LordJob_Ritual ritual)
        {
            return pawn.Spawned && !pawn.Dead && ritual.lord.ownedPawns.Contains(pawn);
        }

        private void ResetTurns()
        {
            if (currentThumper != null)
            {
                RestoreDuty(currentThumper);
            }
            currentThumper = null;
            hadTurn.Clear();
            turnStartedTick = -1;
            turnEndedTick = -1;
        }

        public override void Cleanup(LordJob_Ritual ritual)
        {
            ResetTurns();
            Pawn target = ritual.PawnWithRole(TargetRoleId);
            if (target != null && target.IsPrisonerOfColony)
            {
                target.guest.WaitInsteadOfEscapingFor(2500);
            }
        }

        public override void PostCleanup(LordJob_Ritual ritual)
        {
            Pawn target = ritual.PawnWithRole(TargetRoleId);
            if (target == null || !target.IsPrisonerOfColony)
            {
                return;
            }
            Pawn warden = ritual.assignments.SpectatorsForReading.FirstOrDefault(p => p.Spawned && !p.Downed && p.IsFreeColonist);
            if (warden != null)
            {
                WorkGiver_Warden_TakeToBed.TryTakePrisonerToBed(target, warden);
            }
            target.guest.WaitInsteadOfEscapingFor(1250);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref currentThumper, "currentThumper");
            Scribe_Defs.Look(ref thumperPreviousDuty, "thumperPreviousDuty");
            Scribe_Collections.Look(ref hadTurn, "hadTurn", LookMode.Reference);
            Scribe_Values.Look(ref turnStartedTick, "turnStartedTick", -1);
            Scribe_Values.Look(ref turnEndedTick, "turnEndedTick", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                hadTurn ??= new List<Pawn>();
                hadTurn.RemoveAll(p => p == null);
            }
        }
    }

    /// <summary>Ends the thumping stage once everyone who can thump has had their go.</summary>
    public class StageEndTrigger_AllThumped : StageEndTrigger
    {
        public override Trigger MakeTrigger(LordJob_Ritual ritual, TargetInfo spot, IEnumerable<TargetInfo> foci, RitualStage stage)
        {
            return new Trigger_TickCondition(() => (ritual.Ritual?.behavior as RitualBehaviorWorker_SlapSense)?.AllThumped(ritual) ?? true);
        }
    }
}
