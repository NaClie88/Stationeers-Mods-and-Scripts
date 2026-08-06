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
    //
    // Patched on AirlockControlBase, not AdvancedAirlockControl, even
    // though only AdvancedAirlockControl instances are meant to be
    // affected: AdvancedAirlockControl never overrides OnThreadUpdate
    // itself, it just inherits AirlockControlBase's override, so
    // Harmony has no compiled method body to attach to on the more
    // specific type (confirmed in-game, 2026-08-05: "Undefined target
    // method" HarmonyException when patched against
    // AdvancedAirlockControl directly). Filtered to AdvancedAirlockControl
    // only inside Postfix instead -- see the `is` check below. This
    // means the plain (non-Advanced) AirlockControl class's instances
    // also invoke this Postfix, since it doesn't override
    // OnThreadUpdate either; the type check is load-bearing, not
    // decorative.
    [HarmonyPatch(typeof(AirlockControlBase), nameof(AirlockControlBase.OnThreadUpdate))]
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

        // Measured in-game (2026-08-05, MeasureCallRateOnce below):
        // OnThreadUpdate averages ~17.2ms/call on this machine (close
        // to a 60fps frame interval). 15 * 17.2ms ~= 258ms, close
        // enough to the "a quarter-second response delay is
        // unnoticeable" target from PATCH_PLAN.md. Not a guess anymore
        // -- see PATCH_PLAN.md for why this couldn't be gotten any
        // other way (TickSpeed is Unity Inspector-serialized data, not
        // in the compiled IL).
        //
        // Knock-on effect, per PATCH_PLAN.md: FailsafeController's
        // WakeHoldTicks (default 20, unchanged here) now represents
        // 20 * 15 * ~17.2ms =~ 5.2 real-world seconds of held-open
        // downstream power after the last qualifying event -- a
        // reasonable-sounding hold, but still not scientifically
        // matched to anything IC10-side (that build's own per-tick
        // cadence was never confirmed either, per
        // ic10_airlock_setup_guide.md). Worth an explicit in-game feel
        // check once Deep Idle is actually wired to something real.
        private const int TicksPerCheck = 15;

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

        private static void Postfix(AirlockControlBase __instance)
        {
            if (!(__instance is AdvancedAirlockControl advanced)) return;

            MeasureCallRateOnce(advanced);

            if (++ticksSinceLastCheck < TicksPerCheck) return;
            ticksSinceLastCheck = 0;

            var controller = GetOrCreateController(advanced);
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
