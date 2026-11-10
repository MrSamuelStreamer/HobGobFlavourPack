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

        /// <summary>Shiny Fever needs at least this many managed Noblins.</summary>
        public const int ShinyFeverMinNoblins = 2;

        /// <summary>How often a feverish Noblin looks for something to steal.</summary>
        public const int ShinyFeverCheckTicks = 250;

        /// <summary>2 in-game hours of rummaging to empty a pit stash.</summary>
        public const int RummageTicks = 5000;

        /// <summary>Noblin Stampede needs at least this many managed Noblins.</summary>
        public const int StampedeMinNoblins = 5;

        /// <summary>A door a stampeding Noblin passes stays held open for 4 hours.</summary>
        public const int DoorHoldTicks = 10000;

        /// <summary>Each stampede leg ends at least this far from where it started.</summary>
        public const int StampedeLegMinDistance = 25;

        /// <summary>A leg is abandoned for a new one after this long.</summary>
        public const int StampedeLegMaxTicks = 2500;

        /// <summary>Within this many cells of the target the pack picks a new leg.</summary>
        public const float StampedeArriveDistance = 6f;

        /// <summary>New hold expiry after a pass. Passing again only ever extends it.</summary>
        public static int HoldExpiry(int existingExpiry, int now)
        {
            return Math.Max(existingExpiry, now + DoorHoldTicks);
        }

        public static bool HoldExpired(int now, int expiry)
        {
            return now >= expiry;
        }

        public static bool NeedsNewLeg(int now, int legStartTick, float distanceToTarget, bool targetValid)
        {
            return !targetValid
                || distanceToTarget <= StampedeArriveDistance
                || now - legStartTick >= StampedeLegMaxTicks;
        }

        /// <summary>How much of a stack one Noblin carries away: as much as fits, never more than the stack.</summary>
        public static int CarryCount(int stackCount, int carrySpace)
        {
            return Math.Max(0, Math.Min(stackCount, carrySpace));
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
