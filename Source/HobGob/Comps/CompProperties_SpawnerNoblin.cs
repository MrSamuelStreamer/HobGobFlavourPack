using Verse;

namespace HobGob.Comps
{
    public class CompProperties_SpawnerNoblin : CompProperties
    {
        public IntRange spawnIntervalTicks = new IntRange(120000, 240000);

        public CompProperties_SpawnerNoblin()
        {
            compClass = typeof(CompSpawnerNoblin);
        }
    }
}
