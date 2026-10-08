using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class PressDetectorTests
    {
        [Fact]
        public void QuickPressAndRelease_isTap()
        {
            var d = new PressDetector(0.6f);
            Assert.Equal(Gesture.None, d.Update(true, 0f));
            Assert.Equal(Gesture.None, d.Update(true, 0.2f));
            Assert.Equal(Gesture.Tap, d.Update(false, 0.3f));
        }

        [Fact]
        public void Hold_firesLongPressOnce_whileStillHeld_andNoTapOnRelease()
        {
            var d = new PressDetector(0.6f);
            d.Update(true, 0f);
            Assert.Equal(Gesture.LongPress, d.Update(true, 0.6f));
            Assert.Equal(Gesture.None, d.Update(true, 1.5f));
            Assert.Equal(Gesture.None, d.Update(false, 2f));
        }

        [Fact]
        public void NoInput_isNone()
        {
            var d = new PressDetector(0.6f);
            Assert.Equal(Gesture.None, d.Update(false, 0f));
            Assert.Equal(Gesture.None, d.Update(false, 5f));
        }

        [Fact]
        public void ThresholdChange_appliesToNextPress()
        {
            var d = new PressDetector(0.6f) { LongPressSeconds = 1.0f };
            d.Update(true, 0f);
            Assert.Equal(Gesture.None, d.Update(true, 0.8f));
            Assert.Equal(Gesture.LongPress, d.Update(true, 1.0f));
        }
    }
}
