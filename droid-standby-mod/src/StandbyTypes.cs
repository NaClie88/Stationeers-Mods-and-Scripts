using System;

namespace SaltysDroidStandby
{
    // Wire value is the byte (sent in StandbyRequestMessage). Revision 3: two states; the old
    // double-tap Standby is gone and the old Deep Standby is now "Standby".
    public enum StandbyLevel : byte
    {
        Normal = 0,
        PowerSave = 1,
        Standby = 2,
    }

    public enum Gesture
    {
        None,
        Tap,
        LongPress,
    }

    // What the standby key asks for (KeyActions.Decide).
    public enum KeyAction
    {
        None,
        EnterPowerSave,
        EnterNormal,
        Wake,
        OpenMenu,
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
