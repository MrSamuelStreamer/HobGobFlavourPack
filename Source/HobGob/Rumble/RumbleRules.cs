namespace HobGob.Rumble
{
    /// <summary>What the Nobby Nobby Rumble does after a check.</summary>
    public enum RumbleOutcome
    {
        Continue,

        /// <summary>One noblin left (standing or downed). It hid from the fight, so it becomes a Snoblin.</summary>
        LastOneLeft,

        /// <summary>Nobody left: every fighter died or evolved.</summary>
        Wiped,

        /// <summary>Two or more left and standing, but none able to fight (incapable of violence). They all hid: all become Snoblins.</summary>
        OnlyHidersLeft,

        /// <summary>Two or more left, all downed. Nobody can finish anyone.</summary>
        AllDown,

        /// <summary>Safety net for fighters that cannot reach each other.</summary>
        TimedOut,
    }

    /// <summary>Game-free numbers for the Nobby Nobby Rumble. Linked into the unit test project.</summary>
    public static class RumbleRules
    {
        public const int MinNoblins = 25;

        /// <summary>One in-game day.</summary>
        public const int MaxDurationTicks = 60000;

        /// <summary>Outcomes where every survivor is made a Snoblin for hiding from the fight.</summary>
        public static bool PunishesSurvivors(RumbleOutcome outcome) =>
            outcome == RumbleOutcome.LastOneLeft || outcome == RumbleOutcome.OnlyHidersLeft;

        public static bool EnoughNoblins(int eligible) => eligible >= MinNoblins;

        /// <param name="alive">Fighters still in the rumble, downed included. Winners and the dead are out.</param>
        /// <param name="standing">Of those, the ones not downed.</param>
        /// <param name="ableStanding">Of the standing, the ones capable of violence.</param>
        public static RumbleOutcome Resolve(int alive, int standing, int ableStanding, int elapsedTicks)
        {
            if (alive <= 0)
            {
                return RumbleOutcome.Wiped;
            }
            if (alive == 1)
            {
                return RumbleOutcome.LastOneLeft;
            }
            if (standing <= 0)
            {
                return RumbleOutcome.AllDown;
            }
            if (ableStanding <= 0)
            {
                return RumbleOutcome.OnlyHidersLeft;
            }
            return elapsedTicks >= MaxDurationTicks ? RumbleOutcome.TimedOut : RumbleOutcome.Continue;
        }
    }
}
