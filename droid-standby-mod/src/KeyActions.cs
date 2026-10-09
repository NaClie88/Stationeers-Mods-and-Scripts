namespace SaltysDroidStandby
{
    // Spec Revision 3 key table. Every gesture in Standby wakes (holding the key there must not
    // reopen the menu), and waking lands in Power Save, not Normal (user, 2026-10-08).
    public static class KeyActions
    {
        public const StandbyLevel WakeTarget = StandbyLevel.PowerSave;

        public static KeyAction Decide(StandbyLevel level, Gesture g)
        {
            if (g == Gesture.None) return KeyAction.None;
            switch (level)
            {
                case StandbyLevel.Normal:
                    return g == Gesture.Tap ? KeyAction.EnterPowerSave : KeyAction.OpenMenu;
                case StandbyLevel.PowerSave:
                    return g == Gesture.Tap ? KeyAction.EnterNormal : KeyAction.OpenMenu;
                default:
                    return KeyAction.Wake;
            }
        }

        // The toggle cooldown gates every state change made with the key, waking included
        // ("keep the toggle cool down"). Opening the menu changes nothing, so it isn't gated.
        // Automatic wake conditions never pass through here: they aren't toggles.
        public static bool IsGatedByCooldown(KeyAction a) =>
            a == KeyAction.EnterPowerSave || a == KeyAction.EnterNormal || a == KeyAction.Wake;
    }
}
