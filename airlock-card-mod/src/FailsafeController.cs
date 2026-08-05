// Fail-safe extension logic, ported from ic10-airlock/watcher.ic10 +
// cycle.ic10's Tier/Button-C/Propped-Open/Deep-Idle behavior.
// Deliberately has ZERO Stationeers/Unity/BepInEx dependencies -- this
// class is meant to be attached to the real vanilla Advanced Airlock
// Circuitboard class via a Harmony patch once Milestone 1.5 finds its
// real name and methods (see PATCH_PLAN.md), but the logic itself
// doesn't need to wait on that. Everything vanilla already does
// (cycling, pressurize/evacuate against a target, lock persistence,
// stall/cancel) is untouched by this class -- see GAP_ANALYSIS.md for
// why.
//
// Simplification vs. the IC10 version: no Watcher/Cycle chip split, so
// no Transmitter/packing scheme to port -- that existed only to share
// state across two independently-powered chips, and a single hardcoded
// card has no such split. The WakeHold downstream-power timer IS still
// needed, though -- that's not about the card's own execution (which
// never needs power-gating the way the old Cycle chip's housing did),
// it's about keeping the *doors and Vent* Deep-Idle behind a
// switchable downstream APC/Power Controller, same as the original
// design's zone gate. Dropping it would lose the power saving, not
// just simplify the code.

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
        // div, mul 100). This device must sit outside the switched
        // downstream circuit below -- it feeds the always-on Console
        // directly, same placement requirement as the IC10 build's
        // dedicated battery.
        float DedicatedBatteryChargeRatio { get; }

        // Button reads. All three are confirmed elsewhere in this
        // project to work fully unpowered (SOURCES.md, Logic Switch
        // entry) -- so unlike the dedicated battery above, it doesn't
        // matter whether these sit on the always-on side or the
        // switched downstream side of the APC. Source: watcher.ic10's
        // r3/r4/r5 (BtnE/BtnI/BtnC), read via lbn.
        bool ButtonEHeld { get; }
        bool ButtonIHeld { get; }
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

        // NEW capability, no vanilla equivalent (see GAP_ANALYSIS.md)
        // -- switches a downstream APC/Power Controller feeding the
        // doors, Vent, and anything else that isn't the Console itself
        // or the Buttons. This is what actually delivers the power
        // saving; everything else on this interface is either
        // inherited from vanilla or informational.
        void SetDownstreamPower(bool on);
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

        // Ticks to hold downstream power on after the last qualifying
        // event, before Deep Idle can cut it again. Unconfirmed cadence
        // -- same flag the IC10 build carries for this exact constant
        // (ic10_airlock_setup_guide.md section 7: "20 is an unvalidated
        // starting guess").
        private const int WakeHoldTicks = 20;

        public Tier CurrentTier { get; private set; } = Tier.Normal;

        private readonly IAirlockHost host;
        private int wakeHoldRemaining;

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
        // 33-42, 108-119) plus watcher.ic10's Gate/WakeHold block
        // (lines 89-109). Call after UpdateTier each tick.
        public void ApplyTierEffects()
        {
            host.SetWarningIndicator(CurrentTier);

            bool anyButton = host.ButtonEHeld || host.ButtonIHeld || host.ButtonCHeld;

            switch (CurrentTier)
            {
                case Tier.Critical:
                    // watcher.ic10: Tier == Critical always forces the
                    // gate on ("beq r0 2 forceHold"), zero button press
                    // needed -- the forced evacuation has to be able to
                    // run the Vent and unlock the doors regardless of
                    // whether anyone's there to press anything.
                    UpdateDownstreamPower(forceOn: true);

                    // cycle.ic10: "bnez r8 endLoop" -- Button C held
                    // skips the forced evacuation this tick, matching
                    // the documented override behavior (someone caught
                    // inside gets to cancel the lockdown attempt). Power
                    // stays on either way -- only the evacuate/unlock
                    // action itself is skipped.
                    if (host.ButtonCHeld) return;
                    host.ForceEvacuateAndUnlock();
                    break;

                case Tier.Normal:
                    // watcher.ic10: Tier == Normal also forces the gate
                    // on continuously ("beq r0 0 forceHold") -- NOT
                    // Deep Idle behavior, confirmed explicitly in
                    // ic10_airlock_setup_guide.md step 8.2 ("this is
                    // *not* Deep Idle behavior, and shouldn't idle
                    // off"). Only Low tier below actually idles down.
                    UpdateDownstreamPower(forceOn: true);

                    // cycle.ic10: checkProp -- only checked in Normal
                    // tier, matching the design note that Propped-Open
                    // only matters when the zone would otherwise be
                    // fully powered anyway.
                    if (host.PropAtmosphereMatched) host.HoldBothDoorsOpen();
                    break;

                case Tier.Low:
                    // watcher.ic10: the only tier where the gate isn't
                    // unconditionally forced on -- a button press resets
                    // the WakeHold countdown, otherwise it ticks down
                    // and cuts downstream power once it reaches zero.
                    // This is the actual Deep Idle power saving.
                    UpdateDownstreamPower(forceOn: anyButton);
                    break;
            }
        }

        private void UpdateDownstreamPower(bool forceOn)
        {
            if (forceOn)
            {
                wakeHoldRemaining = WakeHoldTicks;
                host.SetDownstreamPower(true);
                return;
            }

            if (wakeHoldRemaining > 0)
            {
                wakeHoldRemaining--;
                host.SetDownstreamPower(true);
            }
            else
            {
                host.SetDownstreamPower(false);
            }
        }
    }
}
