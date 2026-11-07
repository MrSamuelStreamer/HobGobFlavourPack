using RimWorld;
using Verse;

namespace HobGob.Comps
{
    public class CompProperties_AbilityDigHole : CompProperties_AbilityEffect
    {
        public ThingDef thingDef;

        public CompProperties_AbilityDigHole()
        {
            compClass = typeof(CompAbilityEffect_DigHole);
        }
    }
}
