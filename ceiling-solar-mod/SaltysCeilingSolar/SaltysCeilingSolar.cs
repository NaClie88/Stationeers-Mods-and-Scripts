using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LaunchPadBooster;

namespace SaltysCeilingSolar
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SaltysCeilingSolar : BaseUnityPlugin
    {
        public const string pluginGuid = "com.naclie88.SaltysCeilingSolar";
        public const string pluginName = "Salty's Ceiling Solar";
        public const string pluginVersion = "0.1";

        // Static and created independently of everything else, so it can report a failure
        // anywhere below (including a static-constructor failure that stops Awake() running).
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public static Mod MOD;

        public static ConfigEntry<bool> VerboseLogging;

        public static bool Verbose => VerboseLogging != null && VerboseLogging.Value;

        public static void Log(string line)
        {
            Logger.LogInfo(line);
        }

        static SaltysCeilingSolar()
        {
            try
            {
                MOD = new Mod(pluginName, pluginVersion);
                // The kit's style is sent as an index into its build list and the panel spawns by
                // prefab hash, so server and clients must all have the clinging style.
                MOD.Networking.Required = true;
                Log("LaunchPadBooster.Mod created (multiplayer required)");
            }
            catch (Exception e)
            {
                Log("LaunchPadBooster.Mod creation FAILED");
                Log(e.ToString());
            }
        }

        // Called by LaunchPadBooster's reflection-based entrypoint dispatch once this mod's
        // ConfigFile is available.
        public void OnLoaded(ConfigFile config)
        {
            try
            {
                VerboseLogging = config.Bind(
                    "Debug",
                    "VerboseLogging",
                    true,
                    "When a clinging panel faces the sun but makes no power, log what blocks its sunlight " +
                    "to BepInEx/LogOutput.log (at most every 10 s per panel). On while the mod is being tested.");
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
                PatchSafely(harmony, typeof(Patches.ClingingPanelPrefabPatch), "Clinging panel prefab patch");
                PatchSafely(harmony, typeof(Patches.ClingingPanelRenderPatch), "Clinging panel render patch");
                PatchSafely(harmony, typeof(Patches.ClingingPanelScannerPatch), "Clinging panel scanner patch");
                PatchSafely(harmony, typeof(Patches.ClingingPanelCursorPatch), "Clinging panel cursor patch");
                PatchSafely(harmony, typeof(Patches.ClingingPanelNamePatch), "Clinging panel name patch");
                PatchSafely(harmony, typeof(Patches.ClingingPanelDescriptionPatch), "Clinging panel description patch");
                PatchSafely(harmony, typeof(Patches.ClingingPanelDiagnosticsPatch), "Clinging panel diagnostics patch");
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
