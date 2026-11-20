using System;
using System.Collections.Generic;
using System.Linq;

namespace HobGob.Slap
{
    /// <summary>One skill as the passion re-roll sees it. Passion is 0 none, 1 minor, 2 major.</summary>
    public readonly struct PassionSlot
    {
        public PassionSlot(int passion, bool locked = false, bool blocked = false, bool forced = false)
        {
            Passion = passion;
            Locked = locked;
            Blocked = blocked;
            Forced = forced;
        }

        public int Passion { get; }

        /// <summary>Left exactly as it is (a gene controls it).</summary>
        public bool Locked { get; }

        /// <summary>Can never get a passion (skill disabled, or a trait forbids it).</summary>
        public bool Blocked { get; }

        /// <summary>Must keep at least a minor passion (a trait needs it).</summary>
        public bool Forced { get; }
    }

    /// <summary>Game-free rules for the "slap some sense into 'em" ritual. Linked into the unit test project.</summary>
    public static class SlapRules
    {
        public const int None = 0;
        public const int Minor = 1;
        public const int Major = 2;

        /// <summary>No more thumps once the target hurts this much. Well under the usual pain shock threshold.</summary>
        public const float MaxPainForThump = 0.5f;

        /// <summary>A thump may not take a body part below this share of its max health.</summary>
        public const float MinPartHealthShare = 0.5f;

        public const int MinThumpDamage = 1;
        public const int MaxThumpDamage = 3;

        /// <summary>Pause between turns so each thump reads clearly.</summary>
        public const int GapBetweenTurnsTicks = 45;

        /// <summary>
        /// The thumping is over when nobody eligible is still waiting, no turn is in progress, and the last
        /// thump has had its pause.
        /// </summary>
        public static bool TurnsDone(int eligibleWaiting, bool turnInProgress, int ticksSinceLastTurn)
        {
            return eligibleWaiting <= 0 && !turnInProgress && ticksSinceLastTurn >= GapBetweenTurnsTicks;
        }

        /// <summary>True if a thump of this size is safe: the target is not hurting too much and the part keeps half its health.</summary>
        public static bool SafeToThump(float painTotal, float partHealth, float partMaxHealth, float damage)
        {
            if (painTotal >= MaxPainForThump || partMaxHealth <= 0f)
            {
                return false;
            }
            return partHealth - damage >= partMaxHealth * MinPartHealthShare;
        }

        /// <summary>How many passion levels the outcome adds (positive) or takes away (negative).</summary>
        public static int PassionStepsFor(int positivityIndex)
        {
            if (positivityIndex <= -2)
            {
                return -1;
            }
            if (positivityIndex >= 2)
            {
                return 2;
            }
            return positivityIndex >= 1 ? 1 : 0;
        }

        /// <summary>
        /// Moves the same number of minor and major passions onto random allowed skills, then adds or removes
        /// passion levels by <paramref name="positivityIndex"/>. Returns the new passion for each slot.
        /// </summary>
        public static int[] Reroll(IReadOnlyList<PassionSlot> slots, int positivityIndex, Random rng)
        {
            int[] result = new int[slots.Count];
            var open = new List<int>();
            int majors = 0;
            int minors = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                PassionSlot slot = slots[i];
                if (slot.Locked)
                {
                    result[i] = slot.Passion;
                    continue;
                }
                if (slot.Passion >= Major)
                {
                    majors++;
                }
                else if (slot.Passion == Minor)
                {
                    minors++;
                }
                if (!slot.Blocked)
                {
                    open.Add(i);
                }
            }

            Shuffle(open, rng);
            // Stable sort: forced skills get passions first, the rest stay in shuffled order.
            open = open.OrderByDescending(i => slots[i].Forced).ToList();
            foreach (int i in open)
            {
                if (majors > 0)
                {
                    result[i] = Major;
                    majors--;
                }
                else if (minors > 0 || slots[i].Forced)
                {
                    result[i] = Minor;
                    minors = Math.Max(0, minors - 1);
                }
            }
            // More passions than open skills: spend the left-over majors as upgrades.
            for (; majors > 0 && Step(result, slots, open, +1, rng); majors--)
            {
            }

            int steps = PassionStepsFor(positivityIndex);
            int direction = Math.Sign(steps);
            for (int s = Math.Abs(steps); s > 0 && Step(result, slots, open, direction, rng); s--)
            {
            }
            return result;
        }

        /// <summary>
        /// Raises or lowers one random open skill by one level. Raising prefers a skill with no passion, so a good
        /// outcome gives a brand new passion; it only upgrades minor to major when every open skill already has one.
        /// False if no skill can move.
        /// </summary>
        private static bool Step(int[] result, IReadOnlyList<PassionSlot> slots, List<int> open, int direction, Random rng)
        {
            var candidates = new List<int>();
            foreach (int i in open)
            {
                int floor = slots[i].Forced ? Minor : None;
                if (direction > 0 ? result[i] == None : result[i] > floor)
                {
                    candidates.Add(i);
                }
            }
            if (candidates.Count == 0 && direction > 0)
            {
                candidates.AddRange(open.Where(i => result[i] < Major));
            }
            if (candidates.Count == 0)
            {
                return false;
            }
            result[candidates[rng.Next(candidates.Count)]] += direction;
            return true;
        }

        /// <summary>
        /// Shuffles skill levels between the skills that are not disabled. The same levels come out, just on
        /// different skills. Disabled skills keep their level.
        /// </summary>
        public static int[] ShuffleLevels(IReadOnlyList<int> levels, IReadOnlyList<bool> disabled, Random rng)
        {
            int[] result = levels.ToArray();
            List<int> open = Enumerable.Range(0, levels.Count).Where(i => !disabled[i]).ToList();
            List<int> pool = open.Select(i => levels[i]).ToList();
            Shuffle(pool, rng);
            for (int k = 0; k < open.Count; k++)
            {
                result[open[k]] = pool[k];
            }
            return result;
        }

        private static void Shuffle(List<int> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
