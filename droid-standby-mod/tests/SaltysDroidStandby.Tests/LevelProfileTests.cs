using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class LevelProfileTests
    {
        [Theory]
        [InlineData(StandbyLevel.Normal, 1f, 1f)]
        [InlineData(StandbyLevel.PowerSave, 0.125f, 0.125f)]
        [InlineData(StandbyLevel.Standby, 0f, 0f)]
        public void Factors_matchRevision3(StandbyLevel level, float move, float look)
        {
            Assert.Equal(move, LevelProfile.Movement(level));
            Assert.Equal(look, LevelProfile.Look(level));
        }

        // User request 2026-10-08: holding Q (throw) is capped like the rest of movement.
        [Theory]
        [InlineData(StandbyLevel.Normal, 1f)]
        [InlineData(StandbyLevel.PowerSave, 0.125f)]
        [InlineData(StandbyLevel.Standby, 0f)]
        public void Throw_cappedLikeMovement(StandbyLevel level, float expected)
        {
            Assert.Equal(expected, LevelProfile.Throw(level));
        }

        [Theory]
        [InlineData(StandbyLevel.Normal, false)]
        [InlineData(StandbyLevel.PowerSave, false)]
        [InlineData(StandbyLevel.Standby, true)]
        public void Standby_blocksEverything_PowerSave_nothing(StandbyLevel level, bool blocked)
        {
            Assert.Equal(blocked, LevelProfile.BlocksJetpack(level));
            Assert.Equal(blocked, LevelProfile.BlocksWorldInteraction(level));
            Assert.Equal(blocked, LevelProfile.BlocksInventory(level));
            Assert.Equal(blocked, LevelProfile.BlocksNightVision(level));
        }

        // Ctrl/Alt mouse mode reaches the world on its own path; blocked in Standby and while the
        // Standby menu is open (clicks on Start/Cancel must not reach a switch behind the window).
        [Theory]
        [InlineData(StandbyLevel.Normal, false, false)]
        [InlineData(StandbyLevel.PowerSave, false, false)]
        [InlineData(StandbyLevel.Standby, false, true)]
        [InlineData(StandbyLevel.Normal, true, true)]
        [InlineData(StandbyLevel.PowerSave, true, true)]
        public void MouseWorld_blockedInStandbyOrWhileMenuOpen(StandbyLevel level, bool menuOpen, bool expected)
        {
            Assert.Equal(expected, LevelProfile.BlocksMouseWorld(level, menuOpen));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [InlineData(255, false)]
        public void IsValidWire_rejectsUnknownLevels(byte b, bool expected)
        {
            Assert.Equal(expected, LevelProfile.IsValidWire(b));
        }
    }
}
