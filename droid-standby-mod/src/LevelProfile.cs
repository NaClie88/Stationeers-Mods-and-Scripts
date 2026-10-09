namespace SaltysDroidStandby
{
    // Spec Revision 3 numbers. Movement = top speed and jump; Look = mouse sensitivity.
    // Floors and drains stay in StandbyConfig (user-tunable); these are fixed by the spec.
    public static class LevelProfile
    {
        public static float Movement(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return 0.125f;
                case StandbyLevel.Standby: return 0f;
                default: return 1f;
            }
        }

        public static float Look(StandbyLevel level) => Movement(level);

        // Throw power (hold Q) is capped like movement (user, 2026-10-08).
        public static float Throw(StandbyLevel level) => Movement(level);

        // Standby freezes the droid: no jetpack, world, inventory or built-in night vision
        // (Night Vision Goggles are a tool and untouched). Power Save blocks none of these.
        public static bool BlocksJetpack(StandbyLevel level) => level == StandbyLevel.Standby;
        public static bool BlocksWorldInteraction(StandbyLevel level) => level == StandbyLevel.Standby;
        public static bool BlocksInventory(StandbyLevel level) => level == StandbyLevel.Standby;
        public static bool BlocksNightVision(StandbyLevel level) => level == StandbyLevel.Standby;

        // Vanilla's Ctrl/Alt mouse mode (InputMouse) interacts with the world on its own path;
        // block it with the rest of world interaction, and while the Standby menu is open so
        // clicks on Start/Cancel can't reach a switch behind the window.
        public static bool BlocksMouseWorld(StandbyLevel level, bool menuOpen) =>
            menuOpen || BlocksWorldInteraction(level);

        public static bool IsValidWire(byte b) => b <= (byte)StandbyLevel.Standby;
    }
}
