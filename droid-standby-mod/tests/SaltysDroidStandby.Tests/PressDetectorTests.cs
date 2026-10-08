using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class PressDetectorTests
    {
        private static PressDetector Make() => new PressDetector(0.35f, 3f);

        [Fact]
        public void SingleTap_firesOnlyAfterWindowExpires()
        {
            var d = Make();
            Assert.Equal(Gesture.None, d.Update(true, 0f));
            Assert.Equal(Gesture.None, d.Update(false, 0.1f));
            Assert.Equal(Gesture.None, d.Update(false, 0.3f));   // still inside window
            Assert.Equal(Gesture.SingleTap, d.Update(false, 0.46f));
            Assert.Equal(Gesture.None, d.Update(false, 1f));     // fires once
        }

        [Fact]
        public void SecondPressInsideWindow_isDoubleTap_onPress()
        {
            var d = Make();
            d.Update(true, 0f);
            d.Update(false, 0.1f);
            Assert.Equal(Gesture.DoubleTap, d.Update(true, 0.3f));
            Assert.Equal(Gesture.None, d.Update(false, 0.4f));   // release swallowed
            Assert.Equal(Gesture.None, d.Update(false, 2f));     // no trailing SingleTap
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
            Assert.Equal(Gesture.None, d.Update(false, 9f));
        }

        [Fact]
        public void ImmediateTap_firesSingleTapOnRelease()
        {
            var d = Make();
            d.ImmediateTap = true;
            d.Update(true, 0f);
            Assert.Equal(Gesture.SingleTap, d.Update(false, 0.1f));
            Assert.Equal(Gesture.None, d.Update(false, 1f));
        }

        [Fact]
        public void ImmediateTap_holdStillLongPresses()
        {
            var d = Make();
            d.ImmediateTap = true;
            d.Update(true, 0f);
            Assert.Equal(Gesture.LongPress, d.Update(true, 3f));
            Assert.Equal(Gesture.None, d.Update(false, 3.2f));
        }

        [Fact]
        public void SlowSecondPress_isTwoSingleTaps()
        {
            var d = Make();
            d.Update(true, 0f);
            d.Update(false, 0.1f);
            Assert.Equal(Gesture.SingleTap, d.Update(false, 0.5f));
            d.Update(true, 1f);
            d.Update(false, 1.1f);
            Assert.Equal(Gesture.SingleTap, d.Update(false, 1.5f));
        }

        // User report: a double tap while in Standby woke on tap 1 (immediate), then tap 2 landed
        // in Power Save. The second half of that double tap must be ignored.
        [Fact]
        public void TapRightAfterImmediateWake_isIgnored()
        {
            var d = Make();
            d.ImmediateTap = true;
            d.Update(true, 0f);
            Assert.Equal(Gesture.SingleTap, d.Update(false, 0.1f)); // wakes
            d.ImmediateTap = false;                                  // now Normal
            Assert.Equal(Gesture.None, d.Update(true, 0.3f));
            Assert.Equal(Gesture.None, d.Update(false, 0.4f));
            Assert.Equal(Gesture.None, d.Update(false, 1.5f));      // no late SingleTap either
        }

        [Fact]
        public void TapAfterWindow_followingImmediateWake_counts()
        {
            var d = Make();
            d.ImmediateTap = true;
            d.Update(true, 0f);
            d.Update(false, 0.1f);
            d.ImmediateTap = false;
            d.Update(true, 0.6f);
            d.Update(false, 0.7f);
            Assert.Equal(Gesture.SingleTap, d.Update(false, 1.1f));
        }

        [Fact]
        public void HoldStartedRightAfterImmediateWake_stillLongPresses()
        {
            var d = Make();
            d.ImmediateTap = true;
            d.Update(true, 0f);
            d.Update(false, 0.1f);
            d.ImmediateTap = false;
            d.Update(true, 0.3f);
            Assert.Equal(Gesture.LongPress, d.Update(true, 3.3f));
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
