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

        /// <summary>Noblin Strike needs at least this many managed Noblins.</summary>
        public const int StrikeMinNoblins = 3;

        /// <summary>3 days.</summary>
        public const int StrikeDurationTicks = 180000;

        /// <summary>Shinies within this many cells of the pit count as tribute.</summary>
        public const int TributeRadius = 2;

        public const int StrikeTributePerNoblin = 15;
        public const int StrikeTributeMin = 50;

        /// <summary>Market value of shinies the strikers demand.</summary>
        public static int StrikeTribute(int noblins)
        {
            return Math.Max(StrikeTributeMin, StrikeTributePerNoblin * noblins);
        }

        /// <summary>
        /// How many units of a stack to take so the remaining tribute is covered.
        /// The last unit may overpay a little.
        /// </summary>
        public static int UnitsToTake(float unitValue, int stackCount, float remaining)
        {
            if (remaining <= 0f || unitValue <= 0f || stackCount <= 0)
            {
                return 0;
            }
            double needed = Math.Ceiling(remaining / (double)unitValue);
            return (int)Math.Min(stackCount, needed);
        }
    }
}
