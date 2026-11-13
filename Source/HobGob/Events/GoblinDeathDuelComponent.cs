using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    public enum DeathDuelStage
    {
        None,
        Awaiting,
        Fighting,
    }

    /// <summary>
    /// Death duel state for one map: waiting for the player's answer, then the fight.
    /// The letter holds no state; it calls into this component.
    /// </summary>
    public class GoblinDeathDuelComponent : MapComponent
    {
        private const int CheckInterval = 60;

        private DeathDuelStage stage = DeathDuelStage.None;
        private Pawn fighterA;
        private Pawn fighterB;
        private int deadlineTick = -1;
        private int duelStartTick = -1;

        public GoblinDeathDuelComponent(Verse.Map map) : base(map)
        {
        }

        public DeathDuelStage Stage => stage;
        public bool Active => stage != DeathDuelStage.None;

        public bool CanAccept =>
            stage == DeathDuelStage.Awaiting && Waiting(fighterA) && Waiting(fighterB) && CanStillSwing(fighterA) && CanStillSwing(fighterB);

        public void Begin(Pawn a, Pawn b)
        {
            fighterA = a;
            fighterB = b;
            stage = DeathDuelStage.Awaiting;
            deadlineTick = Find.TickManager.TicksGame + EventRules.ChallengeAnswerTicks;
            duelStartTick = -1;
        }

        /// <summary>The player lets the two fight to the death.</summary>
        public bool Accept()
        {
            if (!CanAccept)
            {
                Messages.Message("MSSG_DeathDuelCannotStart".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            bool aStarted = StartDuel(fighterA, fighterB);
            bool bStarted = StartDuel(fighterB, fighterA);
            if (!aStarted || !bStarted)
            {
                EndDuel(fighterA);
                EndDuel(fighterB);
                Messages.Message("MSSG_DeathDuelCannotStart".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            stage = DeathDuelStage.Fighting;
            duelStartTick = Find.TickManager.TicksGame;
            RemoveLetters();
            Messages.Message(
                "MSSG_DeathDuelStarts".Translate(fighterA.LabelShort, fighterB.LabelShort),
                new LookTargets(new List<Pawn> { fighterA, fighterB }),
                MessageTypeDefOf.ThreatSmall);
            return true;
        }

        /// <summary>The player refused, or did not answer in time. Both fighters sulk.</summary>
        public void Refuse(bool timedOut)
        {
            if (stage != DeathDuelStage.Awaiting)
            {
                return;
            }
            fighterA?.needs?.mood?.thoughts?.memories?.TryGainMemory(HobGobDefOf.MSSG_DuelForbidden);
            fighterB?.needs?.mood?.thoughts?.memories?.TryGainMemory(HobGobDefOf.MSSG_DuelForbidden);
            string key = timedOut ? "MSSG_DeathDuelTimedOut" : "MSSG_DeathDuelRefused";
            Messages.Message(
                key.Translate(NameOf(fighterA), NameOf(fighterB)),
                new LookTargets(new List<Pawn> { fighterA, fighterB }),
                MessageTypeDefOf.NegativeEvent);
            Reset();
        }

        public override void MapComponentTick()
        {
            if (stage == DeathDuelStage.None || !map.IsHashIntervalTick(CheckInterval))
            {
                return;
            }
            if (stage == DeathDuelStage.Awaiting)
            {
                TickAwaiting();
            }
            else
            {
                TickFighting();
            }
        }

        private void TickAwaiting()
        {
            if (!Waiting(fighterA) || !Waiting(fighterB))
            {
                RemoveLetters();
                Reset();
                return;
            }
            if (Find.TickManager.TicksGame >= deadlineTick)
            {
                RemoveLetters();
                Refuse(timedOut: true);
            }
        }

        private void TickFighting()
        {
            bool aDead = fighterA != null && fighterA.Dead;
            bool bDead = fighterB != null && fighterB.Dead;
            bool aGone = fighterA == null || (!aDead && (!fighterA.Spawned || fighterA.Map != map));
            bool bGone = fighterB == null || (!bDead && (!fighterB.Spawned || fighterB.Map != map));
            bool aDowned = !aDead && !aGone && fighterA.Downed;
            bool bDowned = !bDead && !bGone && fighterB.Downed;
            bool cannotFinish = !CanFinish(fighterA, aDead, aGone, aDowned) && !CanFinish(fighterB, bDead, bGone, bDowned);
            bool timedOut = duelStartTick >= 0 && Find.TickManager.TicksGame >= duelStartTick + EventRules.DeathDuelMaxTicks;
            switch (EventRules.ResolveDeathDuel(aDead, bDead, aGone, bGone, aDowned, bDowned, timedOut, cannotFinish))
            {
                case DeathDuelOutcome.AWins:
                    Win(fighterA, fighterB);
                    break;
                case DeathDuelOutcome.BWins:
                    Win(fighterB, fighterA);
                    break;
                case DeathDuelOutcome.Draw:
                    Draw();
                    break;
                default:
                    KeepDueling(fighterA, fighterB);
                    KeepDueling(fighterB, fighterA);
                    break;
            }
        }

        private void Win(Pawn winner, Pawn loser)
        {
            if (winner == null)
            {
                Draw();
                return;
            }
            int kills = EventRules.InheritedKills(loser?.records?.GetAsInt(RecordDefOf.Kills) ?? 0);
            if (!winner.Dead)
            {
                EndDuel(winner);
                if (kills > 0)
                {
                    winner.records?.AddTo(RecordDefOf.Kills, kills);
                }
            }
            LookTargets targets = winner != null && winner.Spawned ? new LookTargets(winner) : LookTargets.Invalid;
            Find.LetterStack.ReceiveLetter(
                "MSSG_DeathDuelWonLabel".Translate(),
                "MSSG_DeathDuelWonText".Translate(NameOf(winner), NameOf(loser), kills),
                LetterDefOf.PositiveEvent,
                targets);
            Reset();
        }

        private void Draw()
        {
            EndDuel(fighterA);
            EndDuel(fighterB);
            Messages.Message("MSSG_DeathDuelDraw".Translate(), MessageTypeDefOf.NeutralEvent);
            Reset();
        }

        private bool Waiting(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && !pawn.Downed && pawn.Spawned && pawn.Map == map;
        }

        private static bool CanStillSwing(Pawn pawn)
        {
            return pawn != null && !pawn.InMentalState && !pawn.WorkTagIsDisabled(WorkTags.Violent);
        }

        private static bool CanFinish(Pawn pawn, bool dead, bool gone, bool downed)
        {
            return pawn != null && !dead && !gone && !downed && !pawn.WorkTagIsDisabled(WorkTags.Violent);
        }

        private static bool StartDuel(Pawn pawn, Pawn opponent)
        {
            return pawn.mindState.mentalStateHandler.TryStartMentalState(
                HobGobDefOf.MSSG_GoblinDeathDuel, forced: true, forceWake: true, otherPawn: opponent, transitionSilently: true);
        }

        private static void KeepDueling(Pawn pawn, Pawn opponent)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.MentalStateDef == HobGobDefOf.MSSG_GoblinDeathDuel)
            {
                return;
            }
            StartDuel(pawn, opponent);
        }

        private static void EndDuel(Pawn pawn)
        {
            if (pawn != null && !pawn.Dead && pawn.MentalStateDef == HobGobDefOf.MSSG_GoblinDeathDuel)
            {
                pawn.MentalState.RecoverFromState();
            }
        }

        private static string NameOf(Pawn pawn)
        {
            return pawn?.LabelShort ?? "?";
        }

        private void RemoveLetters()
        {
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                if (letters[i] is ChoiceLetter_GoblinDeathDuel)
                {
                    Find.LetterStack.RemoveLetter(letters[i]);
                }
            }
        }

        private void Reset()
        {
            stage = DeathDuelStage.None;
            fighterA = null;
            fighterB = null;
            deadlineTick = -1;
            duelStartTick = -1;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stage, "stage", DeathDuelStage.None);
            Scribe_References.Look(ref fighterA, "fighterA");
            Scribe_References.Look(ref fighterB, "fighterB");
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", -1);
            Scribe_Values.Look(ref duelStartTick, "duelStartTick", -1);
        }
    }
}
