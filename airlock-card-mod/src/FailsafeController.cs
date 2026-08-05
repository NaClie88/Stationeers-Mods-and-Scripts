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

        // Setup-time choice, not a live sensor read (project owner,
        // 2026-08-05): if true, a genuine atmosphere match by itself no
        // longer forces downstream power on in Low tier -- the doors
        // and Vent circuit is allowed to idle down even while propped
        // open, on the assumption that (a) a door doesn't need
        // continuous power just to stay in whatever position it's
        // already in, only to move, and (b) all three Gas Sensors (the
        // chamber one plus the exterior/interior-facing Propped-Open
        // pair) are wired to the always-on circuit so they keep
        // reading regardless. Point (a) is the same open question
        // flagged in STATE_TABLE.md's transition notes -- still
        // unconfirmed, Milestone 1.5/in-game territory, so this is an
        // opt-in, not the default (false).
        //
        // Doesn't disable monitoring -- see the mismatch-while-idle
        // handling in ApplyTierEffects: a match going false while this
        // is enabled still forces a wake, so a real leak or reopened
        // mismatch still gets caught and acted on. What this setting
        // skips is re-affirming power for a *steady, unchanging* match,
        // not surveillance of the chamber generally.
        bool AllowPowerDownWhilePropped { get; }

        // Setup/runtime toggle (Console setting), NOT a live sensor
        // read (project owner, 2026-08-05): when true, suspends this
        // entire fail-safe layer. Tier monitoring still runs
        // (UpdateTier keeps updating CurrentTier, SetWarningIndicator
        // still gets called so the indicator stays informative), but
        // ApplyTierEffects takes no other action at all -- no forced
        // downstream power changes, no forced evacuation, no
        // Propped-Open management. Vanilla's own cycling keeps running
        // underneath, completely untouched, exactly as if this mod's
        // logic wasn't installed. For construction/maintenance -- e.g.
        // expanding a room and wanting to hold a door open indefinitely
        // without the fail-safe layer fighting that decision.
        bool MaintenanceModeEnabled { get; }

        // Optional temperature safety check for the Critical-tier
        // unlock specifically (project owner, 2026-08-05 -- flagged as
        // a real gap: matching pressure alone doesn't protect against
        // unlocking into an extreme-temperature environment, e.g. near
        // a lava or ice world). True if the far side's temperature is
        // within a safe range to unlock into right now. Only gates the
        // UNLOCK step -- see UnlockDoors() below -- never the
        // evacuate/depressurize step, which is safe regardless of
        // temperature. Defaults true if no temperature check is wired,
        // same graceful-degradation pattern as everything else on this
        // interface: a host that doesn't implement this just gets the
        // original unconditional-unlock behavior back.
        bool SafeToUnlockTemperature { get; }

        // Commands into whatever vanilla's own cycling logic exposes.
        // These are NOT reimplementations of vent/door control --
        // they're expected to call into the same code path vanilla's
        // own button-driven cycle already uses, once Milestone 1.5
        // confirms what that path actually is.
        //
        // ForceEvacuate() and UnlockDoors() were previously one method
        // (ForceEvacuateAndUnlock()) -- split (2026-08-05) so the
        // temperature check above can gate just the unlock step. See
        // PATCH_PLAN.md for how this affects reusing vanilla's own
        // evacuate method, if that method turns out to unlock as part
        // of the same call.
        void ForceEvacuate();
        void UnlockDoors();
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
        // doesn't chatter between tiers. Configurable (2026-08-05,
        // project owner) rather than fixed -- these were always
        // somewhat-arbitrary defaults (the IC10 build itself never
        // confirmed 90/93/10/13 as anything more than a reasonable
        // starting guess), and different builds may want different
        // sensitivity without recompiling. Settable properties, not
        // constructor parameters, so a host can wire these to Console
        // settings later without this class needing to know anything
        // about "settings" as a concept -- defaults match the values
        // this project already validated, so a host that never touches
        // them gets identical behavior to before this change.
        //
        // Invariant this class assumes but doesn't enforce (keep the
        // host/settings UI honest instead, not worth the bloat of
        // runtime validation here): LowToNormal > NormalToLow, and
        // CriticalToLow > LowToCritical -- the hysteresis bands need
        // to not invert, or Tier will oscillate every tick at the
        // boundary.
        public float NormalToLowThreshold { get; set; } = 90f;
        public float LowToNormalThreshold { get; set; } = 93f;
        public float LowToCriticalThreshold { get; set; } = 10f;
        public float CriticalToLowThreshold { get; set; } = 13f;

        // Ticks to hold downstream power on after the last qualifying
        // event, before Deep Idle can cut it again. Unconfirmed cadence
        // -- same flag the IC10 build carries for this exact constant
        // (ic10_airlock_setup_guide.md section 7: "20 is an unvalidated
        // starting guess"). Configurable for the same reason as the
        // Tier thresholds above.
        public int WakeHoldTicks { get; set; } = 20;

        public Tier CurrentTier { get; private set; } = Tier.Normal;

        private readonly IAirlockHost host;
        private int wakeHoldRemaining;

        // Tracks whether the *previous* tick was relying on
        // AllowPowerDownWhilePropped to skip forcing power on (i.e.
        // matched, saving-mode on, power possibly idle). Needed so a
        // later mismatch can be recognized as "the propped state just
        // broke" and force a wake, without treating every ordinary
        // not-matched tick (the ordinary majority case, propped-open
        // aside entirely) as a wake reason -- that would defeat Deep
        // Idle for everyone, not just people using this option.
        private bool wasIdlingWhileProppedOpen;

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
                    if (charge <= NormalToLowThreshold) CurrentTier = Tier.Low;
                    break;

                case Tier.Low:
                    if (charge >= LowToNormalThreshold) CurrentTier = Tier.Normal;
                    else if (charge <= LowToCriticalThreshold) CurrentTier = Tier.Critical;
                    break;

                case Tier.Critical:
                    if (charge > CriticalToLowThreshold) CurrentTier = Tier.Low;
                    break;
            }
        }

        // Ported from cycle.ic10's tierCrit/checkProp branches (lines
        // 33-42, 108-119) plus watcher.ic10's Gate/WakeHold block
        // (lines 89-109). Call after UpdateTier each tick.
        public void ApplyTierEffects()
        {
            host.SetWarningIndicator(CurrentTier);

            // Maintenance mode: Tier is still tracked and shown (the
            // call above), but nothing else in this method runs --
            // no forced power, no forced evacuation, no Propped-Open
            // management. Vanilla's own cycling underneath is
            // completely unaffected either way, so this is safe to
            // flip on/off at any time.
            if (host.MaintenanceModeEnabled) return;

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
            // AllowPowerDownWhilePropped only removes a STEADY match as
            // a wake reason -- a match that just broke (mismatchJustAppeared)
            // still forces a wake regardless of the setting, so a real
            // leak or reopened mismatch always gets caught. See
            // AllowPowerDownWhilePropped's doc comment above and
            // wasIdlingWhileProppedOpen's doc comment on the field.
            bool matchForcesWake = host.PropAtmosphereMatched && !host.AllowPowerDownWhilePropped;
            bool mismatchJustAppeared = wasIdlingWhileProppedOpen && !host.PropAtmosphereMatched;

            bool wakeRequested =
                host.ButtonEHeld || host.ButtonIHeld || host.ButtonCHeld ||
                host.VanillaCycleRequested || host.PresenceDetected ||
                matchForcesWake || mismatchJustAppeared;

            switch (CurrentTier)
            {
                case Tier.Critical:
                    // watcher.ic10: Tier == Critical always forces the
                    // gate on ("beq r0 2 forceHold"), zero button press
                    // needed -- the forced evacuation has to be able to
                    // run the Vent and unlock the doors regardless of
                    // whether anyone's there to press anything.
                    UpdateDownstreamPower(forceOn: true);
                    wasIdlingWhileProppedOpen = false; // doors are being closed, tracking no longer applies

                    // cycle.ic10: "bnez r8 endLoop" -- Button C held
                    // skips the forced evacuation this tick, matching
                    // the documented override behavior (someone caught
                    // inside gets to cancel the lockdown attempt). Power
                    // stays on either way -- only the evacuate/unlock
                    // action itself is skipped.
                    if (host.ButtonCHeld) return;

                    // Evacuating (relieving chamber pressure) is always
                    // safe regardless of temperature, so it's
                    // unconditional. UNLOCKING is the step that matters
                    // -- a player or the next cycle could walk straight
                    // into whatever's on the other side, so that's the
                    // one gated on SafeToUnlockTemperature (2026-08-05,
                    // project owner: pressure matching alone doesn't
                    // protect against an extreme-temperature
                    // environment). Doors stay evacuated-but-locked
                    // until temperature is confirmed safe, rechecked
                    // every tick this branch runs.
                    host.ForceEvacuate();
                    if (host.SafeToUnlockTemperature) host.UnlockDoors();
                    break;

                case Tier.Normal:
                    // watcher.ic10: Tier == Normal also forces the gate
                    // on continuously ("beq r0 0 forceHold") -- NOT
                    // Deep Idle behavior, confirmed explicitly in
                    // ic10_airlock_setup_guide.md step 8.2 ("this is
                    // *not* Deep Idle behavior, and shouldn't idle
                    // off"). Only Low tier below actually idles down.
                    UpdateDownstreamPower(forceOn: true);
                    wasIdlingWhileProppedOpen = false; // power's unconditionally on in Normal, not idling

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
                        wasIdlingWhileProppedOpen = false; // not actually idle-capable, so not "idling while propped" either
                        if (host.PropAtmosphereMatched) host.HoldBothDoorsOpen();
                        break;
                    }

                    // watcher.ic10: the only tier where the gate isn't
                    // unconditionally forced on -- a wake event resets
                    // the WakeHold countdown, otherwise it ticks down
                    // and cuts downstream power once it reaches zero.
                    // This is the actual Deep Idle power saving.
                    // wakeRequested already folds in matchForcesWake and
                    // mismatchJustAppeared (see their comments above),
                    // so a genuine atmosphere match (or a mismatch that
                    // just broke a prior AllowPowerDownWhilePropped
                    // state) keeps this branch awake on its own -- no
                    // separate condition needed here.
                    UpdateDownstreamPower(forceOn: wakeRequested);
                    if (host.PropAtmosphereMatched) host.HoldBothDoorsOpen();

                    // Recorded AFTER acting on PropAtmosphereMatched this
                    // tick, so next tick's mismatchJustAppeared check
                    // compares against what was actually true here, not
                    // a stale value from before this tick's HoldBothDoorsOpen
                    // call.
                    wasIdlingWhileProppedOpen =
                        host.PropAtmosphereMatched && host.AllowPowerDownWhilePropped;
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
