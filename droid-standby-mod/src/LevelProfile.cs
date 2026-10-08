namespace SaltysDroidStandby
{
    // Spec Revision 2 numbers. Movement = top speed and jump; Look = mouse sensitivity.
    // Floors and drains stay in StandbyConfig (user-tunable); these are fixed by the spec.
    public static class LevelProfile
    {
        public static float Movement(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return 0.125f;
                case StandbyLevel.Standby:
                case StandbyLevel.DeepStandby: return 0f;
                default: return 1f;
            }
        }

        public static float Look(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return 0.125f;
                case StandbyLevel.Standby: return 0.0625f;
                case StandbyLevel.DeepStandby: return 0f;
                default: return 1f;
            }
        }

        public static bool BlocksJetpack(StandbyLevel level) => level >= StandbyLevel.Standby;
        public static bool BlocksWorldInteraction(StandbyLevel level) => level >= StandbyLevel.Standby;
        public static bool BlocksInventory(StandbyLevel level) => level == StandbyLevel.DeepStandby;

        // The droid's built-in night vision only; Night Vision Goggles are a tool and untouched.
        public static bool BlocksNightVision(StandbyLevel level) => level == StandbyLevel.DeepStandby;

        public static bool IsValidWire(byte b) => b <= (byte)StandbyLevel.DeepStandby;
    }
}
