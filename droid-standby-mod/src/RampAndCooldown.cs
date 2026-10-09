namespace SaltysDroidStandby
{
    // Spec Revision 3 (user, 2026-10-08): entering a lower-drain state eases the drain down
    // over the ramp; raising the drain (waking, leaving Power Save) applies at once, so quick
    // toggling can't bank cheap seconds.
    public static class DrainRamp
    {
        public static float Factor(float from, float to, float elapsedSeconds, float rampSeconds)
        {
            if (to >= from || rampSeconds <= 0f) return to;
            float t = elapsedSeconds <= 0f ? 0f : elapsedSeconds / rampSeconds;
            if (t >= 1f) return to;
            return from + (to - from) * t;
        }

        // For the "powering down... N s" status line: 0 when not easing down.
        public static float SecondsLeft(float from, float to, float elapsedSeconds, float rampSeconds)
        {
            if (to >= from || rampSeconds <= 0f) return 0f;
            float left = rampSeconds - (elapsedSeconds < 0f ? 0f : elapsedSeconds);
            return left > 0f ? left : 0f;
        }
    }

    // Spec Revision 3: after any state change the key can't change state again until the
    // cooldown has passed (waking included). Automatic wakes bypass it.
    public static class ToggleCooldown
    {
        public static float Remaining(float lastChangeAt, float now, float cooldownSeconds)
        {
            float left = cooldownSeconds - (now - lastChangeAt);
            return left > 0f ? left : 0f;
        }
    }
}
