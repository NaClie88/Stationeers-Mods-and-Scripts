using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class SafetyNetLogicTests
    {
        [Fact]
        public void LowAndIdle_triggers()
        {
            Assert.True(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.09f, 61f, 0.10f, 60f, false, true));
        }

        [Fact]
        public void LowAndIdle_inPowerSave_triggers()
        {
            Assert.True(SafetyNetLogic.ShouldTrigger(StandbyLevel.PowerSave, 0.09f, 61f, 0.10f, 60f, false, true));
        }

        [Fact]
        public void AlreadyDeep_doesNotTrigger()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Deep, 0.01f, 600f, 0.10f, 60f, false, true));
        }

        [Fact]
        public void NotIdleLongEnough_doesNotTrigger()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.05f, 30f, 0.10f, 60f, false, true));
        }

        [Fact]
        public void BatteryAboveThreshold_doesNotTrigger()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.5f, 600f, 0.10f, 60f, false, true));
        }

        [Fact]
        public void SuppressedAfterAutoWake_untilInput()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.04f, 600f, 0.10f, 60f, true, true));
        }

        [Fact]
        public void Solo_singlePlayer()
        {
            Assert.True(SafetyNetLogic.IsEffectivelySolo(false, false, new bool[0]));
        }

        [Fact]
        public void Solo_hostAlone_withOwnEntry()
        {
            Assert.True(SafetyNetLogic.IsEffectivelySolo(true, true, new[] { true }));
        }

        [Fact]
        public void Solo_hostAlone_emptyList()
        {
            Assert.True(SafetyNetLogic.IsEffectivelySolo(true, true, new bool[0]));
        }

        [Fact]
        public void NotSolo_hostWithAGuest()
        {
            Assert.False(SafetyNetLogic.IsEffectivelySolo(true, true, new[] { true, false }));
        }

        [Fact]
        public void NotSolo_clientOnDedicatedServer()
        {
            Assert.False(SafetyNetLogic.IsEffectivelySolo(true, false, new[] { false }));
        }
    }
}
