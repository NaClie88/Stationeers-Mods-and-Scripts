using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LaunchPadBooster;

namespace SaltysFabricatorOverflow
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SaltysFabricatorOverflow : BaseUnityPlugin
    {
        public const string pluginGuid = "com.naclie88.SaltysFabricatorOverflow";
        public const string pluginName = "Salty's Fabricator Overflow";
        public const string pluginVersion = "1.0";

        // Same setup as SaltysYieldMultiplier: a static ManualLogSource created independently
        // of everything else, so it can report a failure anywhere below (including a
        // static-constructor failure that would stop Awake() from ever running).
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public static Mod MOD;

        public static ConfigEntry<int> MaxPerIngotType;
        public static ConfigEntry<float> EjectDelaySeconds;
        public static ConfigEntry<bool> VerboseLogging;

        // Read through these rather than the ConfigEntry directly, so the patch still behaves
        // sensibly if OnLoaded never ran (config null) or the .cfg holds an out-of-range value.
        public static int Cap => MaxPerIngotType != null ? Math.Max(1, MaxPerIngotType.Value) : 500;
        public static float Delay => EjectDelaySeconds != null ? Math.Max(0f, EjectDelaySeconds.Value) : 5f;

        public static void Log(string line)
        {
            Logger.LogInfo(line);
        }

        // Runtime-toggleable so a Release build can still produce per-eject detail on demand.
        public static void LogVerbose(string line)
        {
            if (VerboseLogging != null && VerboseLogging.Value)
            {
                Log(line);
            }
        }

        static SaltysFabricatorOverflow()
        {
            Log("Static constructor start");
            try
            {
                MOD = new Mod(pluginName, pluginVersion);
                Log("LaunchPadBooster.Mod created successfully");
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
                MaxPerIngotType = config.Bind(
                    "Overflow",
                    "MaxPerIngotType",
                    500,
                    new ConfigDescription(
                        "The most of any one ingot type a printer (Autolathe, Pipe Bender, " +
                        "Electronics Printer, Tool Printer, etc.) will hold. Anything above " +
                        "this is ejected. Ejected overflow is always split into stacks of at " +
                        "most 500, the game's ingot stack limit.",
                        new AcceptableValueRange<int>(50, 5000)));

                EjectDelaySeconds = config.Bind(
                    "Overflow",
                    "EjectDelaySeconds",
                    5f,
                    new ConfigDescription(
                        "How long after the LAST ingot goes in before overflow is ejected. " +
                        "Every new insert restarts the countdown, so you can keep topping up " +
                        "and the overflow comes out together as full stacks once you stop.",
                        new AcceptableValueRange<float>(0f, 60f)));

                VerboseLogging = config.Bind(
                    "Debug",
                    "VerboseLogging",
                    false,
                    "Logs every overflow eject (machine, ingot type, amount) to " +
                    "BepInEx/LogOutput.log. Off by default -- turn on only when diagnosing an issue.");

                Log("Config bound: MaxPerIngotType=" + MaxPerIngotType.Value +
                    ", EjectDelaySeconds=" + EjectDelaySeconds.Value +
                    ", VerboseLogging=" + VerboseLogging.Value);
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
                PatchSafely(harmony, typeof(Patches.FabricatorInsertTrackingPatch), "Insert tracking patch");
                PatchSafely(harmony, typeof(Patches.FabricatorOverflowEjectPatch), "Overflow eject patch");
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
