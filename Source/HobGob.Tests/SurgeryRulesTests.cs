using HobGob.Surgery;
using Xunit;

namespace HobGob.Tests
{
    public class SurgeryRulesTests
    {
        [Theory]
        [InlineData(0f, 0f, true, GoblinSurgeryResult.Success)]
        [InlineData(0.249f, 0.9f, false, GoblinSurgeryResult.Success)]
        [InlineData(0.25f, 0f, true, GoblinSurgeryResult.BadlyAttached)]
        [InlineData(0.9f, 0.499f, true, GoblinSurgeryResult.BadlyAttached)]
        [InlineData(0.9f, 0.5f, true, GoblinSurgeryResult.Failure)]
        [InlineData(0.25f, 0f, false, GoblinSurgeryResult.Failure)]
        public void Roll(float successRoll, float attachRoll, bool installs, GoblinSurgeryResult expected)
        {
            Assert.Equal(expected, SurgeryRules.Roll(successRoll, attachRoll, installs));
        }

        [Fact]
        public void Roll_OddsOverEvenGrid()
        {
            int success = 0, badly = 0, fail = 0;
            const int steps = 100;
            for (int i = 0; i < steps; i++)
            {
                for (int j = 0; j < steps; j++)
                {
                    switch (SurgeryRules.Roll(i / (float)steps, j / (float)steps, true))
                    {
                        case GoblinSurgeryResult.Success: success++; break;
                        case GoblinSurgeryResult.BadlyAttached: badly++; break;
                        default: fail++; break;
                    }
                }
            }
            Assert.Equal(2500, success);
            Assert.Equal(3750, badly);
            Assert.Equal(3750, fail);
        }
    }
}
