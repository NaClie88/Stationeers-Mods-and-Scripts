using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class LevelProfileTests
    {
        [Theory]
        [InlineData(StandbyLevel.Normal, 1f, 1f)]
        [InlineData(StandbyLevel.PowerSave, 0.125f, 0.125f)]
        [InlineData(StandbyLevel.Standby, 0f, 0.0625f)]
        [InlineData(StandbyLevel.DeepStandby, 0f, 0f)]
        public void Factors_matchRevision2(StandbyLevel level, float move, float look)
        {
            Assert.Equal(move, LevelProfile.Movement(level));
            Assert.Equal(look, LevelProfile.Look(level));
        }

        [Theory]
        [InlineData(StandbyLevel.Normal, false, false, false)]
        [InlineData(StandbyLevel.PowerSave, false, false, false)]
        [InlineData(StandbyLevel.Standby, true, true, false)]   // inventory stays usable (Ctrl/Alt mouse)
        [InlineData(StandbyLevel.DeepStandby, true, true, true)]
        public void Blocks_matchRevision2(StandbyLevel level, bool jetpack, bool world, bool inventory)
        {
            Assert.Equal(jetpack, LevelProfile.BlocksJetpack(level));
            Assert.Equal(world, LevelProfile.BlocksWorldInteraction(level));
            Assert.Equal(inventory, LevelProfile.BlocksInventory(level));
        }

        [Theory]
        [InlineData(StandbyLevel.Normal, false)]
        [InlineData(StandbyLevel.PowerSave, false)]
        [InlineData(StandbyLevel.Standby, false)]
        [InlineData(StandbyLevel.DeepStandby, true)]
        public void NightVision_offOnlyInDeep(StandbyLevel level, bool expected)
        {
            Assert.Equal(expected, LevelProfile.BlocksNightVision(level));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(255, false)]
        public void IsValidWire_rejectsUnknownLevels(byte b, bool expected)
        {
            Assert.Equal(expected, LevelProfile.IsValidWire(b));
        }
    }
}
