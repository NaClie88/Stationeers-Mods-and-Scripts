using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Items;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LaunchPadBooster;
using UnityEngine;

namespace SaltysYieldMultiplier
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SaltysYieldMultiplier : BaseUnityPlugin
    {
        public const string pluginGuid = "com.naclie88.SaltysYieldMultiplier";
        public const string pluginName = "Salty's Yield Multiplier";
        public const string pluginVersion = "1.0";

        // A static ManualLogSource (BepInEx's own per-plugin logger, same one that produces
        // the "[Info : ModName]" lines every other mod's log entries use) instead of raw
        // Debug.Log -- created independently of everything else below so it can report a
        // failure in any of it, including a static-constructor failure that would otherwise
        // prevent Awake() from ever running at all.
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public static Mod MOD;

        public static ConfigEntry<float> IngotYieldMultiplier;
        public static ConfigEntry<float> IceGasYieldMultiplier;
        public static ConfigEntry<bool> VerboseLogging;

        public static void Log(string line)
        {
            Logger.LogInfo(line);
        }

        // Runtime-toggleable (not #if DEBUG) so a Release build shipped to Workshop can still
        // produce per-action diagnostic detail on demand. When someone reports a bug "in the
        // wild," ask them to enable this in LaunchPad's config UI, reproduce the issue, and
        // share BepInEx/LogOutput.log -- same detail level we get from a Debug build, without
        // needing a special build for every bug report.
        public static void LogVerbose(string line)
        {
            if (VerboseLogging != null && VerboseLogging.Value)
            {
                Log(line);
            }
        }

        // LaunchPad's config UI can't enforce "discrete steps only" for a float slider --
        // checked its rendering code: without an AcceptableValueRange it's free-text with no
        // restriction at all, and with one it's a continuous ImGui slider that still allows any
        // value inside the range, not fixed steps. So the actual guarantee against "the game
        // bugging out at a weird rounding amount" has to live here, not in UI configuration:
        // every read of a multiplier goes through this snap, regardless of what's stored in the
        // .cfg file or what the UI let someone drag/type. Below 1, snaps to the nearest tenth
        // (0.1-0.9); at or above 1, snaps to the nearest whole number.
        public static float SnapMultiplier(float raw)
        {
            if (raw < 1f)
            {
                return Mathf.Clamp(Mathf.Round(raw * 10f) / 10f, 0.1f, 0.9f);
            }
            return Mathf.Max(1f, Mathf.Round(raw));
        }

        // FIXED 2026-10-06 (bug hunt): null until LaunchPad calls OnLoaded. A copy dropped in
        // BepInEx/plugins never gets OnLoaded (see UpdateNotes.md), and the bare .Value threw a
        // NullReferenceException inside every smelt, breaking the furnaces. Unbound now means
        // 1x, i.e. vanilla behaviour.
        public static float IngotMultiplier => IngotYieldMultiplier != null ? SnapMultiplier(IngotYieldMultiplier.Value) : 1f;
        public static float IceMultiplier => IceGasYieldMultiplier != null ? SnapMultiplier(IceGasYieldMultiplier.Value) : 1f;

        static SaltysYieldMultiplier()
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

        // Called by StationeersLaunchPad/LaunchPadBooster's reflection-based entrypoint
        // dispatch (see LaunchPadBooster README "Mods can also add custom validation..." /
        // ConfigFile example) once the BepInEx ConfigFile for this mod is available. Only
        // fires for mods LaunchPad has discovered via its own mod list (Core/Workshop/
        // Local-with-About.xml) -- see UpdateNotes.md.
        public void OnLoaded(ConfigFile config)
        {
            Log("OnLoaded(ConfigFile) called");
            try
            {
                IngotYieldMultiplier = config.Bind(
                    "Yield",
                    "IngotYieldMultiplier",
                    5f,
                    new ConfigDescription(
                        "How many ingots come out of the material vanilla would turn into 1 " +
                        "ingot. Applies to the basic Furnace, Advanced Furnace, and Arc Furnace. " +
                        "Does not affect the Centrifuge. Actual value used is snapped to the " +
                        "nearest whole number (1, 2, 3...) at or above 1, or the nearest tenth " +
                        "(0.1, 0.2...0.9) below 1 -- the slider itself moves continuously, but " +
                        "odd in-between values never reach the game.",
                        new AcceptableValueRange<float>(0.1f, 20f)));

                IceGasYieldMultiplier = config.Bind(
                    "Yield",
                    "IceGasYieldMultiplier",
                    5f,
                    new ConfigDescription(
                        "How many moles of gas come out of melting ice (Volatile Ice, Oxite, " +
                        "etc.) compared to vanilla. Also applies to freezing environmental " +
                        "gas/liquid into condensed ice (PureIce) and melting it back -- it now " +
                        "takes this many times more liquid to freeze into the same nominal ice " +
                        "quantity, and melting multiplies it back by the same factor, so that " +
                        "cycle nets out to the original amount either way. Snapped the same way " +
                        "as IngotYieldMultiplier (whole numbers at/above 1, tenths below 1).",
                        new AcceptableValueRange<float>(0.1f, 20f)));
                VerboseLogging = config.Bind(
                    "Debug",
                    "VerboseLogging",
                    false,
                    "Logs per-action detail (exact before/after quantities for every smelt/melt " +
                    "event) to BepInEx/LogOutput.log. Off by default -- turn on only when " +
                    "diagnosing a specific issue or reproducing something for a bug report, " +
                    "since it logs on every single smelt/melt.");

                Log("Config bound: IngotYieldMultiplier=" + IngotYieldMultiplier.Value +
                    " (snapped=" + IngotMultiplier + ")" +
                    ", IceGasYieldMultiplier=" + IceGasYieldMultiplier.Value +
                    " (snapped=" + IceMultiplier + ")" +
                    ", VerboseLogging=" + VerboseLogging.Value);
            }
            catch (Exception e)
            {
                Log("Config binding FAILED");
                Log(e.ToString());
            }
        }

        // LaunchPad loads mod assemblies once to populate its pre-game config UI and again
        // for the actual session, calling Awake() on a fresh component instance each time.
        // Harmony would happily re-apply the same postfix a second time (stacking the
        // multiplier on top of itself -- 5x becomes 25x), so patching must be idempotent
        // regardless of how many times Awake() runs.
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
                // Patched per-class (not a single PatchAll) so a future game update breaking
                // one patch's target method can't also take down the other, still-working one.
                PatchSafely(harmony, typeof(Patches.IngotYieldPatch), "Ingot yield patch");
                PatchSafely(harmony, typeof(Patches.IceMeltYieldPatch), "Ice melt yield patch");
                PatchSafely(harmony, typeof(Patches.ArcFurnaceYieldPatch), "Arc Furnace yield patch");
                // Ingot-melt exemption (1 ingot in = 1 ingot out, ore still multiplied). Each is
                // safe to lose on its own: a missing one just falls back to always-multiply.
                PatchSafely(harmony, typeof(Patches.IngotMeltTrackingPatch), "Ingot melt tracking patch");
                PatchSafely(harmony, typeof(Patches.FurnaceIngotPoolPatch), "Furnace ingot pool patch");
                PatchSafely(harmony, typeof(Patches.ArcFurnaceIngotPoolPatch), "Arc Furnace ingot pool patch");
                PatchSafely(harmony, typeof(Patches.IngotScaleBlendPatch), "Ingot scale blend patch");
                PatchSafely(harmony, typeof(Patches.FurnaceTooltipPatch), "Furnace tooltip patch");
                PatchPureIcePairSafely(harmony);
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

        // PureIceFreezeYieldPatch and PureIceMeltYieldPatch are a matched pair: freeze divides
        // the recorded ice quantity by the multiplier, melt multiplies it back, and only
        // together do they net out to the original amount. Applying just one of them would be
        // worse than applying neither -- either a silent material loss (melt patch missing) or
        // a genuine duplication exploit (freeze patch missing). So unlike the other independent
        // patches, if either half fails here, both get rolled back rather than left half-applied.
        private static void PatchPureIcePairSafely(Harmony harmony)
        {
            try
            {
                harmony.CreateClassProcessor(typeof(Patches.PureIceFreezeYieldPatch)).Patch();
                harmony.CreateClassProcessor(typeof(Patches.PureIceMeltYieldPatch)).Patch();
                Log("PureIce freeze/melt pair succeeded");
            }
            catch (Exception e)
            {
                Log("PureIce freeze/melt pair failed -- rolling back both to avoid a half-applied duplication risk");
                Log(e.ToString());
                TryUnpatch(harmony, AccessTools.Method(typeof(AtmosphereHelper), "CreateIcePrefab"));
                TryUnpatch(harmony, AccessTools.Method(typeof(PureIce), nameof(PureIce.Smelt)));
            }
        }

        private static void TryUnpatch(Harmony harmony, System.Reflection.MethodBase method)
        {
            try
            {
                if (method != null)
                {
                    harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id);
                }
            }
            catch (Exception e)
            {
                Log("Rollback of " + method?.Name + " failed");
                Log(e.ToString());
            }
        }
    }
}
