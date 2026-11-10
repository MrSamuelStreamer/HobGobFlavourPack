using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HobGob.Census
{
    /// <summary>Put on a XenotypeDef so pawns of that xenotype count in the goblin census.</summary>
    public class CensusCountedExtension : DefModExtension
    {
    }

    /// <summary>One colony tier of the goblin census. The label is the tier name.</summary>
    public class GoblinCensusTierDef : Def
    {
        /// <summary>Fewest counted goblins needed for this tier.</summary>
        public int minPopulation;

        /// <summary>Letter text when the colony rises into this tier.</summary>
        [MustTranslate]
        public string promotedText;

        /// <summary>Letter text when the colony falls into this tier.</summary>
        [MustTranslate]
        public string demotedText;

        private static List<GoblinCensusTierDef> ordered;
        private static List<int> orderedMins;

        /// <summary>All tiers, lowest minimum first.</summary>
        public static List<GoblinCensusTierDef> Ordered =>
            ordered ??= DefDatabase<GoblinCensusTierDef>.AllDefs.OrderBy(t => t.minPopulation).ToList();

        public static List<int> OrderedMins => orderedMins ??= Ordered.Select(t => t.minPopulation).ToList();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (minPopulation < 0)
            {
                yield return "minPopulation is negative";
            }
            if (promotedText.NullOrEmpty())
            {
                yield return "promotedText is empty";
            }
            if (demotedText.NullOrEmpty())
            {
                yield return "demotedText is empty";
            }
            foreach (GoblinCensusTierDef other in DefDatabase<GoblinCensusTierDef>.AllDefs)
            {
                if (other != this && other.minPopulation == minPopulation)
                {
                    yield return $"same minPopulation ({minPopulation}) as {other.defName}";
                }
            }
            if (this == DefDatabase<GoblinCensusTierDef>.AllDefs.First()
                && !DefDatabase<GoblinCensusTierDef>.AllDefs.Any(t => t.minPopulation == 0))
            {
                yield return "no GoblinCensusTierDef has minPopulation 0, so small colonies have no tier";
            }
        }
    }
}
