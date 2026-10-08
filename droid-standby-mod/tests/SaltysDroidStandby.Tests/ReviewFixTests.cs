using System.Collections.Generic;
using Xunit;

namespace SaltysDroidStandby.Tests
{
    // Tests added in the final-review fix pass (findings C2, C3, I3, I4, I5).
    public class ReviewFixTests
    {
        // --- StandbyRules.ShouldClear: one rule for "standby can't apply" (C3, I3, I5) ---

        [Fact]
        public void Clear_aliveDroidAwake_isFalse()
        {
            Assert.False(StandbyRules.ShouldClear(true, true, false, false, false, true));
        }

        [Fact]
        public void Clear_unconscious_isTrue()   // I3: the floor must never hold a droid unconscious
        {
            Assert.True(StandbyRules.ShouldClear(true, false, false, false, false, true));
        }

        [Fact]
        public void Clear_inLifeSuspender_isTrue()
        {
            Assert.True(StandbyRules.ShouldClear(true, true, false, true, false, true));
        }

        [Fact]
        public void Clear_sleeping_isTrue()
        {
            Assert.True(StandbyRules.ShouldClear(true, true, true, false, false, true));
        }

        [Fact]
        public void Clear_notADroid_isTrue()
        {
            Assert.True(StandbyRules.ShouldClear(false, true, false, false, false, true));
        }

        [Fact]
        public void Clear_multiplayerOwnerDisconnected_isTrue()   // I5
        {
            Assert.True(StandbyRules.ShouldClear(true, true, false, false, true, false));
        }

        [Fact]
        public void Clear_singlePlayer_ignoresOnlineFlag()
        {
            Assert.False(StandbyRules.ShouldClear(true, true, false, false, false, false));
        }

        // --- Safety net gate (C3) ---

        [Fact]
        public void SafetyNet_cannotStandby_doesNotTrigger()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.05f, 600f, 0.10f, 60f, false, false));
        }

        [Fact]
        public void SafetyNet_canStandby_triggers()
        {
            Assert.True(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.05f, 600f, 0.10f, 60f, false, true));
        }

        // --- Only a battery-low wake suppresses the safety net (I4) ---

        private static WorldSnapshot Night() => new WorldSnapshot
        {
            LightPercent = 0f, WindPercent = 10f, Outdoors = true, BatteryRatio = 0.5f,
            PressureKpa = 100f, TemperatureK = 290f,
        };

        [Fact]
        public void BatteryLowWake_isFlagged()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night());
            var low = Night(); low.BatteryRatio = 0.04f;
            string r = null;
            for (int i = 0; i < 3; i++) r = e.Check(low);
            Assert.NotNull(r);
            Assert.True(e.LastWakeWasBatteryLow);
        }

        [Fact]
        public void SunriseWake_isNotBatteryLow()
        {
            var e = new WakeEvaluator(WakeCondition.Light, new WakeThresholds(), Night());
            var dawn = Night(); dawn.LightPercent = 50f;
            string r = null;
            for (int i = 0; i < 3; i++) r = e.Check(dawn);
            Assert.NotNull(r);
            Assert.False(e.LastWakeWasBatteryLow);
        }

        // --- Client battery reading uses the synced percentage (C2) ---

        [Fact]
        public void ClientCell_usesSyncedPercentage()
        {
            var cell = BatteryMath.Cell(0f, 1000f, 75, true);
            Assert.Equal(750f, cell.Key, 3);
            Assert.Equal(1000f, cell.Value, 3);
        }

        [Fact]
        public void ServerCell_usesStoredEnergy()
        {
            var cell = BatteryMath.Cell(420f, 1000f, 75, false);
            Assert.Equal(420f, cell.Key, 3);
        }
    }
}
