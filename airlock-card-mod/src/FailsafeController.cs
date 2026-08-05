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
        // div, mul 100). This is a SECOND Power Controller, distinct
        // from the traditional build's single Area Power Controller
        // (see GAP_ANALYSIS.md "Power architecture") -- must sit
        // outside the switched downstream circuit below, feeding the
        // always-on Console directly. Same placement requirement as
        // the IC10 build's dedicated battery, same reason: the Console
        // has to survive a loss of the main circuit to detect and
        // respond to it.
        //
        // Safe default if no dedicated Power Controller is wired at
        // all: report 100 (always Normal), not 0. A host that can't
        // find one should disable the fail-safe layer by always
        // looking healthy, not fail closed into permanent false
        // Critical-tier lockdowns -- there's nothing to be safe *from*
        // if there's no dedicated battery to monitor in the first
        // place, so this should behave like vanilla with no fail-safe
        // layer at all, not like vanilla that's stuck evacuating.
        float DedicatedBatteryChargeRatio { get; }

        // Button reads. All three are confirmed elsewhere in this
        // project to work fully unpowered (SOURCES.md, Logic Switch
        // entry) -- so unlike the dedicated battery above, it doesn't
        // matter whether these sit on the always-on side or the
        // switched downstream side of the APC. Source: watcher.ic10's
        // r3/r4/r5 (BtnE/BtnI/BtnC), read via lbn. All three default
        // false if unwired -- see "Graceful degradation" in
        // GAP_ANALYSIS.md for what that does to behavior.
        bool ButtonEHeld { get; }
        bool ButtonIHeld { get; }
        bool ButtonCHeld { get; }

        // Capability flag: true if at least one physical entry/exit
        // button (E or I -- not C, that's the Critical-tier override
        // and irrelevant to this) is actually wired. This GATES Deep
        // Idle entirely (see FailsafeController.ApplyTierEffects,
        // Tier.Low) -- deliberately, rather than trying to wake the
        // downstream circuit for a Console-only click. Reasoning: a
        // button press is CONFIRMED safe to detect while downstream
        // power is off (that's the whole point of using Buttons as the
        // wake mechanism in the first place). A Console UI click has no
        // such confirmation -- whether vanilla's own click handling
        // survives a "power wasn't on yet, then came on a tick later"
        // delay is genuinely unknown without decompiling it, and a
        // one-shot click that gets silently dropped would be worse than
        // no power saving at all. So: buttons wired -> Deep Idle runs,
        // using the confirmed-safe mechanism. No buttons wired -> Low
        // tier just holds downstream power on continuously (same as
        // Normal), trading the power saving for not depending on an
        // unconfirmed vanilla behavior.
        bool HasWakeButtons { get; }

        // Capability flag: true if an APC/Power Controller is actually
        // detected on the downstream circuit, in the position needed to
        // gate the doors and Vent. Project owner (2026-08-05): an APC
        // only exposes its logic on its power-SOURCE side, not its
        // downstream/output side -- the network the APC creates
        // downstream of itself is isolated and doesn't carry the APC's
        // own control interface. So this check (and SetDownstreamPower's
        // own write) both depend on the card being wired to that source
        // side, not scanning for the APC from the downstream network it
        // creates. Without one present at all, there's nothing for
        // SetDownstreamPower to act on -- Deep Idle can't function no
        // matter what else is wired. Same gating role as HasWakeButtons,
        // for a different reason: HasWakeButtons is about whether it's
        // SAFE to idle, this is about whether it's even POSSIBLE to.
        bool HasDownstreamController { get; }

        // Optional, secondary wake source -- true whenever vanilla's
        // OWN logic wants to run a cycle right now (Console UI click,
        // most likely), regardless of trigger source. NOT required for
        // correctness the way HasWakeButtons's gating is: this only
        // ever matters when HasWakeButtons is already true (Deep Idle
        // is running) and adds "also wake on a Console click," as a
        // convenience for someone who wired buttons but happens to be
        // standing at the Console instead. Fine to default false if
        // Milestone 1.5 can't find a clean hook for it -- Deep Idle
        // still works correctly off buttons alone either way.
        bool VanillaCycleRequested { get; }

        // Optional. True while a Presence/Motion Sensor detects someone
        // approaching, for auto-cycling instead of requiring a manual
        // button press. Defaults false if not wired -- same graceful
        // degradation as the buttons above.
        //
        // Placement matters here more than for buttons: this project's
        // own IC10 design deliberately did NOT put a presence sensor on
        // the core wake path, specifically because Buttons are
        // confirmed to cost nothing to monitor even fully unpowered
        // (SOURCES.md) while a Motion/Presence Sensor's own idle power
        // draw is NOT confirmed the same way
        // (ic10_airlock_setup_guide.md, "Optional afterthought: APC
        // motion-sensor automation"). A presence sensor MUST sit on the
        // always-on side, same as the Console -- if it's fed from the
        // switched downstream circuit, it can never detect anyone
        // approaching while that circuit is depowered, which defeats
        // its own purpose. That placement cost is the tradeoff for the
        // convenience; it's why this project didn't make it the
        // default.
        bool PresenceDetected { get; }

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

            // Wake sources for Low tier, only consulted when
            // HasWakeButtons is true (see below) -- the confirmed-safe
            // button reads, plus secondary sources that only ever add
            // wake opportunities, never remove the button-based
            // guarantee. PropAtmosphereMatched is included here too
            // (2026-08-05 design pass): whether this can ever actually
            // be true during Deep Idle is entirely a function of where
            // the end user physically wires their Gas Sensors, and
            // that's exactly the intended behavior, not something this
            // class needs to special-case:
            //   - Gas Sensors on the switched downstream circuit (need
            //     power) -> can only ever read true while downstream
            //     power is already on, so this can never force a wake
            //     from a truly Deep-Idle state on its own -- Propped-
            //     Open stays implicitly powered-state-only, same as the
            //     original IC10 design's reasoning, just no longer
            //     enforced by a Tier check.
            //   - Gas Sensors on the always-on circuit (confirmed to
            //     work unpowered, same as Buttons) -> can read true
            //     even while downstream power is off, so a genuine
            //     atmosphere match becomes its own wake reason, keeping
            //     the doors held open across Low tier the way buttons
            //     do. This is the intended behavior for that wiring
            //     choice, not a gap.
            bool wakeRequested =
                host.ButtonEHeld || host.ButtonIHeld || host.ButtonCHeld ||
                host.VanillaCycleRequested || host.PresenceDetected ||
                host.PropAtmosphereMatched;

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

                    // Diverges from cycle.ic10 here (2026-08-05 design
                    // pass, project owner decision): the original
                    // script only checked this in Tier 0/Normal. Now
                    // also checked in Low (below) -- see wakeRequested's
                    // comment above for why that's safe and intentional
                    // rather than a drift from the port. Never checked
                    // in Critical, in either version.
                    if (host.PropAtmosphereMatched) host.HoldBothDoorsOpen();
                    break;

                case Tier.Low:
                    // Deep Idle needs BOTH a confirmed-safe wake
                    // mechanism (HasWakeButtons -- is it SAFE to idle)
                    // and something to actually switch
                    // (HasDownstreamController -- is it even POSSIBLE
                    // to idle). Missing either one -> hold downstream
                    // power on continuously, same as Normal, rather
                    // than gambling on an unconfirmed vanilla behavior
                    // or calling SetDownstreamPower with nothing on the
                    // other end of it.
                    if (!host.HasWakeButtons || !host.HasDownstreamController)
                    {
                        UpdateDownstreamPower(forceOn: true);
                        if (host.PropAtmosphereMatched) host.HoldBothDoorsOpen();
                        break;
                    }

                    // watcher.ic10: the only tier where the gate isn't
                    // unconditionally forced on -- a wake event resets
                    // the WakeHold countdown, otherwise it ticks down
                    // and cuts downstream power once it reaches zero.
                    // This is the actual Deep Idle power saving.
                    // wakeRequested already folds in PropAtmosphereMatched
                    // (see its comment above), so a genuine atmosphere
                    // match keeps this branch awake on its own -- no
                    // separate condition needed here.
                    UpdateDownstreamPower(forceOn: wakeRequested);
                    if (host.PropAtmosphereMatched) host.HoldBothDoorsOpen();
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
