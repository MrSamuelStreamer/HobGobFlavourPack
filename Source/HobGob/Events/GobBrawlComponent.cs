using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace HobGob.Events
{
    /// <summary>
    /// Knob Gob Gobbo Gone Wild on one map: every able noblin brawls every able goblin with social-fight blows.
    /// A fighter who drops leaves the brawl. It ends when one side has nobody standing, or after
    /// <see cref="EventRules.BrawlDurationTicks"/>. Started by <see cref="IncidentWorker_GobboGoneWild"/>.
    /// </summary>
    public class GobBrawlComponent : MapComponent
    {
        private const int CheckInterval = 60;
        private const int MaxReachChecks = 8;

        private static readonly List<Pawn> Buffer = new List<Pawn>();

        private bool active;
        private int startTick;
        private HashSet<Pawn> noblins = new HashSet<Pawn>();
        private HashSet<Pawn> goblins = new HashSet<Pawn>();
        private List<Pawn> noblinsJoined = new List<Pawn>();
        private List<Pawn> goblinsJoined = new List<Pawn>();

        public GobBrawlComponent(Map map) : base(map)
        {
        }

        public bool Active => active;

        public static GobBrawlComponent For(Map map) => map?.GetComponent<GobBrawlComponent>();

        public static bool IsNoblinSide(Pawn pawn) => NoblinRegistry.IsNoblin(pawn);

        public static bool IsGoblinSide(Pawn pawn) =>
            pawn.genes?.Xenotype == HobGobDefOf.MSSG_Goblin && !NoblinRegistry.IsNoblin(pawn);

        /// <summary>A free adult colonist who is free to join a brawl right now.</summary>
        public static bool CanJoin(Pawn pawn) =>
            pawn.Spawned && !pawn.Dead && !pawn.Downed && !pawn.Drafted && !pawn.InMentalState
            && pawn.IsFreeColonist && pawn.DevelopmentalStage.Adult()
            && !pawn.WorkTagIsDisabled(WorkTags.Violent)
            && !Rumble.NobbyRumbleComponent.IsFighter(pawn);

        public static List<Pawn> Eligible(Map map, System.Func<Pawn, bool> side) =>
            map.mapPawns.FreeColonistsSpawned.Where(p => side(p) && CanJoin(p)).ToList();

        /// <summary>True if any other HobGob fight is running on the map.</summary>
        public static bool OtherFightActive(Map map) =>
            (Rumble.NobbyRumbleComponent.For(map)?.Active ?? false)
            || (map.GetComponent<HobgoblinChallengeComponent>()?.Active ?? false)
            || (map.GetComponent<GoblinDeathDuelComponent>()?.Active ?? false);

        /// <summary>Starts the brawl. Returns the pawns that actually joined.</summary>
        public List<Pawn> Start(List<Pawn> noblinSide, List<Pawn> goblinSide)
        {
            noblins.Clear();
            goblins.Clear();
            foreach (Pawn pawn in noblinSide.Where(TryEnterState))
            {
                noblins.Add(pawn);
            }
            foreach (Pawn pawn in goblinSide.Where(TryEnterState))
            {
                goblins.Add(pawn);
            }
            noblinsJoined = noblins.ToList();
            goblinsJoined = goblins.ToList();
            startTick = Find.TickManager.TicksGame;
            active = true;
            return noblinsJoined.Concat(goblinsJoined).ToList();
        }

        public int NoblinCount => noblinsJoined.Count;

        public int GoblinCount => goblinsJoined.Count;

        /// <summary>Nearest standing fighter on the other side that this pawn can reach.</summary>
        public Pawn NearestEnemy(Pawn pawn)
        {
            HashSet<Pawn> enemies = noblins.Contains(pawn) ? goblins : goblins.Contains(pawn) ? noblins : null;
            if (enemies == null)
            {
                return null;
            }
            Buffer.Clear();
            Buffer.AddRange(enemies.Where(o => !o.Dead && !o.Downed && o.Spawned && o.Map == pawn.Map));
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
            if (active && Find.TickManager.TicksGame % CheckInterval == 0)
            {
                Check();
            }
        }

        private void Check()
        {
            DropOut(noblins);
            DropOut(goblins);
            bool timedOut = Find.TickManager.TicksGame - startTick >= EventRules.BrawlDurationTicks;
            BrawlOutcome outcome = EventRules.ResolveBrawl(noblins.Count, goblins.Count, timedOut);
            if (outcome != BrawlOutcome.Ongoing)
            {
                End(outcome);
            }
        }

        /// <summary>Removes fighters who dropped, left the map or lost the brawl state.</summary>
        private void DropOut(HashSet<Pawn> side)
        {
            foreach (Pawn pawn in side.ToList())
            {
                bool fighting = !pawn.Dead && !pawn.Downed && pawn.Spawned && pawn.Map == map
                    && pawn.MentalStateDef == HobGobDefOf.MSSG_GobBrawl;
                if (!fighting)
                {
                    Leave(pawn);
                    side.Remove(pawn);
                }
            }
        }

        private void End(BrawlOutcome outcome)
        {
            List<Pawn> standing = noblins.Concat(goblins).ToList();
            foreach (Pawn pawn in standing)
            {
                Leave(pawn);
            }
            GiveMemories(noblinsJoined, goblinsJoined, outcome == BrawlOutcome.NoblinsWin);
            GiveMemories(goblinsJoined, noblinsJoined, outcome == BrawlOutcome.GoblinsWin);
            SendResult(outcome, standing);
            active = false;
            noblins.Clear();
            goblins.Clear();
            noblinsJoined.Clear();
            goblinsJoined.Clear();
        }

        private static void Leave(Pawn pawn)
        {
            if (!pawn.Dead && pawn.MentalStateDef == HobGobDefOf.MSSG_GobBrawl)
            {
                pawn.MentalState.RecoverFromState();
            }
        }

        private static bool TryEnterState(Pawn pawn) =>
            pawn.mindState.mentalStateHandler.TryStartMentalState(
                HobGobDefOf.MSSG_GobBrawl, forced: true, forceWake: true, transitionSilently: true);

        /// <summary>Vanilla social-fight memories, each toward a random fighter from the other side.</summary>
        private static void GiveMemories(List<Pawn> side, List<Pawn> others, bool won)
        {
            ThoughtDef def = won ? ThoughtDefOf.HadCatharticFight : ThoughtDefOf.HadAngeringFight;
            List<Pawn> alive = others.Where(p => p != null && !p.Dead).ToList();
            foreach (Pawn pawn in side)
            {
                if (pawn == null || pawn.Dead || alive.Count == 0)
                {
                    continue;
                }
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(def, alive.RandomElement());
            }
        }

        private void SendResult(BrawlOutcome outcome, List<Pawn> standing)
        {
            string key = outcome == BrawlOutcome.NoblinsWin ? "MSSG_BrawlNoblinsWin"
                : outcome == BrawlOutcome.GoblinsWin ? "MSSG_BrawlGoblinsWin"
                : "MSSG_BrawlDraw";
            Find.LetterStack.ReceiveLetter(
                (key + "Label").Translate(),
                (key + "Text").Translate(),
                LetterDefOf.NeutralEvent,
                new LookTargets(standing.Where(p => p.Spawned)));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref active, "active");
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Collections.Look(ref noblins, "noblins", LookMode.Reference);
            Scribe_Collections.Look(ref goblins, "goblins", LookMode.Reference);
            Scribe_Collections.Look(ref noblinsJoined, "noblinsJoined", LookMode.Reference);
            Scribe_Collections.Look(ref goblinsJoined, "goblinsJoined", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                noblins ??= new HashSet<Pawn>();
                goblins ??= new HashSet<Pawn>();
                noblinsJoined ??= new List<Pawn>();
                goblinsJoined ??= new List<Pawn>();
                noblins.RemoveWhere(p => p == null);
                goblins.RemoveWhere(p => p == null);
                noblinsJoined.RemoveAll(p => p == null);
                goblinsJoined.RemoveAll(p => p == null);
            }
        }
    }
}
