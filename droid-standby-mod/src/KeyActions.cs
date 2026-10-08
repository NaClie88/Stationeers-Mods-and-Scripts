namespace SaltysDroidStandby
{
    // Spec Revision 2 gesture table. Every gesture in Deep Standby wakes (holding the key
    // there must not reopen the menu). In Standby a tap can only mean "wake", so taps are
    // read immediately (no double-tap wait).
    public static class KeyActions
    {
        public static KeyAction Decide(StandbyLevel level, Gesture g)
        {
            if (g == Gesture.None) return KeyAction.None;
            switch (level)
            {
                case StandbyLevel.Normal:
                    if (g == Gesture.SingleTap) return KeyAction.EnterPowerSave;
                    return g == Gesture.DoubleTap ? KeyAction.EnterStandby : KeyAction.OpenMenu;
                case StandbyLevel.PowerSave:
                    if (g == Gesture.SingleTap) return KeyAction.Wake;
                    return g == Gesture.DoubleTap ? KeyAction.EnterStandby : KeyAction.OpenMenu;
                case StandbyLevel.Standby:
                    return g == Gesture.LongPress ? KeyAction.OpenMenu : KeyAction.Wake;
                default:
                    return KeyAction.Wake;
            }
        }

        public static bool WantsImmediateTap(StandbyLevel level) =>
            level == StandbyLevel.Standby || level == StandbyLevel.DeepStandby;
    }
}
