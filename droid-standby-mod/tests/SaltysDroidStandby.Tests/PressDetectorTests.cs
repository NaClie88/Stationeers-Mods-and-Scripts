using Xunit;

namespace SaltysDroidStandby.Tests
{
    // Revision 3: no double tap, so a tap fires on release, at once.
    public class PressDetectorTests
    {
        private static PressDetector Make() => new PressDetector(3f);

        [Fact]
        public void Tap_firesOnRelease_immediately()
        {
            var d = Make();
            Assert.Equal(Gesture.None, d.Update(true, 0f));
            Assert.Equal(Gesture.Tap, d.Update(false, 0.1f));
            Assert.Equal(Gesture.None, d.Update(false, 1f));
        }

        [Fact]
        public void Hold_firesLongPressOnce_andNothingOnRelease()
        {
            var d = Make();
            d.Update(true, 0f);
            Assert.Equal(Gesture.None, d.Update(true, 2.9f));
            Assert.Equal(Gesture.LongPress, d.Update(true, 3f));
            Assert.Equal(Gesture.None, d.Update(true, 5f));
            Assert.Equal(Gesture.None, d.Update(false, 5.1f));
        }

        [Fact]
        public void TwoQuickTaps_areTwoTaps()
        {
            var d = Make();
            d.Update(true, 0f);
            Assert.Equal(Gesture.Tap, d.Update(false, 0.1f));
            d.Update(true, 0.2f);
            Assert.Equal(Gesture.Tap, d.Update(false, 0.3f));
        }

        [Fact]
        public void ThresholdChange_appliesToNextPress()
        {
            var d = new PressDetector(3f) { LongPressSeconds = 1f };
            d.Update(true, 0f);
            Assert.Equal(Gesture.LongPress, d.Update(true, 1f));
        }

        [Fact]
        public void NoInput_isNone()
        {
            var d = Make();
            Assert.Equal(Gesture.None, d.Update(false, 0f));
            Assert.Equal(Gesture.None, d.Update(false, 5f));
        }
    }
}
