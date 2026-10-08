namespace SaltysDroidStandby
{
    // One key, two gestures (spec §5.1): released before LongPressSeconds = Tap; held to
    // LongPressSeconds = LongPress, fired once while still held (no Tap on that release).
    public sealed class PressDetector
    {
        private bool _wasDown;
        private float _downAt;
        private bool _longFired;

        public PressDetector(float longPressSeconds)
        {
            LongPressSeconds = longPressSeconds;
        }

        public float LongPressSeconds { get; set; }

        public Gesture Update(bool isDown, float now)
        {
            if (isDown && !_wasDown)
            {
                _wasDown = true;
                _downAt = now;
                _longFired = false;
                return Gesture.None;
            }
            if (isDown)
            {
                if (!_longFired && now - _downAt >= LongPressSeconds)
                {
                    _longFired = true;
                    return Gesture.LongPress;
                }
                return Gesture.None;
            }
            if (_wasDown)
            {
                _wasDown = false;
                return _longFired ? Gesture.None : Gesture.Tap;
            }
            return Gesture.None;
        }
    }
}
