using System;

namespace HobGob.Events
{
    /// <summary>Game-free numbers for the goblin events. Linked into the unit test project.</summary>
    public static class EventRules
    {
        /// <summary>Pit Overflow base roll, before extra pits.</summary>
        public const int OverflowMin = 3;
        public const int OverflowMax = 8;

        /// <summary>Extra Noblins for each player pit after the first.</summary>
        public const int OverflowPerExtraPit = 2;

        /// <summary>A pit-fresh Noblin below this food level feels starving.</summary>
        public const float PitStarvingFoodThreshold = 0.3f;

        /// <summary>How many Noblins crawl out: base roll plus extra pits, capped by the free cells.</summary>
        public static int OverflowCount(int baseRoll, int playerPits, int freeCells)
        {
            int wanted = baseRoll + OverflowPerExtraPit * Math.Max(0, playerPits - 1);
            return Math.Max(0, Math.Min(wanted, freeCells));
        }

        public static bool IsPitStarving(bool hasPitFresh, float foodLevelPct)
        {
            return hasPitFresh && foodLevelPct < PitStarvingFoodThreshold;
        }
    }
}
