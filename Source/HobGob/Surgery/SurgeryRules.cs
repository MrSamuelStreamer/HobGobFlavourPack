namespace HobGob.Surgery
{
    public enum GoblinSurgeryResult
    {
        Success,
        BadlyAttached,
        Failure,
    }

    /// <summary>Game-free goblin surgery odds. Linked into the unit test project.</summary>
    public static class SurgeryRules
    {
        /// <summary>Flat chance any surgery on a goblin succeeds, whatever the surgeon, bed or medicine.</summary>
        public const float SuccessChance = 0.25f;

        /// <summary>When an install fails, chance the part goes in badly attached instead of the normal failure.</summary>
        public const float BadlyAttachedChance = 0.5f;

        /// <param name="successRoll">Uniform random value in [0, 1).</param>
        /// <param name="attachRoll">A second, independent uniform random value in [0, 1).</param>
        /// <param name="installsPart">The surgery installs a limb or organ.</param>
        public static GoblinSurgeryResult Roll(float successRoll, float attachRoll, bool installsPart)
        {
            if (successRoll < SuccessChance)
            {
                return GoblinSurgeryResult.Success;
            }
            if (installsPart && attachRoll < BadlyAttachedChance)
            {
                return GoblinSurgeryResult.BadlyAttached;
            }
            return GoblinSurgeryResult.Failure;
        }
    }
}
