using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>
    /// What goblins count as shiny. One def (MSSG_Shinies); other mods can patch the lists.
    /// </summary>
    public class GoblinShinyDef : Def
    {
        /// <summary>Always shiny, whatever their value per unit.</summary>
        public List<ThingDef> things = new List<ThingDef>();

        /// <summary>Shiny when worth at least <see cref="minUnitValue"/> per unit.</summary>
        public List<ThingCategoryDef> categories = new List<ThingCategoryDef>();

        public float minUnitValue = 40f;

        public static bool IsShiny(Thing thing)
        {
            GoblinShinyDef shinies = HobGobDefOf.MSSG_Shinies;
            if (shinies == null || thing == null)
            {
                return false;
            }
            ThingDef inner = thing.GetInnerIfMinified().def;
            if (shinies.things.Contains(inner))
            {
                return true;
            }
            if (thing.MarketValue < shinies.minUnitValue)
            {
                return false;
            }
            foreach (ThingCategoryDef category in shinies.categories)
            {
                if (category.ContainedInThisOrDescendant(inner))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
