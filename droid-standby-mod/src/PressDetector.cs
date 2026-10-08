namespace SaltysDroidStandby
{
    // One key, three gestures (spec Revision 2):
    //  - SingleTap: press+release, then no second press within DoubleTapSeconds (fires when the
    //    window expires), or immediately on release when ImmediateTap is set (used in Standby
    //    and Deep Standby so waking never waits on the window).
    //  - DoubleTap: a second press inside the window; fires on that press, its release is swallowed.
    //  - LongPress: held for LongPressSeconds; fires once while held, nothing on release.
    // After an immediate tap (a wake), a tap that starts within DoubleTapSeconds is ignored:
    // it is the second half of a double tap aimed at a sleeping droid, not a new Power Save
    // request (user report). A hold started in that window still long-presses.
    public sealed class PressDetector
    {
        private bool _wasDown;
        private float _downAt;
        private bool _swallowRelease;
        private bool _pendingTap;
        private float _releasedAt;
        private float _ignoreTapUntil = float.NegativeInfinity;
        private bool _ignoreThisTap;

        public PressDetector(float doubleTapSeconds, float longPressSeconds)
        {
            DoubleTapSeconds = doubleTapSeconds;
            LongPressSeconds = longPressSeconds;
        }

        public float DoubleTapSeconds { get; set; }
        public float LongPressSeconds { get; set; }
        public bool ImmediateTap { get; set; }

        public Gesture Update(bool isDown, float now)
        {
            if (isDown && !_wasDown)
            {
                _wasDown = true;
                _downAt = now;
                _swallowRelease = false;
                _ignoreThisTap = now <= _ignoreTapUntil;
                if (_ignoreThisTap)
                {
                    _pendingTap = false;
                    return Gesture.None;
                }
                if (_pendingTap && !ImmediateTap && now - _releasedAt <= DoubleTapSeconds)
                {
                    _pendingTap = false;
                    _swallowRelease = true;
                    return Gesture.DoubleTap;
                }
                _pendingTap = false;
                return Gesture.None;
            }
            if (isDown)
            {
                if (!_swallowRelease && now - _downAt >= LongPressSeconds)
                {
                    _swallowRelease = true;
                    return Gesture.LongPress;
                }
                return Gesture.None;
            }
            if (_wasDown)
            {
                _wasDown = false;
                if (_swallowRelease || _ignoreThisTap) return Gesture.None;
                if (ImmediateTap)
                {
                    _ignoreTapUntil = now + DoubleTapSeconds;
                    return Gesture.SingleTap;
                }
                _pendingTap = true;
                _releasedAt = now;
                return Gesture.None;
            }
            if (_pendingTap && now - _releasedAt > DoubleTapSeconds)
            {
                _pendingTap = false;
                return Gesture.SingleTap;
            }
            return Gesture.None;
        }
    }
}
