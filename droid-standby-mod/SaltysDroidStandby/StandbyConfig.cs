using BepInEx.Configuration;
using UnityEngine;

namespace SaltysDroidStandby
{
    // All tunables (spec §13). Every accessor falls back to the spec default when the
    // ConfigEntry is unbound (null) -- e.g. if LaunchPad never called OnLoaded.
    public static class StandbyConfig
    {
        public static ConfigEntry<KeyCode> Key;
        public static ConfigEntry<float> LongPress;
        public static ConfigEntry<float> PowerSaveFloor, PowerSaveDrain;
        public static ConfigEntry<float> DeepFloor, DeepDrain;
        public static ConfigEntry<int> Consecutive;
        public static ConfigEntry<float> LightThreshold, WindThreshold;
        public static ConfigEntry<float> BatteryCharged, BatteryLow;
        public static ConfigEntry<float> DamageDelta, PressureDelta, TempMinC, TempMaxC;
        public static ConfigEntry<bool> WakeLight, WakeWind, WakeStorm, WakeBattery, WakeDanger;
        public static ConfigEntry<float> SafetyBatteryEntry, SafetyIdleEntry;
        public static ConfigEntry<bool> SafetyPauseEntry;
        public static ConfigEntry<bool> Verbose;

        public static void Bind(ConfigFile c)
        {
            Key = c.Bind("Controls", "StandbyKey", KeyCode.Z, "Tap = Power Save Mode, hold = Deep Standby, any press in Deep Standby = wake.");
            LongPress = c.Bind("Controls", "LongPressSeconds", 0.6f, new ConfigDescription("How long to hold the key for Deep Standby.", new AcceptableValueRange<float>(0.2f, 2f)));

            PowerSaveFloor = c.Bind("PowerSave", "CognitionLossFloor", 40f, new ConfigDescription("Minimum cognition loss (stun) held in Power Save Mode. Speed = 1 - 0.9 x floor/100.", new AcceptableValueRange<float>(0f, 85f)));
            PowerSaveDrain = c.Bind("PowerSave", "DrainFactor", 0.5f, new ConfigDescription("Battery drain multiplier.", new AcceptableValueRange<float>(0.05f, 1f)));
            DeepFloor = c.Bind("DeepStandby", "CognitionLossFloor", 85f, new ConfigDescription("Minimum cognition loss held in Deep Standby. Keep below 90 (vanilla falls unconscious at 90 in a bed, 100 anywhere).", new AcceptableValueRange<float>(0f, 89f)));
            DeepDrain = c.Bind("DeepStandby", "DrainFactor", 0.25f, new ConfigDescription("Battery drain multiplier.", new AcceptableValueRange<float>(0.05f, 1f)));

            Consecutive = c.Bind("Wake", "ConsecutiveChecks", 3, new ConfigDescription("Checks (about 1 s apart) a threshold must hold before waking.", new AcceptableValueRange<int>(1, 10)));
            LightThreshold = c.Bind("Wake", "LightPercent", 20f, new ConfigDescription("Wake when light (sun height x storm dimming) rises above this %.", new AcceptableValueRange<float>(1f, 100f)));
            WindThreshold = c.Bind("Wake", "WindPercent", 40f, new ConfigDescription("Wake when wind rises above this %.", new AcceptableValueRange<float>(1f, 100f)));
            BatteryCharged = c.Bind("Wake", "BatteryChargedPercent", 90f, new ConfigDescription("Wake when total battery charges to this %.", new AcceptableValueRange<float>(10f, 100f)));
            BatteryLow = c.Bind("Wake", "BatteryLowPercent", 5f, new ConfigDescription("Wake when total battery drops to this %.", new AcceptableValueRange<float>(0f, 50f)));
            DamageDelta = c.Bind("Wake", "DamagePoints", 1f, new ConfigDescription("Danger: wake on this much brute+burn damage between checks.", new AcceptableValueRange<float>(0.1f, 50f)));
            PressureDelta = c.Bind("Wake", "PressureChangeKpa", 20f, new ConfigDescription("Danger: wake on a pressure swing of this many kPa between checks.", new AcceptableValueRange<float>(1f, 500f)));
            TempMinC = c.Bind("Wake", "SafeTempMinC", -50f, "Danger: wake when surrounding temperature stays below this (C).");
            TempMaxC = c.Bind("Wake", "SafeTempMaxC", 50f, "Danger: wake when surrounding temperature stays above this (C).");
            WakeLight = c.Bind("WakeDefaults", "Light", true, "Pre-ticked in the wake panel.");
            WakeWind = c.Bind("WakeDefaults", "Wind", false, "Pre-ticked in the wake panel.");
            WakeStorm = c.Bind("WakeDefaults", "Storm", false, "Pre-ticked in the wake panel.");
            WakeBattery = c.Bind("WakeDefaults", "Battery", true, "Pre-ticked in the wake panel.");
            WakeDanger = c.Bind("WakeDefaults", "Danger", true, "Pre-ticked in the wake panel.");

            SafetyBatteryEntry = c.Bind("SafetyNet", "BatteryPercent", 10f, new ConfigDescription("Auto Deep Standby at or below this total battery %, when idle.", new AcceptableValueRange<float>(0f, 50f)));
            SafetyIdleEntry = c.Bind("SafetyNet", "IdleSeconds", 60f, new ConfigDescription("Seconds without input before the safety net may act.", new AcceptableValueRange<float>(10f, 600f)));
            SafetyPauseEntry = c.Bind("SafetyNet", "PauseInSinglePlayer", true, "Also pause the game in single-player (or when you are the only player on your hosted game) when the safety net acts.");

            Verbose = c.Bind("Debug", "VerboseLogging", false, "Log level changes, wake checks and refunds.");
        }

        public static KeyCode StandbyKey => Key != null ? Key.Value : KeyCode.Z;
        public static float LongPressSeconds => LongPress != null ? LongPress.Value : 0.6f;
        public static float CheckIntervalSeconds => 1f;

        public static float StunFloor(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return PowerSaveFloor != null ? PowerSaveFloor.Value : 40f;
                case StandbyLevel.Deep: return DeepFloor != null ? DeepFloor.Value : 85f;
                default: return 0f;
            }
        }

        public static float DrainFactor(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return PowerSaveDrain != null ? PowerSaveDrain.Value : 0.5f;
                case StandbyLevel.Deep: return DeepDrain != null ? DeepDrain.Value : 0.25f;
                default: return 1f;
            }
        }

        // Same factor vanilla applies to walking speed for this much stun (MovementController).
        public static float JumpFactor(StandbyLevel level) => Mathf.Clamp01(1f - 0.9f * StunFloor(level) / 100f);

        public static WakeThresholds Thresholds() => new WakeThresholds
        {
            LightPercent = LightThreshold != null ? LightThreshold.Value : 20f,
            WindPercent = WindThreshold != null ? WindThreshold.Value : 40f,
            BatteryChargedRatio = (BatteryCharged != null ? BatteryCharged.Value : 90f) / 100f,
            BatteryLowRatio = (BatteryLow != null ? BatteryLow.Value : 5f) / 100f,
            DamageDelta = DamageDelta != null ? DamageDelta.Value : 1f,
            PressureDeltaKpa = PressureDelta != null ? PressureDelta.Value : 20f,
            SafeTempMinK = (TempMinC != null ? TempMinC.Value : -50f) + 273.15f,
            SafeTempMaxK = (TempMaxC != null ? TempMaxC.Value : 50f) + 273.15f,
            ConsecutiveChecks = Consecutive != null ? Consecutive.Value : 3,
        };

        public static WakeCondition DefaultWake
        {
            get
            {
                WakeCondition w = WakeCondition.None;
                if (WakeLight == null || WakeLight.Value) w |= WakeCondition.Light;
                if (WakeWind != null && WakeWind.Value) w |= WakeCondition.Wind;
                if (WakeStorm != null && WakeStorm.Value) w |= WakeCondition.Storm;
                if (WakeBattery == null || WakeBattery.Value) w |= WakeCondition.Battery;
                if (WakeDanger == null || WakeDanger.Value) w |= WakeCondition.Danger;
                return w;
            }
        }

        public static float SafetyBattery => (SafetyBatteryEntry != null ? SafetyBatteryEntry.Value : 10f) / 100f;
        public static float SafetyIdleSeconds => SafetyIdleEntry != null ? SafetyIdleEntry.Value : 60f;
        public static bool SafetyPause => SafetyPauseEntry == null || SafetyPauseEntry.Value;
        public static bool IsVerbose => Verbose != null && Verbose.Value;
    }
}
