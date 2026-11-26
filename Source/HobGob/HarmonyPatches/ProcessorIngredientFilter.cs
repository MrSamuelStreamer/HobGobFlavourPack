using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace HobGob.HarmonyPatches
{
    /// <summary>
    /// Processor Framework's "fill processor" work giver looks for an ingredient once per processor, and each
    /// look scans every ingredient on the map that any processor accepts. Its own check then throws away anything
    /// the processor does not accept. With hundreds of unrelated items (for example rock chunks) that scan
    /// dominates job searching. This patch hands the search only the items the processor accepts, which gives
    /// the same answer. Applied by name at runtime so this pack needs no reference to Processor Framework.
    /// </summary>
    public static class ProcessorIngredientFilter
    {
        private const string GiverTypeName = "ProcessorFramework.WorkGiver_FillProcessor";
        private const string CompTypeName = "ProcessorFramework.CompProcessor";
        private const string MapCompTypeName = "ProcessorFramework.MapComponent_Processors";

        private static readonly List<Thing> Filtered = new List<Thing>();

        private static MethodInfo validIngredientsGetter;
        private static HashSet<ThingDef> activeFilter;

        /// <summary>Patches Processor Framework when that mod is loaded; does nothing otherwise.</summary>
        public static void TryPatch(Harmony harmony)
        {
            System.Type giver = AccessTools.TypeByName(GiverTypeName);
            System.Type comp = AccessTools.TypeByName(CompTypeName);
            System.Type mapComp = AccessTools.TypeByName(MapCompTypeName);
            if (giver == null || comp == null || mapComp == null)
            {
                return;
            }

            MethodInfo findIngredient = AccessTools.Method(giver, "FindIngredient");
            MethodInfo potentialIngredients = AccessTools.PropertyGetter(mapComp, "PotentialIngredients");
            validIngredientsGetter = AccessTools.PropertyGetter(comp, "ValidIngredients");
            if (findIngredient == null || potentialIngredients == null || validIngredientsGetter == null)
            {
                Log.Warning("[Goblin Flavour Pack] Processor Framework changed shape; ingredient filter patch skipped.");
                return;
            }

            harmony.Patch(findIngredient,
                prefix: new HarmonyMethod(typeof(ProcessorIngredientFilter), nameof(FindIngredientPrefix)),
                finalizer: new HarmonyMethod(typeof(ProcessorIngredientFilter), nameof(FindIngredientFinalizer)));
            harmony.Patch(potentialIngredients,
                postfix: new HarmonyMethod(typeof(ProcessorIngredientFilter), nameof(PotentialIngredientsPostfix)));
            Log.Message("[Goblin Flavour Pack] Filtered Processor Framework ingredient search.");
        }

        public static void FindIngredientPrefix(ThingComp comp)
        {
            activeFilter = comp == null ? null : validIngredientsGetter.Invoke(comp, null) as HashSet<ThingDef>;
        }

        public static void FindIngredientFinalizer()
        {
            activeFilter = null;
        }

        public static void PotentialIngredientsPostfix(ref List<Thing> __result)
        {
            if (activeFilter == null || __result == null)
            {
                return;
            }

            Filtered.Clear();
            foreach (Thing thing in __result)
            {
                if (activeFilter.Contains(thing.def))
                {
                    Filtered.Add(thing);
                }
            }
            __result = Filtered;
        }
    }
}
