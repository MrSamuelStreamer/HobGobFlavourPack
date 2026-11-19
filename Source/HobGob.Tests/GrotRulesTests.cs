using System;
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
        [InlineData(0.5f, 0.5f)]
        [InlineData(-0.5f, -0.5f)]
        [InlineData(1f, 1f)]
        public void ImpressivenessFactor_LinearInsideUnitRange(float input, float expected)
        {
            Assert.Equal(expected, GrotRules.ImpressivenessFactor(input), 4);
        }

        [Fact]
        public void ImpressivenessFactor_LogarithmicOutside()
        {
            Assert.Equal(1f + (float)Math.Log(3), GrotRules.ImpressivenessFactor(3f), 4);
            Assert.Equal(-1f - (float)Math.Log(3), GrotRules.ImpressivenessFactor(-3f), 4);
        }

        [Fact]
        public void GrotImpressiveness_FilthyUglyRoomBeatsCleanPrettyRoom()
        {
            float grotty = GrotRules.GrotImpressiveness(wealth: 1500f, beauty: -6f, space: 125f, cleanliness: -3f);
            float posh = GrotRules.GrotImpressiveness(wealth: 1500f, beauty: 6f, space: 125f, cleanliness: 0.6f);
            Assert.True(grotty > 50f, $"grotty={grotty}");
            Assert.True(posh < 0f, $"posh={posh}");
        }

        [Fact]
        public void GrotImpressiveness_TinyRoomCappedBySpace()
        {
            float cramped = GrotRules.GrotImpressiveness(wealth: 100000f, beauty: -50f, space: 5f, cleanliness: -5f);
            float roomy = GrotRules.GrotImpressiveness(wealth: 100000f, beauty: -50f, space: 125f, cleanliness: -5f);
            Assert.True(cramped < roomy);
        }

        [Theory]
        [InlineData(-3f, 1)]
        [InlineData(-2f, 1)]
        [InlineData(-1.5f, 0)]
        [InlineData(-1f, 0)]
        [InlineData(0f, -1)]
        [InlineData(2f, -1)]
        public void GrotPrettyStage_UglyLooksPretty(float beauty, int expected)
        {
            Assert.Equal(expected, GrotRules.GrotPrettyStage(beauty));
        }

        [Theory]
        [InlineData(3f, 1)]
        [InlineData(2f, 1)]
        [InlineData(1f, 0)]
        [InlineData(0f, -1)]
        [InlineData(-2f, -1)]
        public void GrotUglyStage_PrettyLooksUgly(float beauty, int expected)
        {
            Assert.Equal(expected, GrotRules.GrotUglyStage(beauty));
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
