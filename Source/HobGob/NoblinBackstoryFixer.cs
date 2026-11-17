using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HobGob
{
    /// <summary>
    /// Swaps a Noblin's backstory when it disables Noblin work (e.g. a childhood that bans mining).
    /// New Noblins avoid these through MSSG_Pitborn's requiredWorkTags; this fixes Noblins that already exist.
    /// </summary>
    public static class NoblinBackstoryFixer
    {
        /// <summary>Tags of every Noblin task's work type. Keep in sync with MSSG_Pitborn's requiredWorkTags.</summary>
        public const WorkTags NoblinWorkTags = WorkTags.Firefighting | WorkTags.ManualDumb | WorkTags.ManualSkilled
            | WorkTags.Hauling | WorkTags.Cleaning | WorkTags.Mining | WorkTags.PlantWork;

        /// <summary>Replaces each blocking backstory. Returns true if anything changed.</summary>
        public static bool TryFix(Pawn pawn)
        {
            Pawn_StoryTracker story = pawn.story;
            if (story == null)
            {
                return false;
            }
            bool changed = false;
            if (Blocks(story.Childhood) && TryPickReplacement(story.Childhood, out BackstoryDef childhood))
            {
                story.Childhood = childhood;
                changed = true;
            }
            if (Blocks(story.Adulthood) && TryPickReplacement(story.Adulthood, out BackstoryDef adulthood))
            {
                story.Adulthood = adulthood;
                changed = true;
            }
            if (changed)
            {
                pawn.Notify_DisabledWorkTypesChanged();
            }
            return changed;
        }

        private static bool Blocks(BackstoryDef backstory) =>
            backstory != null && (backstory.workDisables & NoblinWorkTags) != WorkTags.None;

        /// <summary>A same-slot backstory that blocks no Noblin work, sharing a spawn category with the old one if possible.</summary>
        private static bool TryPickReplacement(BackstoryDef old, out BackstoryDef result)
        {
            List<BackstoryDef> candidates = DefDatabase<BackstoryDef>.AllDefsListForReading
                .Where(b => b.slot == old.slot && b.shuffleable && !Blocks(b)
                    && !b.spawnCategories.Contains("Child") && !b.spawnCategories.Contains("Newborn"))
                .ToList();
            return candidates.Where(b => b.spawnCategories.Any(old.spawnCategories.Contains)).TryRandomElement(out result)
                || candidates.TryRandomElement(out result);
        }
    }
}
