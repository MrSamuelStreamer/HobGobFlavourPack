namespace HobGob.Surgery
{
    public enum GoblinSurgeryResult
    {
        Success,
        BadlyAttached,
        Failure,
    }

    public enum GoblinSurgeryKind
    {
        /// <summary>Any surgery that neither chops a part off nor installs one.</summary>
        Other,

        /// <summary>Chopping a part off or out of a goblin.</summary>
        Harvest,

        /// <summary>Installing a limb or organ that did not come off a goblin.</summary>
        Install,

        /// <summary>Installing a part of a kind that gets chopped off goblins.</summary>
        InstallHarvestedPart,
    }

    /// <summary>Game-free goblin surgery odds. Linked into the unit test project.</summary>
    public static class SurgeryRules
    {
        /// <summary>Flat chance a surgery on a goblin succeeds, whatever the surgeon, bed or medicine.</summary>
        public const float SuccessChance = 0.25f;

        /// <summary>Flat chance chopping a part off a goblin succeeds. Hacking bits off is the easy part.</summary>
        public const float HarvestSuccessChance = 0.75f;

        /// <summary>
        /// When an install fails, chance the part goes in anyway instead of the normal failure:
        /// badly attached for ordinary parts, properly for chopped-off goblin bits.
        /// </summary>
        public const float BadlyAttachedChance = 0.5f;

        /// <param name="successRoll">Uniform random value in [0, 1).</param>
        /// <param name="attachRoll">A second, independent uniform random value in [0, 1).</param>
        public static GoblinSurgeryResult Roll(float successRoll, float attachRoll, GoblinSurgeryKind kind)
        {
            float chance = kind == GoblinSurgeryKind.Harvest ? HarvestSuccessChance : SuccessChance;
            if (successRoll < chance)
            {
                return GoblinSurgeryResult.Success;
            }
            if (attachRoll < BadlyAttachedChance)
            {
                if (kind == GoblinSurgeryKind.Install)
                {
                    return GoblinSurgeryResult.BadlyAttached;
                }
                if (kind == GoblinSurgeryKind.InstallHarvestedPart)
                {
                    return GoblinSurgeryResult.Success;
                }
            }
            return GoblinSurgeryResult.Failure;
        }
    }
}
