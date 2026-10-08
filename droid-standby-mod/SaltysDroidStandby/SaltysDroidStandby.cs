using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LaunchPadBooster;

namespace SaltysDroidStandby
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SaltysDroidStandby : BaseUnityPlugin
    {
        public const string pluginGuid = "com.naclie88.SaltysDroidStandby";
        public const string pluginName = "Salty's Droid Standby";
        public const string pluginVersion = "0.1";

        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);
        public static Mod MOD;

        // LaunchPad creates this component more than once; only the instance that patched
        // drives the per-frame client logic (Update), so it never runs twice per frame.
        private static SaltysDroidStandby _driver;

        // Spec §14: if the floor or drain patch can't apply, standby turns itself off so a
        // player can never end up in a half-working state.
        public static bool StandbyDisabled;
        private static bool _patched;

        public static void Log(string line) => Logger.LogInfo(line);

        public static void LogError(string line) => Logger.LogError(line);

        public static void LogVerbose(string line)
        {
            if (StandbyConfig.IsVerbose) Log(line);
        }

        static SaltysDroidStandby()
        {
            try
            {
                MOD = new Mod(pluginName, pluginVersion);
                MOD.Networking.Required = true;
                Log("LaunchPadBooster.Mod created (multiplayer required)");
            }
            catch (Exception e)
            {
                Log("LaunchPadBooster.Mod creation FAILED");
                Log(e.ToString());
            }
        }

        public void OnLoaded(ConfigFile config)
        {
            try
            {
                StandbyConfig.Bind(config);
                Log("Config bound: Deep floor=" + StandbyConfig.StunFloor(StandbyLevel.Deep) + ", long press=" + StandbyConfig.LongPressSeconds + "s");
            }
            catch (Exception e)
            {
                Log("Config binding FAILED");
                Log(e.ToString());
            }
        }

        private void Awake()
        {
            if (_patched)
            {
                Log("Already patched by an earlier Awake() call -- skipping");
                return;
            }
            try
            {
                var harmony = new Harmony(pluginGuid);
                RegisterPatches(harmony);
                _patched = true;
                _driver = this;
                Log("Awake() completed");
            }
            catch (Exception e)
            {
                Log("Awake() threw unexpectedly");
                Log(e.ToString());
            }
        }

        private void Update()
        {
            if (_driver != this) return;
            try
            {
                Game.LocalController.Tick();
            }
            catch (Exception e)
            {
                Log("LocalController.Tick threw");
                Log(e.ToString());
            }
        }

        // Filled in by later tasks: one PatchSafely line per patch class, plus network
        // message registration.
        private static void RegisterPatches(Harmony harmony)
        {
            MOD?.Networking.RegisterMessage<StandbyRequestMessage>();
            if (!PatchSafely(harmony, typeof(Patches.CognitionFloorPatch), "Cognition floor patch"))
            {
                StandbyDisabled = true;
            }
            if (!PatchSafely(harmony, typeof(Patches.DrainPatch), "Drain scaling patch"))
            {
                StandbyDisabled = true;
            }
            Patches.LightDrainFix.Apply(harmony); // optional; never disables the mod
            PatchSafely(harmony, typeof(Patches.JumpPatch), "Jump power patch");
            PatchSafely(harmony, typeof(UI.WakePanelPatch), "Wake panel patch");
            PatchSafely(harmony, typeof(Patches.SpeedPatch), "Top speed patch");
            PatchSafely(harmony, typeof(Patches.LookPatch), "Mouse look patch");
            PatchSafely(harmony, typeof(Patches.KeyBindingPatch), "Controls keybind patch");
            Patches.KeyBinding.EnsureRegistered(latePath: true); // if vanilla setup already ran
        }

        internal static bool PatchSafely(Harmony harmony, Type patchClass, string label)
        {
            try
            {
                harmony.CreateClassProcessor(patchClass).Patch();
                Log(label + " succeeded");
                return true;
            }
            catch (Exception e)
            {
                Log(label + " failed");
                Log(e.ToString());
                return false;
            }
        }
    }
}
