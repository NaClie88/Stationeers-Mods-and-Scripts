namespace SaltysDroidStandby
{
    // Spec §5.1 table. Any gesture in Deep Standby wakes to Normal.
    public static class LevelTransitions
    {
        public static StandbyLevel Next(StandbyLevel current, Gesture gesture)
        {
            if (gesture == Gesture.None)
            {
                return current;
            }
            switch (current)
            {
                case StandbyLevel.Normal:
                    return gesture == Gesture.Tap ? StandbyLevel.PowerSave : StandbyLevel.Deep;
                case StandbyLevel.PowerSave:
                    return gesture == Gesture.Tap ? StandbyLevel.Normal : StandbyLevel.Deep;
                default:
                    return StandbyLevel.Normal;
            }
        }
    }
}
