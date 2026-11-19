namespace HobGob.Grot
{
    /// <summary>
    /// Game-free rules for the grot lover gene. Linked into the unit test project.
    /// Inputs are the vanilla room Cleanliness score: about -1.1 or less is "very dirty", 0.4 or more is "sterile".
    /// </summary>
    public static class GrotRules
    {
        /// <summary>Vanilla's "sterile" cleanliness stage starts here.</summary>
        public const float SterileCleanliness = 0.4f;

        /// <summary>Vanilla's "dirty" cleanliness stage ends here. At or below this a goblin feels at home.</summary>
        public const float GrottyCleanliness = -0.4f;

        public const int NoRoomThought = -1;
        public const int TooCleanStage = 0;
        public const int ProperGrottyStage = 1;

        // (cleanliness, factor). Mirrors vanilla's SurgerySuccessChanceCleanlinessFactor, the other way round.
        private static readonly (float x, float y)[] SurgeryPoints =
        {
            (-5f, 1.15f), (-1.1f, 1.10f), (0f, 1.0f), (SterileCleanliness, 0.9f), (1f, 0.85f),
        };

        // (cleanliness, factor). Vanilla gives 1.0 when very dirty, 0.5 when clean and 0.2 when sterile.
        private static readonly (float x, float y)[] InfectionPoints =
        {
            (-5f, 0.2f), (-1.1f, 0.3f), (0f, 0.5f), (SterileCleanliness, 0.65f), (1f, 0.75f),
        };

        /// <summary>Ugly and filthy look good, pretty looks bad.</summary>
        public static float FlipBeauty(float beauty) => -beauty;

        /// <summary>Surgery success factor from the room for a grot-loving surgeon. Replaces vanilla's cleanliness factor.</summary>
        public static float SurgeryFactor(float cleanliness) => Evaluate(SurgeryPoints, cleanliness);

        /// <summary>Infection chance factor from the tend room for a grot-loving patient. Replaces vanilla's factor.</summary>
        public static float InfectionFactor(float cleanliness) => Evaluate(InfectionPoints, cleanliness);

        /// <summary>Thought stage for a grot lover's bedroom, or <see cref="NoRoomThought"/> when it is just ordinary.</summary>
        public static int RoomThoughtStage(float cleanliness)
        {
            if (cleanliness >= SterileCleanliness)
            {
                return TooCleanStage;
            }
            return cleanliness <= GrottyCleanliness ? ProperGrottyStage : NoRoomThought;
        }

        /// <summary>Piecewise-linear lookup, clamped at both ends (same as vanilla SimpleCurve).</summary>
        private static float Evaluate((float x, float y)[] points, float x)
        {
            if (x <= points[0].x)
            {
                return points[0].y;
            }
            for (int i = 1; i < points.Length; i++)
            {
                if (x <= points[i].x)
                {
                    (float x0, float y0) = points[i - 1];
                    (float x1, float y1) = points[i];
                    return y0 + (y1 - y0) * (x - x0) / (x1 - x0);
                }
            }
            return points[points.Length - 1].y;
        }
    }
}
