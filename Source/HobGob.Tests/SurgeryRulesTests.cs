using HobGob.Surgery;
using Xunit;

namespace HobGob.Tests
{
    public class SurgeryRulesTests
    {
        [Theory]
        [InlineData(0f, 0f, GoblinSurgeryKind.Install, GoblinSurgeryResult.Success)]
        [InlineData(0.249f, 0.9f, GoblinSurgeryKind.Other, GoblinSurgeryResult.Success)]
        [InlineData(0.25f, 0f, GoblinSurgeryKind.Install, GoblinSurgeryResult.BadlyAttached)]
        [InlineData(0.9f, 0.499f, GoblinSurgeryKind.Install, GoblinSurgeryResult.BadlyAttached)]
        [InlineData(0.9f, 0.5f, GoblinSurgeryKind.Install, GoblinSurgeryResult.Failure)]
        [InlineData(0.25f, 0f, GoblinSurgeryKind.Other, GoblinSurgeryResult.Failure)]
        // Chopping a bit off works three goes in four and never leaves anything badly attached.
        [InlineData(0.749f, 0.9f, GoblinSurgeryKind.Harvest, GoblinSurgeryResult.Success)]
        [InlineData(0.75f, 0f, GoblinSurgeryKind.Harvest, GoblinSurgeryResult.Failure)]
        // A chopped-off bit that would have gone in badly attached goes in properly instead.
        [InlineData(0.249f, 0.9f, GoblinSurgeryKind.InstallHarvestedPart, GoblinSurgeryResult.Success)]
        [InlineData(0.25f, 0f, GoblinSurgeryKind.InstallHarvestedPart, GoblinSurgeryResult.Success)]
        [InlineData(0.9f, 0.499f, GoblinSurgeryKind.InstallHarvestedPart, GoblinSurgeryResult.Success)]
        [InlineData(0.9f, 0.5f, GoblinSurgeryKind.InstallHarvestedPart, GoblinSurgeryResult.Failure)]
        public void Roll(float successRoll, float attachRoll, GoblinSurgeryKind kind, GoblinSurgeryResult expected)
        {
            Assert.Equal(expected, SurgeryRules.Roll(successRoll, attachRoll, kind));
        }

        [Theory]
        [InlineData(GoblinSurgeryKind.Other, 2500, 0, 7500)]
        [InlineData(GoblinSurgeryKind.Harvest, 7500, 0, 2500)]
        [InlineData(GoblinSurgeryKind.Install, 2500, 3750, 3750)]
        [InlineData(GoblinSurgeryKind.InstallHarvestedPart, 6250, 0, 3750)]
        public void Roll_OddsOverEvenGrid(GoblinSurgeryKind kind, int expectedSuccess, int expectedBadly, int expectedFail)
        {
            int success = 0, badly = 0, fail = 0;
            const int steps = 100;
            for (int i = 0; i < steps; i++)
            {
                for (int j = 0; j < steps; j++)
                {
                    switch (SurgeryRules.Roll(i / (float)steps, j / (float)steps, kind))
                    {
                        case GoblinSurgeryResult.Success: success++; break;
                        case GoblinSurgeryResult.BadlyAttached: badly++; break;
                        default: fail++; break;
                    }
                }
            }
            Assert.Equal(expectedSuccess, success);
            Assert.Equal(expectedBadly, badly);
            Assert.Equal(expectedFail, fail);
        }
    }
}
