using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HobGob.Events
{
    public enum ChallengeStage
    {
        None,
        Awaiting,
        Fighting,
    }

    /// <summary>
    /// E2 Hobgoblin Challenge state for one map: waiting for the player's answer, then the duel.
    /// The letters hold no state; they call into this component.
    /// </summary>
    public class HobgoblinChallengeComponent : MapComponent
    {
        private const int CheckInterval = 250;

        private ChallengeStage stage = ChallengeStage.None;
        private Pawn challenger;
        private Pawn champion;
        private int deadlineTick = -1;
        private int duelDeadlineTick = -1;

        /// <summary>Challengers that must leave but are downed: they get an exit lord once they can walk.</summary>
        private List<Pawn> leavers = new List<Pawn>();

        public HobgoblinChallengeComponent(Verse.Map map) : base(map)
        {
        }

        public ChallengeStage Stage => stage;
        public Pawn Challenger => challenger;
        public Pawn Champion => champion;
        public bool Active => stage != ChallengeStage.None;

        public void Begin(Pawn newChallenger)
        {
            challenger = newChallenger;
            champion = null;
            stage = ChallengeStage.Awaiting;
            deadlineTick = Find.TickManager.TicksGame + EventRules.ChallengeAnswerTicks;
        }

        /// <summary>The player sends <paramref name="fighter"/> into the duel.</summary>
        public bool Accept(Pawn fighter)
        {
            if (stage != ChallengeStage.Awaiting || !ChallengerCanFight() || fighter == null || !ChallengeUtility.CanBeChampion(fighter))
            {
                Messages.Message("MSSG_ChallengeCannotStart".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            champion = fighter;
            stage = ChallengeStage.Fighting;
            duelDeadlineTick = Find.TickManager.TicksGame + EventRules.DuelMaxTicks;
            RemoveLetters();
            StartDuel(champion, challenger);
            StartDuel(challenger, champion);
            Messages.Message("MSSG_ChallengeDuelStarts".Translate(champion.LabelShort, challenger.LabelShort), champion, MessageTypeDefOf.ThreatSmall);
            return true;
        }

        /// <summary>The player refused, or did not answer in time.</summary>
        public void Refuse(bool timedOut)
        {
            if (stage != ChallengeStage.Awaiting)
            {
                return;
            }
            GiveGroupThought(HobGobDefOf.MSSG_CowardlyChief, null);
            Messages.Message((timedOut ? "MSSG_ChallengeTimedOut" : "MSSG_ChallengeRefused").Translate(), challenger, MessageTypeDefOf.NegativeEvent);
            SendAway(challenger);
            Reset();
        }

        public void AcceptJoin(Pawn joiner)
        {
            if (joiner == null || joiner.Dead || joiner.Faction == Faction.OfPlayer)
            {
                return;
            }
            joiner.GetLord()?.RemovePawn(joiner);
            joiner.SetFaction(Faction.OfPlayer);
            Messages.Message("MSSG_ChallengerJoined".Translate(joiner.LabelShort), joiner, MessageTypeDefOf.PositiveEvent);
        }

        public void DeclineJoin(Pawn joiner)
        {
            if (joiner != null && !joiner.Dead && joiner.Spawned && joiner.Faction != Faction.OfPlayer)
            {
                SendAway(joiner);
            }
        }

        public override void MapComponentTick()
        {
            if (!map.IsHashIntervalTick(CheckInterval))
            {
                return;
            }
            TickLeavers();
            if (stage == ChallengeStage.None)
            {
                return;
            }
            if (stage == ChallengeStage.Awaiting)
            {
                TickAwaiting();
            }
            else
            {
                TickFighting();
            }
        }

        private void TickLeavers()
        {
            for (int i = leavers.Count - 1; i >= 0; i--)
            {
                Pawn pawn = leavers[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != map || pawn.Faction == Faction.OfPlayer)
                {
                    leavers.RemoveAt(i);
                }
                else if (!pawn.Downed)
                {
                    leavers.RemoveAt(i);
                    SendAway(pawn);
                }
            }
        }

        private void TickAwaiting()
        {
            if (!ChallengerCanFight())
            {
                // Died, left, or was downed before the answer: call it off quietly.
                RemoveLetters();
                if (challenger != null && challenger.Spawned && !challenger.Dead)
                {
                    SendAway(challenger);
                }
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
            bool challengerDown = challenger == null || challenger.Dead || challenger.Downed;
            bool championDown = champion == null || champion.Dead || champion.Downed;
            bool championGone = !championDown && (!champion.Spawned || champion.Map != map);
            bool challengerGone = !challengerDown && (!challenger.Spawned || challenger.Map != map);
            bool timedOut = Find.TickManager.TicksGame >= duelDeadlineTick;
            switch (EventRules.ResolveDuel(challengerDown, championDown, championGone, challengerGone, timedOut))
            {
                case DuelOutcome.Win:
                    Win();
                    break;
                case DuelOutcome.Loss:
                    Lose();
                    break;
                case DuelOutcome.Draw:
                    Messages.Message("MSSG_ChallengeDraw".Translate(), challenger, MessageTypeDefOf.NeutralEvent);
                    EndDuelState(champion);
                    SendAway(challenger);
                    Reset();
                    break;
                default:
                    // Both still standing: if something knocked either out of the duel state, put it back.
                    KeepDueling(champion, challenger);
                    KeepDueling(challenger, champion);
                    break;
            }
        }

        private static void KeepDueling(Pawn pawn, Pawn opponent)
        {
            if (!IsDueling(pawn))
            {
                StartDuel(pawn, opponent);
            }
        }

        private void Win()
        {
            Pawn winner = champion;
            Pawn loser = challenger;
            EndDuelState(winner);
            winner.needs?.mood?.thoughts?.memories?.TryGainMemory(HobGobDefOf.MSSG_ChallengeVictor);
            winner.records?.AddTo(RecordDefOf.Kills, EventRules.ChallengeKillCredit);
            Reset();
            if (loser != null && !loser.Dead)
            {
                ChoiceLetter_ChallengerJoins letter = (ChoiceLetter_ChallengerJoins)LetterMaker.MakeLetter(
                    "MSSG_ChallengeWonLabel".Translate(),
                    "MSSG_ChallengeWonText".Translate(winner.LabelShort, loser.LabelShort, EventRules.ChallengeKillCredit),
                    HobGobDefOf.MSSG_ChallengerJoinsLetter,
                    new LookTargets(loser));
                letter.challenger = loser;
                letter.StartTimeout(EventRules.ChallengeAnswerTicks);
                Find.LetterStack.ReceiveLetter(letter);
            }
            else
            {
                Messages.Message("MSSG_ChallengeWonNoJoin".Translate(winner.LabelShort), winner, MessageTypeDefOf.PositiveEvent);
            }
        }

        private void Lose()
        {
            Pawn loser = champion;
            EndDuelState(challenger);
            GiveGroupThought(HobGobDefOf.MSSG_ChampionShamed, loser);
            loser?.needs?.mood?.thoughts?.memories?.TryGainMemory(HobGobDefOf.MSSG_HumiliatedInDuel);
            Messages.Message("MSSG_ChallengeLost".Translate(loser?.LabelShort ?? "?"), challenger, MessageTypeDefOf.NegativeEvent);
            SendAway(challenger);
            Reset();
        }

        private bool ChallengerCanFight()
        {
            return challenger != null && !challenger.Dead && !challenger.Downed && challenger.Spawned && challenger.Map == map;
        }

        private static bool IsDueling(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && pawn.MentalStateDef == HobGobDefOf.MSSG_GoblinDuel;
        }

        private static void StartDuel(Pawn pawn, Pawn opponent)
        {
            pawn.mindState.mentalStateHandler.TryStartMentalState(
                HobGobDefOf.MSSG_GoblinDuel, forced: true, forceWake: true, otherPawn: opponent, transitionSilently: true);
        }

        private static void EndDuelState(Pawn pawn)
        {
            if (IsDueling(pawn))
            {
                pawn.MentalState.RecoverFromState();
            }
        }

        private void GiveGroupThought(ThoughtDef thought, Pawn except)
        {
            foreach (Pawn pawn in ChallengeUtility.GoblinColonists(map))
            {
                if (pawn != except)
                {
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
                }
            }
        }

        /// <summary>Walks off the map once it can. A downed pawn leaves after it gets up.</summary>
        private void SendAway(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Faction == Faction.OfPlayer)
            {
                return;
            }
            EndDuelState(pawn);
            if (pawn.Downed)
            {
                // A lord drops downed pawns, so wait until it is back on its feet.
                if (!leavers.Contains(pawn))
                {
                    leavers.Add(pawn);
                }
                return;
            }
            pawn.GetLord()?.RemovePawn(pawn);
            LordMaker.MakeNewLord(pawn.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Walk), map, new List<Pawn> { pawn });
        }

        private void RemoveLetters()
        {
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                if (letters[i] is ChoiceLetter_HobgoblinChallenge)
                {
                    Find.LetterStack.RemoveLetter(letters[i]);
                }
            }
        }

        private void Reset()
        {
            stage = ChallengeStage.None;
            challenger = null;
            champion = null;
            deadlineTick = -1;
            duelDeadlineTick = -1;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stage, "stage", ChallengeStage.None);
            Scribe_References.Look(ref challenger, "challenger");
            Scribe_References.Look(ref champion, "champion");
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", -1);
            Scribe_Values.Look(ref duelDeadlineTick, "duelDeadlineTick", -1);
            Scribe_Collections.Look(ref leavers, "leavers", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (stage == ChallengeStage.Fighting && duelDeadlineTick < 0)
                {
                    // Save from before the duel deadline existed.
                    duelDeadlineTick = Find.TickManager.TicksGame + EventRules.DuelMaxTicks;
                }
                leavers ??= new List<Pawn>();
                leavers.RemoveAll(p => p == null);
            }
        }
    }
}
