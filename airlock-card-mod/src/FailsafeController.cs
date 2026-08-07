// Fail-safe extension logic, ported from airlock-ic10-scripts/watcher.ic10 +
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
    }

    public enum DoorSide
    {
        Exterior,
        Interior,
    }

    // Everything this controller needs from the host card/circuit, kept
    // as an interface so this class has no compile-time dependency on
    // real game types. The eventual Harmony patch adapts the real
    // vanilla class to this shape -- see PATCH_PLAN.md for what each
    // member is expected to map to once real method/field names are
    // confirmed.
    public interface IAirlockHost
    {
        // Simplified fail-safe trigger (2026-08-07, project owner,
        // replacing the original percentage-based DedicatedBatteryChargeRatio
        // design below this comment in git history): vanilla gives no
        // way to read a battery's true remaining runway from a point
        // upstream of this project's own network boundary (an APC only
        // exposes its own logic on its power-SOURCE side -- see
        // logic-network-reference/modding-architecture-notes.md section
        // 2b -- so a downstream battery reads as artificially healthy
        // for as long as anything upstream is still supplying it,
        // giving almost no advance warning before a real failure).
        // Rather than risk an incorrect "still plenty of runway"
        // estimate that locks someone out, the design gave up on
        // graceful percentage-based staging entirely: a Cable Analyser
        // placed on the always-on backbone itself (the same network
        // this Console already reaches, no bridging needed) exposes
        // Required/Potential for that whole segment, and
        // Required > Potential -- a genuine brownout, worse than a
        // clean blackout because it drains what reserve exists while
        // delivering nothing -- is treated as maximally urgent every
        // single time, no severity staging.
        //
        // Safe default if no Cable Analyser is wired at all: false
        // (never trigger). Same reasoning as the property this
        // replaced: a host that can't find one should disable the
        // fail-safe layer by always looking healthy, not fail closed
        // into a permanent false lockdown.
        bool BasePowerBrownout { get; }

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
        //
        // Second role (2026-08-07, project owner): also drives Low
        // tier's re-idle decision -- once woken, Low tier stays awake
        // until this reads true at least once (someone genuinely
        // entered the chamber) and then false again (they left), only
        // then returning to the evacuate-and-idle sequence. Same
        // sensor, same always-on placement requirement, a second
        // consumer of the same reading rather than a separate field.
        bool PresenceDetected { get; }

        // True once Propped-Open's match condition is confirmed (see
        // gas_sensor.ic10's tolerance checks). Optional -- a host that
        // never sets this true just never enters Propped-Open, same as
        // skipping the Gas Sensor chip in the IC10 build.
        bool PropAtmosphereMatched { get; }

        // Optional, per-door presence tracking (2026-08-05, project
        // owner) -- distinct from the single generic PresenceDetected
        // above. Requires a SECOND pair of Presence/Motion Sensors, one
        // per door, mirroring the existing exterior/interior Gas Sensor
        // placement. Feeds the exit-ordering decision when Propped-Open
        // breaks: which door FailsafeController leaves open vs. closes.
        // Both default false if unwired -- see "Graceful degradation"
        // in GAP_ANALYSIS.md for what that falls back to.
        bool ExteriorPresenceDetected { get; }
        bool InteriorPresenceDetected { get; }

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
        //
        // ForceEvacuate() is expected to close AND lock both doors as
        // part of sealing the chamber, then run the vent(s) toward
        // vacuum -- locking is bundled in here rather than a separate
        // interface member because it's the same "seal it" duty every
        // time this gets called (2026-08-07, project owner's Low-tier
        // redesign: called every idle tick, same as it was for the
        // tier this replaced). Safe to call repeatedly/every tick --
        // not a one-shot action.
        void ForceEvacuate();
        void UnlockDoors();

        // Counterpart to UnlockDoors() (2026-08-07) -- re-locks both
        // doors. Needed specifically when Low tier wakes from idle:
        // ForceEvacuate()/UnlockDoors() leave doors unlocked (so a
        // fully depowered chamber can still be crowbarred open by a
        // player with no tools -- project owner, 2026-08-07), but
        // vanilla's own IsOperable check requires both doors LOCKED
        // before its own Pressurizing/Depressurizing cycling will run
        // at all. Re-locking on wake gives vanilla's normal cycling
        // its best chance of taking over once the player is inside and
        // using the Console/buttons normally. NOT yet confirmed
        // in-game whether this is sufficient on its own -- flagged as
        // a real open question, see modding-architecture-notes.md.
        void LockDoors();

        void HoldBothDoorsOpen();

        // Closes ONE specific door, leaves the other exactly as it was
        // (2026-08-05, project owner -- exit-ordering when Propped-Open
        // breaks). Not a lock -- same "close, don't lock" distinction
        // as everywhere else in this design. The door left alone isn't
        // actively re-commanded open either; it's simply not touched,
        // same as any other door this design isn't currently managing.
        void CloseDoor(DoorSide side);

        // Opens ONE specific door (2026-08-07, project owner's Low-tier
        // redesign) -- the wake-from-idle behavior only opens whichever
        // side's button was pressed, not both, unlike HoldBothDoorsOpen
        // below which is a Normal-tier convenience for a confirmed
        // atmosphere match. The other door isn't touched, same
        // "leave the rest alone" convention as CloseDoor.
        void OpenDoor(DoorSide side);

        void SetWarningIndicator(Tier tier);

        // NEW capability, no vanilla equivalent (see GAP_ANALYSIS.md)
        // -- switches a downstream APC/Power Controller feeding the
        // doors, Vent, and anything else that isn't the Console itself
        // or the Buttons. This is what actually delivers the power
        // saving; everything else on this interface is either
        // inherited from vanilla or informational.
        void SetDownstreamPower(bool on);

        // Extends that side's vent operation briefly so any excess
        // pressure in its inline storage tank bleeds into the
        // now-open room, instead of accumulating over repeated cycles
        // (2026-08-05, project owner -- inline air tank design behind
        // each Active Vent, used to speed up cycling). Deliberately NOT
        // gated on reading live tank pressure -- that capability isn't
        // confirmed to exist at all (Milestone 1.5 territory). Instead
        // this always runs, on the principle that relieving pressure at
        // a moment that's already safe by construction (a door that's
        // open is already connected to a room -- venting excess there
        // is harmless regardless of how much excess there is) beats
        // reacting to a threshold. See OnDoorOpened() below for when
        // this gets called.
        void ExtendVentRelief(DoorSide side);
    }

    public sealed class FailsafeController
    {
        // Ticks to hold downstream power on after the last qualifying
        // event, before Deep Idle can cut it again. Unconfirmed cadence
        // -- same flag the IC10 build carries for this exact constant
        // (ic10_airlock_setup_guide.md section 7: "20 is an unvalidated
        // starting guess"). Configurable, same reasoning as
        // ReidleDelayTicks below.
        public int WakeHoldTicks { get; set; } = 20;

        // Ticks to wait, once the chamber reads empty again after
        // having been occupied, before actually returning to the
        // evacuate-and-idle sequence (2026-08-07, project owner's
        // "short wait" after someone leaves). Unvalidated starting
        // guess, same status as WakeHoldTicks -- configurable rather
        // than fixed so a host can tune this without recompiling.
        public int ReidleDelayTicks { get; set; } = 20;

        public Tier CurrentTier { get; private set; } = Tier.Normal;

        private enum LowPowerPhase
        {
            // Powered off (subject to WakeHoldTicks), doors closed,
            // locked, then unlocked, vents running toward vacuum --
            // every tick, same as the old Critical tier always did.
            Idle,

            // Woken: power held on unconditionally, the requested
            // door opened, waiting for the chamber to be entered and
            // then vacated again before returning to Idle.
            Active,
        }

        private readonly IAirlockHost host;
        private int wakeHoldRemaining;
        private LowPowerPhase lowPowerPhase = LowPowerPhase.Idle;
        private bool hasBeenOccupiedSinceWake;
        private int reidleCountdown;

        // True whenever HoldBothDoorsOpen() was called on the PREVIOUS
        // tick (Normal tier only, since the 2026-08-07 redesign -- Low
        // tier no longer does propped-open handling at all). Used to
        // detect "Propped-Open just broke" as its own event, for the
        // exit-ordering decision below.
        private bool wasHoldingDoorsOpenLastTick;

        // Which door a presence sensor most recently saw someone at,
        // if ExteriorPresenceDetected/InteriorPresenceDetected are
        // wired at all (2026-08-05, project owner exit-ordering
        // feature). Null until the first detection; simple
        // last-write-wins if a host ever reports both true on the
        // exact same tick -- not worth more precision than that for an
        // edge case this minor.
        private DoorSide? lastDoorUsed;

        public FailsafeController(IAirlockHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        // Call whenever EITHER door opens, regardless of trigger source
        // -- the native button that comes on powered doors, the
        // Console UI, or this design's own Critical-tier logic
        // (2026-08-05, project owner: this design modifies the whole
        // airlock's behavior, not just the parts this class directly
        // triggers, and the inline-tank relief needs to cover every
        // cycle, not only the ones this class initiated). Separate
        // from UpdateTier()/ApplyTierEffects() -- this isn't part of
        // the per-tick loop, it's an event notification the patch
        // fires whenever it observes a door transition to open,
        // wherever in vanilla's code that turns out to happen.
        //
        // Suspended by MaintenanceModeEnabled, same as everything else
        // in this class -- a player doing manual construction work
        // shouldn't have this firing on them either.
        public void OnDoorOpened(DoorSide side)
        {
            if (host.MaintenanceModeEnabled) return;
            host.ExtendVentRelief(side);
        }

        // Call once per tick/update, same cadence as watcher.ic10's
        // main loop. Pure state transition -- no host calls here,
        // those happen in ApplyTierEffects below, mirroring the IC10
        // script's own split between computing r0 (Tier) and acting on
        // it afterward.
        public void UpdateTier()
        {
            bool brownout = host.BasePowerBrownout;
            Tier newTier = brownout ? Tier.Low : Tier.Normal;

            // Reset Low tier's own sub-state whenever brownout clears
            // entirely, so a later brownout always starts fresh from
            // Idle (evacuate) rather than resuming mid-Active as if
            // nothing happened -- deliberately conservative, matching
            // "treat every entry as maximally urgent."
            if (CurrentTier == Tier.Low && newTier == Tier.Normal)
            {
                lowPowerPhase = LowPowerPhase.Idle;
                hasBeenOccupiedSinceWake = false;
            }

            CurrentTier = newTier;
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

            // Exit-ordering tracking (2026-08-05, project owner) --
            // updated every tick regardless of Tier, since through-
            // traffic can happen any time, not just while Propped-Open
            // is active. Last-write-wins if a host ever reports both
            // sensors true on the exact same tick -- see lastDoorUsed's
            // doc comment on the field.
            if (host.ExteriorPresenceDetected) lastDoorUsed = DoorSide.Exterior;
            if (host.InteriorPresenceDetected) lastDoorUsed = DoorSide.Interior;

            // Wake sources for Low tier, only consulted when
            // HasWakeButtons is true (see below) -- the confirmed-safe
            // button reads, plus secondary sources that only ever add
            // wake opportunities, never remove the button-based
            // guarantee. PropAtmosphereMatched dropped from this list
            // (2026-08-07 redesign) -- Low tier no longer does any
            // propped-open handling, so it's Normal-tier-only now (see
            // that branch below); no longer a Low-tier wake source
            // either, kept simple rather than half-relevant.
            bool wakeRequested =
                host.ButtonEHeld || host.ButtonIHeld || host.ButtonCHeld ||
                host.VanillaCycleRequested || host.PresenceDetected;

            // Exit-ordering support (2026-08-05, project owner) -- only
            // meaningful for Normal tier's own propped-open handling
            // now, see that branch below.
            bool propOpenJustBroke = wasHoldingDoorsOpenLastTick && !host.PropAtmosphereMatched;

            switch (CurrentTier)
            {
                case Tier.Normal:
                    // watcher.ic10: Tier == Normal also forces the gate
                    // on continuously ("beq r0 0 forceHold") -- NOT
                    // Deep Idle behavior, confirmed explicitly in
                    // ic10_airlock_setup_guide.md step 8.2 ("this is
                    // *not* Deep Idle behavior, and shouldn't idle
                    // off"). Only Low tier below actually idles down.
                    UpdateDownstreamPower(forceOn: true);

                    if (host.PropAtmosphereMatched)
                    {
                        host.HoldBothDoorsOpen();
                        wasHoldingDoorsOpenLastTick = true;
                    }
                    else
                    {
                        // Exit-ordering (2026-08-05, project owner):
                        // only acts the one tick Propped-Open actually
                        // breaks -- propOpenJustBroke is false on every
                        // other not-matched tick (the ordinary case,
                        // propped-open aside entirely), so this doesn't
                        // fire on every idle tick, just the transition.
                        if (propOpenJustBroke) CloseNonPreferredDoor();
                        wasHoldingDoorsOpenLastTick = false;
                    }
                    break;

                case Tier.Low:
                    // Redesigned 2026-08-07 (project owner): Low tier no
                    // longer means "battery getting low" -- it means "a
                    // base-power brownout is happening right now,"
                    // treated with the same maximum urgency the old
                    // Critical tier reserved for a confirmed near-total
                    // failure (see BasePowerBrownout's doc comment for
                    // why graceful staging isn't possible in vanilla).
                    // PropAtmosphereMatched (the Normal-tier propped-open
                    // convenience) is deliberately NOT consulted here --
                    // during a real brownout the chamber should be at
                    // vacuum, not held open matched to atmosphere, so
                    // that field is left alone for Normal tier's own use.
                    wasHoldingDoorsOpenLastTick = false;

                    // Same graceful-degradation fallback as before:
                    // can't safely idle without both a confirmed-safe
                    // wake mechanism and something to actually switch,
                    // so just hold power on and don't attempt the
                    // evacuate sequence -- matches vanilla with no
                    // fail-safe layer at all, not a false lockdown.
                    if (!host.HasWakeButtons || !host.HasDownstreamController)
                    {
                        UpdateDownstreamPower(forceOn: true);
                        break;
                    }

                    switch (lowPowerPhase)
                    {
                        case LowPowerPhase.Idle:
                            UpdateDownstreamPower(forceOn: wakeRequested);

                            // cycle.ic10 lineage: Button C held skips
                            // the forced lockdown this tick -- someone
                            // caught inside gets to cancel it. Power
                            // stays on either way (the call above
                            // already ran) -- only evacuate/lock/unlock
                            // is skipped.
                            if (!host.ButtonCHeld)
                            {
                                // Evacuating is always safe regardless
                                // of temperature, so unconditional.
                                // UNLOCKING is gated on
                                // SafeToUnlockTemperature -- a player or
                                // the next cycle could walk straight
                                // into whatever's on the other side.
                                // Unlocked (not just closed) specifically
                                // so a fully depowered chamber can still
                                // be crowbarred open by a player with no
                                // tools (project owner, 2026-08-07) --
                                // the intended manual fallback once this
                                // mod's own safety margin runs out.
                                host.ForceEvacuate();
                                if (host.SafeToUnlockTemperature) host.UnlockDoors();
                            }

                            if (wakeRequested)
                            {
                                // Re-lock before opening -- vanilla's own
                                // IsOperable requires both doors locked
                                // before its Pressurizing/Depressurizing
                                // cycling will run at all, so this gives
                                // a normal cycle its best chance once
                                // the player is inside. NOT yet
                                // confirmed in-game whether this alone
                                // is sufficient -- see LockDoors()'s doc
                                // comment.
                                host.LockDoors();
                                if (host.ButtonEHeld) host.OpenDoor(DoorSide.Exterior);
                                if (host.ButtonIHeld) host.OpenDoor(DoorSide.Interior);
                                lowPowerPhase = LowPowerPhase.Active;
                                hasBeenOccupiedSinceWake = false;
                                reidleCountdown = ReidleDelayTicks;
                            }
                            break;

                        case LowPowerPhase.Active:
                            // Awake and (expected to be) in use --
                            // power held on unconditionally so vanilla's
                            // own cycling can actually run; this design
                            // doesn't drive the traversal itself once
                            // the requested door is open. Watching the
                            // occupancy sensor rather than a fixed
                            // timer for when it's safe to return to
                            // Idle -- a real cycle can take longer than
                            // any fixed hold, and cutting power mid-use
                            // is exactly the outcome this whole design
                            // exists to avoid.
                            UpdateDownstreamPower(forceOn: true);

                            if (host.PresenceDetected)
                            {
                                hasBeenOccupiedSinceWake = true;
                                reidleCountdown = ReidleDelayTicks;
                            }
                            else if (hasBeenOccupiedSinceWake)
                            {
                                // Someone genuinely entered and has now
                                // left -- short wait (ReidleDelayTicks)
                                // before actually returning to Idle,
                                // rather than snapping back the instant
                                // the sensor clears.
                                if (reidleCountdown > 0) reidleCountdown--;
                                if (reidleCountdown <= 0) lowPowerPhase = LowPowerPhase.Idle;
                            }
                            // else: woken but chamber never confirmed
                            // occupied yet (e.g. button just pressed,
                            // door still opening) -- stay Active and
                            // keep waiting, don't re-idle prematurely.
                            break;
                    }
                    break;
            }
        }

        // Favors keeping open whichever door was more recently used, if
        // tracked (ExteriorPresenceDetected/InteriorPresenceDetected
        // wired -- see lastDoorUsed). Otherwise defaults to the
        // safety-first choice: close Exterior (the vacuum/hostile
        // side), keep Interior open -- matches project owner's
        // "lean toward the inner door, most likely first assigned"
        // fallback (2026-08-05).
        private void CloseNonPreferredDoor()
        {
            DoorSide doorToClose = lastDoorUsed.HasValue
                ? (lastDoorUsed.Value == DoorSide.Exterior ? DoorSide.Interior : DoorSide.Exterior)
                : DoorSide.Exterior;

            host.CloseDoor(doorToClose);
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
