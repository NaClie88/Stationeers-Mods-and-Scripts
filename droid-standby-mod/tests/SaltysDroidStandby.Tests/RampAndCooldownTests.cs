using Xunit;

namespace SaltysDroidStandby.Tests
{
    // Revision 3 (user, 2026-10-08): drain eases DOWN over the ramp; going up is immediate.
    public class DrainRampTests
    {
        [Theory]
        [InlineData(1f, 0.5f, 0f, 1f)]      // just entered Power Save: still full drain
        [InlineData(1f, 0.5f, 2.5f, 0.75f)] // halfway
        [InlineData(1f, 0.5f, 5f, 0.5f)]    // done
        [InlineData(1f, 0.5f, 60f, 0.5f)]   // stays at target
        [InlineData(0.5f, 0f, 2.5f, 0.25f)] // Power Save -> Standby, halfway
        [InlineData(0f, 0.5f, 0f, 0.5f)]    // waking into Power Save: higher drain at once
        [InlineData(0.5f, 1f, 0f, 1f)]      // back to Normal: at once
        [InlineData(1f, 0f, -1f, 1f)]       // clock oddity: never below 0 elapsed
        public void Factor_easesDown_jumpsUp(float from, float to, float elapsed, float expected)
        {
            Assert.Equal(expected, DrainRamp.Factor(from, to, elapsed, 5f), 3);
        }

        [Fact]
        public void ZeroRamp_isImmediate()
        {
            Assert.Equal(0f, DrainRamp.Factor(1f, 0f, 0f, 0f), 3);
        }

        [Theory]
        [InlineData(1f, 0.5f, 0f, 5f)]
        [InlineData(1f, 0.5f, 3.2f, 1.8f)]
        [InlineData(1f, 0.5f, 6f, 0f)]
        [InlineData(0f, 0.5f, 0f, 0f)] // going up: no "powering down" message
        public void SecondsLeft_onlyWhileEasingDown(float from, float to, float elapsed, float expected)
        {
            Assert.Equal(expected, DrainRamp.SecondsLeft(from, to, elapsed, 5f), 3);
        }
    }

    public class ToggleCooldownTests
    {
        [Theory]
        [InlineData(10f, 10f, 5f, 5f)]
        [InlineData(10f, 13f, 5f, 2f)]
        [InlineData(10f, 15f, 5f, 0f)]
        [InlineData(10f, 99f, 5f, 0f)]
        public void Remaining(float lastChange, float now, float cooldown, float expected)
        {
            Assert.Equal(expected, ToggleCooldown.Remaining(lastChange, now, cooldown), 3);
        }

        [Fact]
        public void NeverChanged_isReady()
        {
            Assert.Equal(0f, ToggleCooldown.Remaining(float.NegativeInfinity, 0f, 5f), 3);
        }
    }
}
