using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LaunchPadBooster;

namespace SaltysPipeSplicer
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SaltysPipeSplicer : BaseUnityPlugin
    {
        public const string pluginGuid = "com.naclie88.SaltysPipeSplicer";
        public const string pluginName = "Salty's Pipe Splicer";
        public const string pluginVersion = "0.1";

        // Static and created independently of everything else, so it can report a failure
        // anywhere below (including a static-constructor failure that stops Awake() running).
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public static Mod MOD;

        public static SaltysPipeSplicer Instance;

        private static readonly System.Collections.Generic.HashSet<string> _once = new System.Collections.Generic.HashSet<string>();

        public static void LogOnce(string line)
        {
            if (_once.Add(line))
            {
                Log(line);
            }
        }

        public static void Log(string line)
        {
            Logger.LogInfo(line);
        }

        static SaltysPipeSplicer()
        {
            try
            {
                MOD = new Mod(pluginName, pluginVersion);
                // The host does the splitting and each client's build cursor must accept the merge,
                // so everyone needs it.
                MOD.Networking.Required = true;
                Log("LaunchPadBooster.Mod created (multiplayer required)");
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
            Instance = this;
            if (_patched)
            {
                Log("Already patched by an earlier Awake() call -- skipping");
                return;
            }
            try
            {
                var harmony = new Harmony(pluginGuid);
                PatchSafely(harmony, typeof(Patches.MultiMergeSplicePatch), "Merge splice patch");
                PatchSafely(harmony, typeof(Patches.PipingCanReplacePatch), "Pipe merge cursor patch");
                PatchSafely(harmony, typeof(Patches.CableCanReplacePatch), "Cable merge cursor patch");
                PatchSafely(harmony, typeof(Patches.CableCollisionPatch), "Cable collision patch");
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
