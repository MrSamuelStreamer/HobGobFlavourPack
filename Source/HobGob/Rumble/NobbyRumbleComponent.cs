using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Rumble
{
    /// <summary>
    /// The Nobby Nobby Rumble on one map: every free adult noblin fights to the death.
    /// A fighter with enough kills to evolve leaves as a winner. The last one left becomes a Snoblin.
    /// Started by <see cref="RitualOutcomeEffectWorker_NobbyRumble"/>.
    /// </summary>
    public class NobbyRumbleComponent : MapComponent
    {
        private const int CheckInterval = 60;
        private const int MaxReachChecks = 8;

        /// <summary>Fighters on every map. Read by Harmony patches, so lookups are O(1).</summary>
        private static readonly SnapshotSet<Pawn> AllFighters = new SnapshotSet<Pawn>();

        private static readonly List<Pawn> Buffer = new List<Pawn>();

        private bool active;
        private bool startPending;
        private int startTick;
        private int startCount;
        private int deaths;
        private HashSet<Pawn> fighters = new HashSet<Pawn>();
        private List<string> winners = new List<string>();

        public NobbyRumbleComponent(Map map) : base(map)
        {
        }

        public bool Active => active || startPending;

        public static NobbyRumbleComponent For(Map map) => map?.GetComponent<NobbyRumbleComponent>();

        public static bool IsFighter(Pawn pawn) => pawn != null && AllFighters.Contains(pawn);

        /// <summary>Called when a game starts or loads, before saved maps re-register their fighters.</summary>
        public static void ClearFighters() => AllFighters.Clear();

        public static bool Eligible(Pawn pawn) =>
            pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.IsColonist
            && pawn.DevelopmentalStage.Adult() && NoblinRegistry.IsNoblin(pawn);

        public static int CountEligible(Map map)
        {
            int count = 0;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (Eligible(pawn))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Starts on the next check, after the ritual lord has let go of its pawns.</summary>
        public void RequestStart() => startPending = !active;

        /// <summary>Nearest other fighter this pawn can reach. Downed fighters count: the rumble is to the death.</summary>
        public Pawn NearestTarget(Pawn pawn)
        {
            Buffer.Clear();
            foreach (Pawn other in fighters)
            {
                if (other != pawn && !other.Dead && other.Spawned && other.Map == pawn.Map)
                {
                    Buffer.Add(other);
                }
            }
            Buffer.SortBy(p => p.Position.DistanceToSquared(pawn.Position));
            int checks = 0;
            foreach (Pawn other in Buffer)
            {
                if (checks++ >= MaxReachChecks)
                {
                    break;
                }
                if (pawn.CanReach(other, PathEndMode.Touch, Danger.Deadly))
                {
                    return other;
                }
            }
            return null;
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % CheckInterval != 0)
            {
                return;
            }
            if (startPending)
            {
                startPending = false;
                Start();
                return;
            }
            if (active)
            {
                Check();
            }
        }

        private void Start()
        {
            if (active)
            {
                return;
            }
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (Eligible(pawn))
                {
                    fighters.Add(pawn);
                }
            }
            // The count can drop between the ritual starting and ending.
            if (!RumbleRules.EnoughNoblins(fighters.Count))
            {
                Messages.Message("MSSG_RumbleFizzled".Translate(fighters.Count, RumbleRules.MinNoblins), MessageTypeDefOf.NeutralEvent);
                fighters.Clear();
                return;
            }
            active = true;
            startTick = Find.TickManager.TicksGame;
            startCount = fighters.Count;
            deaths = 0;
            winners.Clear();
            foreach (Pawn pawn in fighters)
            {
                AllFighters.Add(pawn);
                pawn.health.AddHediff(HobGobDefOf.MSSG_RumbleFrenzy);
                EnsureFighting(pawn);
            }
            Find.LetterStack.ReceiveLetter("MSSG_RumbleStartLabel".Translate(), "MSSG_RumbleStartText".Translate(startCount),
                LetterDefOf.ThreatSmall, new LookTargets(fighters));
        }

        private void Check()
        {
            Buffer.Clear();
            Buffer.AddRange(fighters);
            int standing = 0;
            int ableStanding = 0;
            foreach (Pawn pawn in Buffer)
            {
                if (pawn.Dead)
                {
                    deaths++;
                    Leave(pawn);
                    continue;
                }
                if (!pawn.Spawned || pawn.Map != map)
                {
                    Leave(pawn);
                    continue;
                }
                Gene_GoblinEvolution tracker = Hediff_EvolutionProgress.FindTracker(pawn);
                if (tracker != null && tracker.CanEvolveNow)
                {
                    Win(pawn, tracker);
                    continue;
                }
                if (!pawn.Downed)
                {
                    standing++;
                    if (!pawn.WorkTagIsDisabled(WorkTags.Violent))
                    {
                        ableStanding++;
                    }
                    EnsureFighting(pawn);
                }
            }
            RumbleOutcome outcome = RumbleRules.Resolve(fighters.Count, standing, ableStanding, Find.TickManager.TicksGame - startTick);
            if (outcome != RumbleOutcome.Continue)
            {
                End(outcome);
            }
        }

        private void Win(Pawn pawn, Gene_GoblinEvolution tracker)
        {
            Leave(pawn);
            winners.Add(pawn.LabelShort);
            tracker.EnqueueEvolution();
            Messages.Message("MSSG_RumbleWinner".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
        }

        private void End(RumbleOutcome outcome)
        {
            Pawn last = null;
            List<Pawn> survivors = new List<Pawn>(fighters);
            foreach (Pawn pawn in survivors)
            {
                last = pawn;
                Leave(pawn);
            }
            active = false;
            string ending;
            if (RumbleRules.PunishesSurvivors(outcome))
            {
                survivors.RemoveAll(p => p.Dead);
                foreach (Pawn pawn in survivors)
                {
                    GoblinEvolutionComponent.ForceEvolve(pawn, HobGobDefOf.MSSG_Snoblin);
                }
                ending = survivors.Count == 1
                    ? "MSSG_RumblePunished".Translate(survivors[0].Named("PAWN"))
                    : "MSSG_RumblePunishedMany".Translate(survivors.Select(p => p.LabelShort).ToCommaList(useAnd: true));
            }
            else
            {
                ending = ("MSSG_RumbleEnd_" + outcome).Translate();
            }
            string winnerList = winners.Count > 0 ? winners.ToCommaList(useAnd: true) : (string)"MSSG_RumbleNoWinners".Translate();
            Find.LetterStack.ReceiveLetter("MSSG_RumbleOverLabel".Translate(),
                "MSSG_RumbleOverText".Translate(startCount, deaths, winners.Count, winnerList, ending),
                LetterDefOf.NeutralEvent, last != null && last.Spawned ? new LookTargets(last) : LookTargets.Invalid);
            winners.Clear();
        }

        /// <summary>Takes the pawn out of the fight. Safe on dead pawns.</summary>
        private void Leave(Pawn pawn)
        {
            fighters.Remove(pawn);
            AllFighters.Remove(pawn);
            if (pawn.Dead)
            {
                return;
            }
            Hediff frenzy = pawn.health?.hediffSet.GetFirstHediffOfDef(HobGobDefOf.MSSG_RumbleFrenzy);
            if (frenzy != null)
            {
                pawn.health.RemoveHediff(frenzy);
            }
            if (pawn.MentalStateDef == HobGobDefOf.MSSG_NobbyRumble)
            {
                pawn.MentalState.RecoverFromState();
            }
        }

        /// <summary>Puts a standing fighter (back) into the rumble state, e.g. after it got up from being downed.</summary>
        private static void EnsureFighting(Pawn pawn)
        {
            if (pawn.Downed || pawn.MentalStateDef == HobGobDefOf.MSSG_NobbyRumble)
            {
                return;
            }
            bool started = pawn.mindState.mentalStateHandler.TryStartMentalState(HobGobDefOf.MSSG_NobbyRumble,
                forced: true, forceWake: true, transitionSilently: true);
            // Some mods block mental states. ThinkNode_ConditionalRumbleFighter then drives the pawn,
            // but only once its current job ends, so ask for a re-think now.
            if (!started && pawn.CurJobDef != JobDefOf.AttackMelee)
            {
                pawn.jobs?.CheckForJobOverride();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref active, "active");
            Scribe_Values.Look(ref startPending, "startPending");
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref startCount, "startCount");
            Scribe_Values.Look(ref deaths, "deaths");
            Scribe_Collections.Look(ref fighters, "fighters", LookMode.Reference);
            Scribe_Collections.Look(ref winners, "winners", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                fighters ??= new HashSet<Pawn>();
                fighters.RemoveWhere(p => p == null);
                winners ??= new List<string>();
                foreach (Pawn pawn in fighters)
                {
                    AllFighters.Add(pawn);
                }
            }
        }
    }
}
