using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects.Motherboards;
using HarmonyLib;

namespace AirlockCardMod.Patches
{
    // Milestone 2: attaches FailsafeController to the real vanilla
    // AdvancedAirlockControl. See PATCH_PLAN.md's "Where the Harmony
    // patch itself attaches" for why OnThreadUpdate is the correct
    // target (UpdateEachFrame stops running whenever the Console isn't
    // on-screen, which would silently break monitoring).
    [HarmonyPatch(typeof(AdvancedAirlockControl), nameof(AdvancedAirlockControl.OnThreadUpdate))]
    internal static class AdvancedAirlockFailsafePatch
    {
        private static readonly ConditionalWeakTable<AdvancedAirlockControl, FailsafeController> Controllers =
            new ConditionalWeakTable<AdvancedAirlockControl, FailsafeController>();

        // ConditionalWeakTable can't be enumerated on .NET Framework,
        // so DoorOpenPatch needs a separate reverse-lookup list of
        // known instances. Destroyed entries are skipped via Unity's
        // overloaded null check at lookup time (see DoorOpenPatch),
        // not pruned eagerly here.
        internal static readonly List<AdvancedAirlockControl> KnownControllers = new List<AdvancedAirlockControl>();

        // TicksPerCheck deliberately left at 1 (no throttling) for this
        // first cut -- PATCH_PLAN.md flags OnThreadUpdate's real call
        // rate as unrecoverable via static decompilation (it's a Unity
        // Inspector-serialized value, not in the compiled IL). The
        // block below measures it empirically the first time this
        // actually runs in-game; once that number is known, come back
        // and set TicksPerCheck (and recalibrate FailsafeController's
        // WakeHoldTicks to match) rather than guessing.
        private const int TicksPerCheck = 1;

        private static int ticksSinceLastCheck;
        private static bool loggedAttachment;
        private static AdvancedAirlockControl rateSampleInstance;
        private static readonly Stopwatch RateStopwatch = new Stopwatch();
        private static int rateSampleCount;
        private static bool rateLogged;

        internal static FailsafeController GetOrCreateController(AdvancedAirlockControl instance)
        {
            if (Controllers.TryGetValue(instance, out var existing)) return existing;

            var created = new FailsafeController(new AdvancedAirlockControlHost(instance));
            Controllers.Add(instance, created);
            KnownControllers.Add(instance);
            return created;
        }

        private static void Postfix(AdvancedAirlockControl __instance)
        {
            MeasureCallRateOnce(__instance);

            if (++ticksSinceLastCheck < TicksPerCheck) return;
            ticksSinceLastCheck = 0;

            var controller = GetOrCreateController(__instance);
            controller.UpdateTier();
            controller.ApplyTierEffects();

            if (!loggedAttachment)
            {
                loggedAttachment = true;
                UnityEngine.Debug.Log("[Salty's Advanced Airlock]: Failsafe layer attached, Tier=" + controller.CurrentTier);
            }
        }

        // One-time empirical measurement of OnThreadUpdate's real call
        // interval, scoped to a single instance so multiple airlocks in
        // the same save don't skew the average. See PATCH_PLAN.md.
        private static void MeasureCallRateOnce(AdvancedAirlockControl instance)
        {
            if (rateLogged) return;

            if (rateSampleInstance == null) rateSampleInstance = instance;
            if (instance != rateSampleInstance) return;

            if (!RateStopwatch.IsRunning)
            {
                RateStopwatch.Start();
                return;
            }

            rateSampleCount++;
            if (rateSampleCount < 20) return;

            double avgMs = RateStopwatch.Elapsed.TotalMilliseconds / rateSampleCount;
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: OnThreadUpdate avg interval over " + rateSampleCount + " calls: " + avgMs.ToString("F1") + " ms");
            rateLogged = true;
        }
    }
}
