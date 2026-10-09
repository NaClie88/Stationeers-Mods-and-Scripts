using BepInEx.Configuration;
using UnityEngine;

namespace SaltysDroidStandby
{
    // All tunables (spec §13). Every accessor falls back to the spec default when the
    // ConfigEntry is unbound (null) -- e.g. if LaunchPad never called OnLoaded.
    public static class StandbyConfig
    {
        public static ConfigEntry<float> Hold, Ramp, Cooldown, OverlayBottom;
        public static ConfigEntry<float> PowerSaveFloor, PowerSaveDrain;
        public static ConfigEntry<float> StandbyFloorEntry;
        public static ConfigEntry<bool> LightFix, ShowGrid;
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
            // The key itself is bound in the game's Settings > Controls > Inventory ("Droid Standby").
            // New key names on purpose: an old LongPressSeconds = 0.6 in an existing .cfg must not
            // become the 3 s Deep Standby hold (phase 1b Review Focus 1).
            Hold = c.Bind("Controls", "HoldSeconds", 3f, new ConfigDescription("Hold the standby key this long to open the Standby (Time Skip) menu.", new AcceptableValueRange<float>(1f, 6f)));

            PowerSaveFloor = c.Bind("PowerSave", "CognitionLossFloor", 40f, new ConfigDescription("Minimum cognition loss (stun) held in Power Save Mode. Vision only; speed is fixed by the level.", new AcceptableValueRange<float>(0f, 85f)));
            PowerSaveDrain = c.Bind("PowerSave", "DrainFactor", 0.5f, new ConfigDescription("Battery drain multiplier.", new AcceptableValueRange<float>(0.05f, 1f)));
            StandbyFloorEntry = c.Bind("Standby", "CognitionLossFloor", 85f, new ConfigDescription("Minimum cognition loss held in Standby (battery drain is frozen there). Keep below 90 (vanilla falls unconscious at 90 in a bed, 100 anywhere).", new AcceptableValueRange<float>(0f, 89f)));
            Ramp = c.Bind("Timing", "RampDownSeconds", 5f, new ConfigDescription("Battery drain eases down to the new state's rate over this long. Raising the drain is immediate.", new AcceptableValueRange<float>(0f, 30f)));
            Cooldown = c.Bind("Timing", "ToggleCooldownSeconds", 5f, new ConfigDescription("After any state change, the standby key can't change state again for this long (waking included). Automatic wakes ignore it.", new AcceptableValueRange<float>(0f, 30f)));
            ShowGrid = c.Bind("Overlay", "ShowPositionGrid", true, "TEMPORARY test aid: draw numbered lines (percent of screen height) to pick BottomPercent. Turn off once set.");
            OverlayBottom = c.Bind("Overlay", "BottomPercent", 82f, new ConfigDescription("How far down the screen the bottom edge of the standby overlay sits (percent of screen height). Lower it to move the overlay up. Read live.", new AcceptableValueRange<float>(20f, 98f)));

            Consecutive = c.Bind("Wake", "ConsecutiveChecks", 3, new ConfigDescription("Checks (about 1 s apart) a threshold must hold before waking.", new AcceptableValueRange<int>(1, 10)));
            LightThreshold = c.Bind("Wake", "LightPercent", 20f, new ConfigDescription("Wake when light (sun height x storm dimming) rises above this %.", new AcceptableValueRange<float>(1f, 100f)));
            WindThreshold = c.Bind("Wake", "WindPercent", 40f, new ConfigDescription("Wake when wind rises above this %.", new AcceptableValueRange<float>(1f, 100f)));
            BatteryCharged = c.Bind("Wake", "BatteryChargedPercent", 90f, new ConfigDescription("Wake when total battery charges to this %.", new AcceptableValueRange<float>(10f, 100f)));
            BatteryLow = c.Bind("Wake", "BatteryLowPercent", 5f, new ConfigDescription("Wake when total battery drops to this %.", new AcceptableValueRange<float>(0f, 50f)));
            DamageDelta = c.Bind("Wake", "DamagePoints", 1f, new ConfigDescription("Danger: wake on this much brute+burn damage between checks.", new AcceptableValueRange<float>(0.1f, 50f)));
            PressureDelta = c.Bind("Wake", "PressureChangeKpa", 20f, new ConfigDescription("Danger: wake on a pressure swing of this many kPa between checks.", new AcceptableValueRange<float>(1f, 500f)));
            TempMinC = c.Bind("Wake", "SafeTempMinC", -50f, "Danger: wake when surrounding temperature stays below this (C).");
            TempMaxC = c.Bind("Wake", "SafeTempMaxC", 50f, "Danger: wake when surrounding temperature stays above this (C).");
            WakeLight = c.Bind("WakeDefaults", "Light", true, "Pre-ticked in the Standby menu, and what the safety net's Standby wakes on.");
            WakeWind = c.Bind("WakeDefaults", "Wind", false, "Pre-ticked in the Standby menu, and what the safety net's Standby wakes on.");
            WakeStorm = c.Bind("WakeDefaults", "Storm", false, "Pre-ticked in the Standby menu, and what the safety net's Standby wakes on.");
            WakeBattery = c.Bind("WakeDefaults", "Battery", true, "Pre-ticked in the Standby menu, and what the safety net's Standby wakes on.");
            WakeDanger = c.Bind("WakeDefaults", "Danger", true, "Pre-ticked in the Standby menu, and what the safety net's Standby wakes on.");

            SafetyBatteryEntry = c.Bind("SafetyNet", "BatteryPercent", 10f, new ConfigDescription("Auto Standby at or below this total battery %, when idle.", new AcceptableValueRange<float>(0f, 50f)));
            SafetyIdleEntry = c.Bind("SafetyNet", "IdleSeconds", 60f, new ConfigDescription("Seconds without input before the safety net may act.", new AcceptableValueRange<float>(10f, 600f)));
            SafetyPauseEntry = c.Bind("SafetyNet", "PauseInSinglePlayer", true, "Also pause the game in single-player (or when you are the only player on your hosted game) when the safety net acts.");

            LightFix = c.Bind("Fixes", "HelmetLightDrainFix", true, "Fix the vanilla bug where the helmet-light key raises every droid's battery drain by 5% until night vision is toggled. With the fix, each droid pays 5% extra only while its own helmet light is on. Turn off if a game update fixes it differently.");

            Verbose = c.Bind("Debug", "VerboseLogging", false, "Log level changes, wake checks and refunds.");
        }

        public static bool ShowPositionGrid => ShowGrid == null || ShowGrid.Value;
        public static float RampSeconds => Ramp != null ? Ramp.Value : 5f;
        public static float CooldownSeconds => Cooldown != null ? Cooldown.Value : 5f;
        public static float OverlayBottomFraction => (OverlayBottom != null ? OverlayBottom.Value : 82f) / 100f;
        public static float HoldSeconds => Hold != null ? Hold.Value : 3f;
        public static bool LightDrainFixEnabled => LightFix == null || LightFix.Value;
        public static float CheckIntervalSeconds => 1f;

        public static float StunFloor(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return PowerSaveFloor != null ? PowerSaveFloor.Value : 40f;
                case StandbyLevel.Standby: return StandbyFloorEntry != null ? StandbyFloorEntry.Value : 85f;
                default: return 0f;
            }
        }

        public static float DrainFactor(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return PowerSaveDrain != null ? PowerSaveDrain.Value : 0.5f;
                case StandbyLevel.Standby: return 0f; // frozen (spec Revision 3)
                default: return 1f;
            }
        }

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
