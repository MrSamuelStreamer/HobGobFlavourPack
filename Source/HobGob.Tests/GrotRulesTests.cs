using HobGob.Grot;
using Xunit;

namespace HobGob.Tests
{
    public class GrotRulesTests
    {
        [Theory]
        [InlineData(5f, -5f)]
        [InlineData(-3f, 3f)]
        [InlineData(0f, 0f)]
        public void FlipBeauty_Negates(float beauty, float expected)
        {
            Assert.Equal(expected, GrotRules.FlipBeauty(beauty));
        }

        [Fact]
        public void SurgeryFactor_FilthHelps_SterileHurts()
        {
            Assert.Equal(1.0f, GrotRules.SurgeryFactor(0f), 3);
            Assert.True(GrotRules.SurgeryFactor(-1.1f) > 1.0f);
            Assert.True(GrotRules.SurgeryFactor(GrotRules.SterileCleanliness) < 1.0f);
        }

        [Fact]
        public void SurgeryFactor_ClampsAtEnds()
        {
            Assert.Equal(1.15f, GrotRules.SurgeryFactor(-50f), 3);
            Assert.Equal(0.85f, GrotRules.SurgeryFactor(50f), 3);
        }

        [Fact]
        public void SurgeryFactor_InterpolatesBetweenPoints()
        {
            // Halfway between (0, 1.0) and (0.4, 0.9).
            Assert.Equal(0.95f, GrotRules.SurgeryFactor(0.2f), 3);
        }

        [Fact]
        public void InfectionFactor_FilthProtects_SterileExposes()
        {
            Assert.Equal(0.5f, GrotRules.InfectionFactor(0f), 3);
            Assert.True(GrotRules.InfectionFactor(-2f) < 0.5f);
            Assert.True(GrotRules.InfectionFactor(1f) > 0.5f);
            Assert.Equal(0.2f, GrotRules.InfectionFactor(-50f), 3);
            Assert.Equal(0.75f, GrotRules.InfectionFactor(50f), 3);
        }

        [Theory]
        [InlineData(1.0f, GrotRules.TooCleanStage)]
        [InlineData(0.4f, GrotRules.TooCleanStage)]
        [InlineData(0.0f, GrotRules.NoRoomThought)]
        [InlineData(-0.39f, GrotRules.NoRoomThought)]
        [InlineData(-0.4f, GrotRules.ProperGrottyStage)]
        [InlineData(-3f, GrotRules.ProperGrottyStage)]
        public void RoomThoughtStage_ByCleanliness(float cleanliness, int expected)
        {
            Assert.Equal(expected, GrotRules.RoomThoughtStage(cleanliness));
        }
    }
}
