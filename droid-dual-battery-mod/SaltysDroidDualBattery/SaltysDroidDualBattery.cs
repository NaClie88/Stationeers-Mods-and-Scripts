using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LaunchPadBooster;

namespace SaltysDroidDualBattery
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SaltysDroidDualBattery : BaseUnityPlugin
    {
        public const string pluginGuid = "com.naclie88.SaltysDroidDualBattery";
        public const string pluginName = "Salty's Droid Dual Battery";
        public const string pluginVersion = "0.2";

        // Same setup as the other Salty mods: a static ManualLogSource created independently
        // of everything else, so it can report a failure anywhere below (including a
        // static-constructor failure that would stop Awake() from ever running).
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public static Mod MOD;

        public static ConfigEntry<bool> VerboseLogging;

        public static void Log(string line)
        {
            Logger.LogInfo(line);
        }

        // Runtime-toggleable so a Release build can still produce battery-switch detail on demand.
        public static void LogVerbose(string line)
        {
            if (VerboseLogging != null && VerboseLogging.Value)
            {
                Log(line);
            }
        }

        static SaltysDroidDualBattery()
        {
            Log("Static constructor start");
            try
            {
                MOD = new Mod(pluginName, pluginVersion);
                // Droids get an extra slot, and saves/network messages address slots by index,
                // so a client without the mod would disagree with the server on a droid's layout.
                MOD.Networking.Required = true;
                Log("LaunchPadBooster.Mod created successfully (multiplayer required)");
            }
            catch (Exception e)
            {
                Log("LaunchPadBooster.Mod creation FAILED");
                Log(e.ToString());
            }
        }

        // Called by LaunchPadBooster's reflection-based entrypoint dispatch once this mod's
        // ConfigFile is available (see SaltysYieldMultiplier.OnLoaded for the same pattern).
        public void OnLoaded(ConfigFile config)
        {
            Log("OnLoaded(ConfigFile) called");
            try
            {
                VerboseLogging = config.Bind(
                    "Debug",
                    "VerboseLogging",
                    false,
                    "Logs droid battery switches (slot 1 -> slot 2) and HUD button setup detail to " +
                    "BepInEx/LogOutput.log. Off by default -- turn on only when diagnosing an issue.");

                Log("Config bound: VerboseLogging=" + VerboseLogging.Value);
            }
            catch (Exception e)
            {
                Log("Config binding FAILED");
                Log(e.ToString());
            }
        }

        // LaunchPad calls Awake() more than once (config-UI load + session load); patch only once.
        private static bool _patched;

        private void Awake()
        {
            Log("Awake() called");
            if (_patched)
            {
                Log("Already patched by an earlier Awake() call -- skipping");
                return;
            }
            try
            {
                var harmony = new Harmony(pluginGuid);
                PatchSafely(harmony, typeof(Patches.ExtraBatterySlotPatch), "Droid second battery slot patch");
                PatchSafely(harmony, typeof(Patches.RobotBatteryPatch), "Robot battery selection patch");
                PatchSafely(harmony, typeof(Patches.HudSlotPatch), "HUD slot button patch");
                PatchSafely(harmony, typeof(Patches.DroidSleeperPatch), "Droid Sleeper slot-2 charging patch");
                PatchSafely(harmony, typeof(Patches.BatteryLabelLocalizationPatch), "Battery label localization patch");
                PatchSafely(harmony, typeof(Patches.BatteryChargerPatch), "Disposable charger targeting patch");
                _patched = true;
                Log("Awake() completed");
            }
            catch (Exception e)
            {
                Log("Awake() threw unexpectedly");
                Log(e.ToString());
            }
        }

        private static void PatchSafely(Harmony harmony, Type patchClass, string label)
        {
            try
            {
                harmony.CreateClassProcessor(patchClass).Patch();
                Log(label + " succeeded");
            }
            catch (Exception e)
            {
                Log(label + " failed");
                Log(e.ToString());
            }
        }
    }
}
