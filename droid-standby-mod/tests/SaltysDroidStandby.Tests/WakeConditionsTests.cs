using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class WakeConditionsTests
    {
        private static WorldSnapshot Night() => new WorldSnapshot
        {
            LightPercent = 0f, WindPercent = 10f, StormActive = false, SolarStormActive = false,
            Outdoors = true, BatteryRatio = 0.5f, BruteBurn = 0f, PressureKpa = 100f, TemperatureK = 290f,
        };

        private static string Run(WakeEvaluator e, WorldSnapshot s, int times)
        {
            string r = null;
            for (int i = 0; i < times; i++) r = e.Check(s);
            return r;
        }

        [Fact]
        public void Light_wakesAfterThreeConsecutiveBrightChecks()
        {
            var e = new WakeEvaluator(WakeCondition.Light, new WakeThresholds(), Night());
            var dawn = Night(); dawn.LightPercent = 25f;
            Assert.Null(e.Check(dawn));
            Assert.Null(e.Check(dawn));
            Assert.Contains("light", e.Check(dawn));
        }

        [Fact]
        public void Light_flicker_resetsTheCount()
        {
            var e = new WakeEvaluator(WakeCondition.Light, new WakeThresholds(), Night());
            var dawn = Night(); dawn.LightPercent = 25f;
            e.Check(dawn); e.Check(dawn);
            Assert.Null(e.Check(Night()));
            Assert.Null(e.Check(dawn));
        }

        [Fact]
        public void Light_alreadyBright_atEntry_doesNotWakeUntilItDipsAndRecovers()
        {
            var noon = Night(); noon.LightPercent = 90f;
            var e = new WakeEvaluator(WakeCondition.Light, new WakeThresholds(), noon);
            Assert.Null(Run(e, noon, 10));
            e.Check(Night());
            Assert.Contains("light", Run(e, noon, 3));
        }

        [Fact]
        public void Unselected_conditions_neverFire()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night());
            var bright = Night(); bright.LightPercent = 100f; bright.WindPercent = 100f;
            Assert.Null(Run(e, bright, 10));
        }

        [Fact]
        public void Wind_wakesWhenAboveThreshold()
        {
            var e = new WakeEvaluator(WakeCondition.Wind, new WakeThresholds(), Night());
            var gusty = Night(); gusty.WindPercent = 60f;
            Assert.Contains("wind", Run(e, gusty, 3));
        }

        [Fact]
        public void BatteryCharged_wakesAt90Percent()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night());
            var full = Night(); full.BatteryRatio = 0.92f;
            Assert.Contains("charged", Run(e, full, 3));
        }

        [Fact]
        public void BatteryCharged_alreadyFull_atEntry_doesNotWake()
        {
            var full = Night(); full.BatteryRatio = 1f;
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), full);
            Assert.Null(Run(e, full, 10));
        }

        [Fact]
        public void BatteryLow_wakesAt5Percent()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night());
            var low = Night(); low.BatteryRatio = 0.04f;
            Assert.Contains("low", Run(e, low, 3));
        }

        // Final review I5: a silent Standby wake at 5 % would return an AFK droid to x4 drain
        // and let it die sooner. Standby keeps "charged" but never wakes on "low".
        [Fact]
        public void BatteryLow_ignored_whenWakeOnLowIsOff()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night(), wakeOnLow: false);
            var low = Night(); low.BatteryRatio = 0.04f;
            Assert.Null(Run(e, low, 5));
        }

        [Fact]
        public void BatteryCharged_stillWakes_whenWakeOnLowIsOff()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night(), wakeOnLow: false);
            var full = Night(); full.BatteryRatio = 0.92f;
            Assert.Contains("charged", Run(e, full, 3));
        }

        [Fact]
        public void Storm_startOutdoors_wakes()
        {
            var e = new WakeEvaluator(WakeCondition.Storm, new WakeThresholds(), Night());
            var storm = Night(); storm.StormActive = true;
            Assert.Contains("storm", Run(e, storm, 3));
        }

        [Fact]
        public void Storm_end_wakes()
        {
            var stormy = Night(); stormy.StormActive = true;
            var e = new WakeEvaluator(WakeCondition.Storm, new WakeThresholds(), stormy);
            Assert.Contains("storm", Run(e, Night(), 3));
        }

        [Fact]
        public void SolarStorm_start_wakes()
        {
            var e = new WakeEvaluator(WakeCondition.Storm, new WakeThresholds(), Night());
            var solar = Night(); solar.StormActive = true; solar.SolarStormActive = true;
            Assert.Contains("solar", Run(e, solar, 3));
        }

        [Fact]
        public void Storm_indoors_isIgnored()
        {
            var e = new WakeEvaluator(WakeCondition.Storm, new WakeThresholds(), Night());
            var inside = Night(); inside.StormActive = true; inside.Outdoors = false;
            Assert.Null(Run(e, inside, 10));
        }

        [Fact]
        public void Danger_damage_wakesImmediately()
        {
            var e = new WakeEvaluator(WakeCondition.Danger, new WakeThresholds(), Night());
            var hurt = Night(); hurt.BruteBurn = 5f;
            Assert.Contains("damage", e.Check(hurt));
        }

        [Fact]
        public void Danger_pressureSwing_wakesImmediately()
        {
            var e = new WakeEvaluator(WakeCondition.Danger, new WakeThresholds(), Night());
            var breach = Night(); breach.PressureKpa = 40f;
            Assert.Contains("pressure", e.Check(breach));
        }

        [Fact]
        public void Danger_temperatureOutOfBand_wakesAfterThreeChecks()
        {
            var e = new WakeEvaluator(WakeCondition.Danger, new WakeThresholds(), Night());
            var cold = Night(); cold.TemperatureK = 150f;
            Assert.Null(e.Check(cold));
            Assert.Null(e.Check(cold));
            Assert.Contains("temperature", e.Check(cold));
        }

        [Fact]
        public void Danger_temperature_alreadyOutOfBand_atEntry_doesNotWake()
        {
            var cold = Night(); cold.TemperatureK = 150f;
            var e = new WakeEvaluator(WakeCondition.Danger, new WakeThresholds(), cold);
            Assert.Null(Run(e, cold, 10));
        }
    }
}
