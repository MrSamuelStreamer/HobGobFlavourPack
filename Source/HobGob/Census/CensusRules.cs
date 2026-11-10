using System.Collections.Generic;

namespace HobGob.Census
{
    public enum CensusChange
    {
        NoChange,
        Promoted,
        Demoted,
    }

    /// <summary>Game-free census maths. Kept free of Verse types so it can be unit tested.</summary>
    public static class CensusRules
    {
        public const int CensusHour = 11;

        /// <summary>Index of the highest tier whose minimum is at or below <paramref name="count"/>. 0 when below every tier.</summary>
        /// <param name="sortedMins">Tier minimums, ascending.</param>
        public static int TierIndexFor(int count, IReadOnlyList<int> sortedMins)
        {
            int index = 0;
            for (int i = 0; i < sortedMins.Count; i++)
            {
                if (sortedMins[i] > count)
                {
                    break;
                }
                index = i;
            }
            return index;
        }

        /// <summary>Goblins still needed to reach the next tier, or null at the top tier.</summary>
        public static int? MoreUntilNext(int count, int tierIndex, IReadOnlyList<int> sortedMins)
        {
            int next = tierIndex + 1;
            if (next >= sortedMins.Count)
            {
                return null;
            }
            int needed = sortedMins[next] - count;
            return needed < 1 ? 1 : needed;
        }

        public static CensusChange Compare(int previousIndex, int newIndex)
        {
            if (newIndex > previousIndex)
            {
                return CensusChange.Promoted;
            }
            return newIndex < previousIndex ? CensusChange.Demoted : CensusChange.NoChange;
        }

        /// <summary>True during the census hour of a day that has not had a census yet.</summary>
        public static bool IsDue(int localHour, int censusHour, int localDayStamp, int lastDayStamp) =>
            localHour == censusHour && localDayStamp != lastDayStamp;

        /// <summary>A number unique to each local day, so the census runs once per day.</summary>
        public static int DayStamp(int year, int dayOfYear, int daysPerYear) => year * daysPerYear + dayOfYear;
    }
}
