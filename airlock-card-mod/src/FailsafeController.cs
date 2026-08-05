// Fail-safe extension logic, ported from ic10-airlock/watcher.ic10 +
// cycle.ic10's Tier/Button-C/Propped-Open behavior. Deliberately has
// ZERO Stationeers/Unity/BepInEx dependencies -- this class is meant to
// be attached to the real vanilla Advanced Airlock Circuitboard class
// via a Harmony patch once Milestone 1.5 finds its real name and
// methods (see PATCH_PLAN.md), but the logic itself doesn't need to
// wait on that. Everything vanilla already does (cycling,
// pressurize/evacuate against a target, lock persistence, stall/cancel)
// is untouched by this class -- see GAP_ANALYSIS.md for why.
//
// Simplification vs. the IC10 version: no Watcher/Cycle chip split, so
// no WakeHold zone-gate timer and no Transmitter/packing scheme --
// those existed only because the IC10 design had to share state across
// two independently-powered chips. A single hardcoded card has no such
// split, so this state machine is a straight port of the Tier logic,
// Button C override, and Propped-Open check, nothing else.

using System;

namespace AirlockCardMod
{
    public enum Tier
    {
        Normal = 0,
        Low = 1,
        Critical = 2,
    }

    // Everything this controller needs from the host card/circuit, kept
    // as an interface so this class has no compile-time dependency on
    // real game types. The eventual Harmony patch adapts the real
    // vanilla class to this shape -- see PATCH_PLAN.md for what each
    // member is expected to map to once real method/field names are
    // confirmed.
    public interface IAirlockHost
    {
        // 0-100. Source: dedicated Power Controller's Charge/Maximum,
        // same as watcher.ic10 lines 32-35 (l Battery Charge / Maximum,
        // div, mul 100).
        float DedicatedBatteryChargeRatio { get; }

        // True while the chamber-interior override button is held.
        // Source: cycle.ic10's r8 (BtnC), read in tierCrit via
        // "bnez r8 endLoop".
        bool ButtonCHeld { get; }

        // True once Propped-Open's match condition is confirmed (see
        // gas_sensor.ic10's tolerance checks). Optional -- a host that
        // never sets this true just never enters Propped-Open, same as
        // skipping the Gas Sensor chip in the IC10 build.
        bool PropAtmosphereMatched { get; }

        // Commands into whatever vanilla's own cycling logic exposes.
        // These are NOT reimplementations of vent/door control --
        // they're expected to call into the same code path vanilla's
        // own button-driven cycle already uses, once Milestone 1.5
        // confirms what that path actually is.
        void ForceEvacuateAndUnlock();
        void HoldBothDoorsOpen();
        void SetWarningIndicator(Tier tier);
    }

    public sealed class FailsafeController
    {
        // Same thresholds as watcher.ic10's fromNorm/fromLow/fromCrit
        // branches (lines 41-62) -- hysteresis bands, not simple
        // crossings, so a charge value bouncing right at a boundary
        // doesn't chatter between tiers.
        private const float NormalToLow = 90f;
        private const float LowToNormal = 93f;
        private const float LowToCritical = 10f;
        private const float CriticalToLow = 13f;

        public Tier CurrentTier { get; private set; } = Tier.Normal;

        private readonly IAirlockHost host;

        public FailsafeController(IAirlockHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        // Call once per tick/update, same cadence as watcher.ic10's
        // main loop. Pure state transition -- no host calls here,
        // those happen in ApplyTierEffects below, mirroring the IC10
        // script's own split between computing r0 (Tier) and acting on
        // it afterward.
        public void UpdateTier()
        {
            float charge = host.DedicatedBatteryChargeRatio;

            switch (CurrentTier)
            {
                case Tier.Normal:
                    if (charge <= NormalToLow) CurrentTier = Tier.Low;
                    break;

                case Tier.Low:
                    if (charge >= LowToNormal) CurrentTier = Tier.Normal;
                    else if (charge <= LowToCritical) CurrentTier = Tier.Critical;
                    break;

                case Tier.Critical:
                    if (charge > CriticalToLow) CurrentTier = Tier.Low;
                    break;
            }
        }

        // Ported from cycle.ic10's tierCrit/checkProp branches (lines
        // 33-42, 108-119). Call after UpdateTier each tick.
        public void ApplyTierEffects()
        {
            host.SetWarningIndicator(CurrentTier);

            switch (CurrentTier)
            {
                case Tier.Critical:
                    // cycle.ic10: "bnez r8 endLoop" -- Button C held
                    // skips the forced evacuation this tick, matching
                    // the documented override behavior (someone caught
                    // inside gets to cancel the lockdown attempt).
                    if (host.ButtonCHeld) return;
                    host.ForceEvacuateAndUnlock();
                    break;

                case Tier.Normal:
                    // cycle.ic10: checkProp -- only checked in Normal
                    // tier, matching the design note that Propped-Open
                    // only matters when the zone would otherwise be
                    // fully powered anyway.
                    if (host.PropAtmosphereMatched) host.HoldBothDoorsOpen();
                    break;

                case Tier.Low:
                    // No extra action -- Low tier is a warning state
                    // only, same as watcher.ic10 (LED goes yellow, no
                    // forced behavior yet).
                    break;
            }
        }
    }
}
