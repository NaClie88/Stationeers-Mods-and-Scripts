using System;

namespace SaltysDroidStandby
{
    // Wire value is the byte; never reorder (sent in StandbyRequestMessage).
    public enum StandbyLevel : byte
    {
        Normal = 0,
        PowerSave = 1,
        Deep = 2,
    }

    public enum Gesture
    {
        None,
        Tap,
        LongPress,
    }

    [Flags]
    public enum WakeCondition
    {
        None = 0,
        Light = 1,
        Wind = 2,
        Storm = 4,
        Battery = 8,
        Danger = 16,
    }
}
