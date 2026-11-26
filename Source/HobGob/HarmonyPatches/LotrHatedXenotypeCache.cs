using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HobGob.HarmonyPatches
{
    /// <summary>
    /// Legacy of the Rim's ThoughtWorker_HatedXenotypePresent scans every colonist on the map for every pawn
    /// each time the thought is evaluated, so a large colony pays colonists x colonists per refresh.
    /// The answer only depends on the faction, the ideo and the map, so it is computed once per group and reused
    /// for a short time. Applied by name at runtime so this pack needs no reference to Legacy of the Rim.
    /// </summary>
    public static class LotrHatedXenotypeCache
    {
        private const string WorkerTypeName = "LegacyofTheRimCore.ThoughtWorker_HatedXenotypePresent";
        private const string UtilTypeName = "LegacyofTheRimCore.XenotypePreceptUtility";
        private const int RefreshTicks = 250;

        private static readonly Dictionary<(int Map, int Faction, int Ideo), CacheEntry> Cache =
            new Dictionary<(int Map, int Faction, int Ideo), CacheEntry>();

        private static Func<Pawn, IEnumerable<Precept>, bool> isHatedXenotype;
        private static Game cachedGame;

        private sealed class CacheEntry
        {
            public int Tick;
            public readonly List<Pawn> Hated = new List<Pawn>();
        }

        /// <summary>Patches the Legacy of the Rim worker when that mod is loaded; does nothing otherwise.</summary>
        public static void TryPatch(Harmony harmony)
        {
            Type worker = AccessTools.TypeByName(WorkerTypeName);
            Type util = AccessTools.TypeByName(UtilTypeName);
            if (worker == null || util == null)
            {
                return;
            }

            var target = AccessTools.Method(worker, "ShouldHaveThought", new[] { typeof(Pawn) });
            var check = AccessTools.Method(util, "IsPawnHatedXenotype", new[] { typeof(Pawn), typeof(IEnumerable<Precept>) });
            if (target == null || check == null)
            {
                Log.Warning("[Goblin Flavour Pack] Legacy of the Rim hated-xenotype thought changed shape; cache patch skipped.");
                return;
            }

            isHatedXenotype = (Func<Pawn, IEnumerable<Precept>, bool>)Delegate.CreateDelegate(
                typeof(Func<Pawn, IEnumerable<Precept>, bool>), check);
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(LotrHatedXenotypeCache), nameof(Prefix)));
            Log.Message("[Goblin Flavour Pack] Cached Legacy of the Rim hated-xenotype thought.");
        }

        /// <returns>False once the cached answer is in __result; true to run the original for any unusual pawn.</returns>
        public static bool Prefix(Pawn p, ref ThoughtState __result)
        {
            if (!ModsConfig.BiotechActive || !ModsConfig.IdeologyActive)
            {
                return true;
            }
            Faction faction = p.Faction;
            Ideo ideo = p.Ideo;
            Map map = p.MapHeld;
            if (faction == null || ideo == null || map == null)
            {
                return true;
            }

            CacheEntry entry = Lookup(map, faction, ideo);
            bool otherHated = false;
            foreach (Pawn hated in entry.Hated)
            {
                if (hated != p)
                {
                    otherHated = true;
                    break;
                }
            }
            __result = otherHated ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
            return false;
        }

        private static CacheEntry Lookup(Map map, Faction faction, Ideo ideo)
        {
            // A loaded save can reuse the same ids and tick, so entries from the previous game must not survive it.
            Game game = Current.Game;
            if (!ReferenceEquals(cachedGame, game))
            {
                Cache.Clear();
                cachedGame = game;
            }

            int now = Find.TickManager.TicksGame;
            var key = (map.uniqueID, faction.loadID, ideo.id);
            if (Cache.TryGetValue(key, out CacheEntry cached) && now >= cached.Tick && now - cached.Tick < RefreshTicks)
            {
                return cached;
            }

            CacheEntry fresh = new CacheEntry { Tick = now };
            List<Precept> precepts = ideo.PreceptsListForReading;
            foreach (Pawn item in map.mapPawns.SpawnedPawnsInFaction(faction))
            {
                if (item.IsFreeColonist && !item.IsSlave && !item.IsPrisoner && item.genes != null
                    && isHatedXenotype(item, precepts))
                {
                    fresh.Hated.Add(item);
                }
            }
            Cache[key] = fresh;
            return fresh;
        }
    }
}
