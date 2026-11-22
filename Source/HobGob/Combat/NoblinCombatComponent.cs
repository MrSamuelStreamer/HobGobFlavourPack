using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Combat
{
    /// <summary>
    /// While a hostile threat is active on a map, its Noblin colonists are drafted with Big and Small Auto-Combat on,
    /// and the player cannot stand them down (see the draft patches). When the threat ends, they are undrafted.
    /// </summary>
    public class NoblinCombatComponent : GameComponent
    {
        private const int CheckInterval = 120;

        private static NoblinCombatComponent instance;

        private HashSet<Pawn> forced = new HashSet<Pawn>();
        private readonly HashSet<Map> threatMaps = new HashSet<Map>();
        private readonly List<Pawn> scratch = new List<Pawn>();

        public NoblinCombatComponent(Verse.Game game)
        {
            instance = this;
        }

        /// <summary>True when the pawn was drafted by this component and its map still has a threat.</summary>
        public static bool IsLocked(Pawn pawn)
        {
            NoblinCombatComponent c = instance;
            return c != null && pawn != null && Enabled && c.forced.Contains(pawn) && pawn.MapHeld != null && c.threatMaps.Contains(pawn.MapHeld);
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % CheckInterval == 0)
            {
                Update();
            }
        }

        /// <summary>Re-checks every map now. Public so it can be run from the dev console.</summary>
        public void Update()
        {
            threatMaps.Clear();
            if (!Enabled)
            {
                // Drop the lock but leave pawns drafted, so turning this off mid-fight does not pull them out.
                forced.Clear();
                return;
            }
            foreach (Map map in Find.Maps)
            {
                if (map.mapPawns.AnyFreeColonistSpawned)
                {
                    if (GenHostility.AnyHostileActiveThreatToPlayer(map, countDormantPawnsAsHostile: false))
                    {
                        threatMaps.Add(map);
                        DraftNoblins(map);
                    }
                }
            }
            ReleaseIdle();
        }

        private void DraftNoblins(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (!CanFight(pawn) || !NoblinRegistry.IsNoblin(pawn))
                {
                    continue;
                }
                if (!pawn.Drafted)
                {
                    pawn.drafter.Drafted = true;
                }
                if (pawn.Drafted)
                {
                    forced.Add(pawn);
                    BigSmallAutoCombat.ForceHunt(pawn);
                }
            }
        }

        /// <summary>Undrafts forced pawns whose map no longer has a threat; forgets pawns that are gone.</summary>
        private void ReleaseIdle()
        {
            scratch.Clear();
            foreach (Pawn pawn in forced)
            {
                if (pawn == null || pawn.Destroyed || pawn.MapHeld == null || !threatMaps.Contains(pawn.MapHeld))
                {
                    scratch.Add(pawn);
                }
            }
            foreach (Pawn pawn in scratch)
            {
                // Remove first so the draft lock lets this undraft through.
                forced.Remove(pawn);
                if (pawn != null && !pawn.Destroyed && pawn.Spawned && pawn.Drafted)
                {
                    pawn.drafter.Drafted = false;
                }
            }
        }

        /// <summary>The mod setting that turns Noblin auto-draft and Auto-Combat on or off.</summary>
        private static bool Enabled => HobGobMod.Settings?.noblinAutoCombat ?? true;

        /// <summary>A pawn that can be drafted to fight now. Non-violent Noblins are left alone.</summary>
        public static bool CanFight(Pawn pawn) =>
            pawn.Spawned && !pawn.Dead && !pawn.Downed && !pawn.InMentalState && pawn.drafter != null
            && !pawn.WorkTagIsDisabled(WorkTags.Violent);

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref forced, "forcedNoblins", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                forced ??= new HashSet<Pawn>();
                forced.RemoveWhere(p => p == null);
            }
        }

        public override void FinalizeInit()
        {
            instance = this;
            // Fill threatMaps now, so the lock holds on the first frames after a load.
            threatMaps.Clear();
            foreach (Map map in Find.Maps)
            {
                if (GenHostility.AnyHostileActiveThreatToPlayer(map, countDormantPawnsAsHostile: false))
                {
                    threatMaps.Add(map);
                }
            }
        }
    }
}
