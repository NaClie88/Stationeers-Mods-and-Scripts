using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LaunchPadBooster;

namespace SaltysRotationCycle
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SaltysRotationCycle : BaseUnityPlugin
    {
        public const string pluginGuid = "com.naclie88.SaltysRotationCycle";
        public const string pluginName = "Salty's Rotation Cycle";
        public const string pluginVersion = "0.1";

        // Static and created independently of everything else, so it can report a failure
        // anywhere below (including a static-constructor failure that stops Awake() running).
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public static Mod MOD;

        public static ConfigEntry<bool> ShowEverySpin;

        public static void Log(string line)
        {
            Logger.LogInfo(line);
        }

        static SaltysRotationCycle()
        {
            try
            {
                MOD = new Mod(pluginName, pluginVersion);
                // Only the final rotation is sent to the server, so other players don't need the mod.
                Log("LaunchPadBooster.Mod created (client-side only)");
            }
            catch (Exception e)
            {
                Log("LaunchPadBooster.Mod creation FAILED");
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
                ShowEverySpin = Config.Bind("General", "ShowEverySpin", false,
                    "Stop at every one of the 24 orientations, even ones whose connections land in the same " +
                    "places (e.g. spinning a straight pipe about its own length). Off: those look-alikes are skipped.");
                var harmony = new Harmony(pluginGuid);
                PatchSafely(harmony, typeof(Patches.SmartRotateCyclePatch), "Smart rotate cycle patch");
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
