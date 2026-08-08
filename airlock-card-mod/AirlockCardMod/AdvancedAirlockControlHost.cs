using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace AirlockCardMod
{
    // Milestone 2, real-hardware wiring. Downstream controller/buttons
    // (2026-08-06) are confirmed working in-game. This pass (2026-08-07,
    // second one today) restores percentage-based Tier staging on top of
    // the brownout-triggered redesign from earlier the same day: a
    // dedicated Station Battery (Assets.Scripts.Objects.Electrical.Battery
    // -- ground truth confirmed, logic-network-reference/
    // ground-truth-database.md's Battery entry) now drives
    // StationBatteryChargeRatio, with the Cable Analyser's BasePowerBrownout
    // kept as a secondary immediate-override signal rather than removed --
    // see FailsafeController.cs's IAirlockHost interface for the full
    // reasoning on both. PresenceDetected (Occupancy Sensor) and the
    // door/vent primitives ForceEvacuate/UnlockDoors/LockDoors/OpenDoor/
    // CloseDoor are unchanged from that same pass. The door/vent primitives
    // are built from decompiled evidence of vanilla's own
    // AdvancedAirlockControl (Pressurizing/Depressurizing/WaitDoorClose/
    // AirlockControlState's IsOperable check) but have NOT been exercised
    // in-game yet -- flagged per-method below where confidence is lower
    // than the already-proven patterns (buttons, power controller
    // discovery).
    internal sealed class AdvancedAirlockControlHost : IAirlockHost
    {
        // Mirrors AdvancedAirlockControl's own private DefaultPressureMax
        // (50662.5) -- used as the vent's InternalPressure cap while
        // evacuating, same field vanilla's own Depressurizing() sets
        // before commanding the vent into evacuate mode. Not otherwise
        // meaningful on its own; it's a flow-rate-adjacent cap, not a
        // target (ExternalPressure=Zero is the actual vacuum target).
        private static readonly PressurekPa VentEvacuateCap = new PressurekPa(50662.5);

        private readonly AdvancedAirlockControl _control;
        private Tier? _lastLoggedTier;

        public AdvancedAirlockControlHost(AdvancedAirlockControl control)
        {
            _control = control;
        }

        // Finds the one AreaPowerControl ("Power Controller"/"Area Power
        // Controller") reachable on the Console's own data network --
        // confirmed via live NETDUMP (2026-08-07) that a real multi-tier
        // APC chain only ever exposes ONE controller here (the switchable
        // Sub APC directly upstream of the Console's own backbone; see
        // logic-network-reference/modding-architecture-notes.md section
        // 2b). This is purely the Deep-Idle power switch now -- the
        // "battery" monitoring role that used to live on an APC lives on
        // a separate, dedicated Station Battery instead, see
        // FindStationBattery/StationBatteryChargeRatio below (2026-08-07,
        // project owner, same day, second pass).
        private void FindDownstreamController(out AreaPowerControl downstream)
        {
            downstream = null;

            var deviceList = _control.ParentComputer?.DeviceList();
            if (deviceList == null) return;

            foreach (var logicable in deviceList)
            {
                if (logicable is AreaPowerControl apc)
                {
                    downstream = apc;
                    break;
                }
            }
        }

        private bool _loggedControllerDiscovery;

        private void LogControllerDiscoveryOnce(AreaPowerControl downstream)
        {
            if (_loggedControllerDiscovery) return;
            _loggedControllerDiscovery = true;
            string info = downstream == null ? "none found (Deep Idle can't run)" : downstream.DisplayName;
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: HARDWARE -- downstream controller: " + info);
        }

        public bool HasDownstreamController
        {
            get
            {
                FindDownstreamController(out var downstream);
                LogControllerDiscoveryOnce(downstream);
                return downstream != null;
            }
        }

        // BasePowerBrownout -- see FailsafeController.cs's IAirlockHost
        // interface for the full reasoning (2026-08-07, project owner).
        // Short version: a Cable Analyser placed on the always-on
        // backbone itself (same network as the Console -- no bridging
        // needed) exposes RequiredLoad/PotentialLoad for that whole
        // segment (Assets.Scripts.Objects.Electrical.CableAnalyser,
        // confirmed via decompile: single-cable-clamp device, no
        // separate connector, so it's only logic-readable from whatever
        // network it's physically clamped to -- must be the backbone,
        // not the true upstream base-power segment, or the Console
        // can't see it at all). Required > Potential means that segment
        // can't currently get enough power to meet its own demand. Kept
        // wired even after percentage staging came back (below) as a
        // secondary, immediate Critical override -- see
        // FailsafeController.UpdateTier.
        private void FindCableAnalyser(out CableAnalyser analyser)
        {
            analyser = null;
            var deviceList = _control.ParentComputer?.DeviceList();
            if (deviceList == null) return;

            foreach (var logicable in deviceList)
            {
                if (logicable is CableAnalyser found)
                {
                    analyser = found;
                    break;
                }
            }

            LogAnalyserDiscoveryOnce(analyser);
        }

        private bool _loggedAnalyserDiscovery;

        private void LogAnalyserDiscoveryOnce(CableAnalyser analyser)
        {
            if (_loggedAnalyserDiscovery) return;
            _loggedAnalyserDiscovery = true;
            string info = analyser == null
                ? "none found (BasePowerBrownout will always read false -- the immediate Critical override can never trigger, only the Station Battery percentage chain can)"
                : analyser.DisplayName;
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: ANALYSER -- " + info);
        }

        private bool _lastBrownout;

        public bool BasePowerBrownout
        {
            get
            {
                FindCableAnalyser(out var analyser);
                if (analyser == null) return false;

                bool brownout = analyser.RequiredLoad > analyser.PotentialLoad;
                if (brownout != _lastBrownout)
                {
                    _lastBrownout = brownout;
                    UnityEngine.Debug.Log("[Salty's Advanced Airlock]: Base power brownout " + (brownout ? "STARTED" : "cleared")
                        + " (required=" + analyser.RequiredLoad.ToString("F1") + "W, potential=" + analyser.PotentialLoad.ToString("F1") + "W)");
                }
                return brownout;
            }
        }

        // Finds the dedicated Station Battery ("Station Battery" in-game,
        // real class Assets.Scripts.Objects.Electrical.Battery -- ground
        // truth confirmed via logic-network-reference/ground-truth-
        // database.md's Battery entry, prefab ThingStructureBattery per
        // logic-network-reference/device-index.md) reachable on the
        // Console's own data network. Unlike the Sub APC above, this is
        // safe to find via the same DeviceList() scan without the
        // source-side wiring caveat -- project owner (2026-08-07): a
        // Station Battery has its Data IO as a fully separate port from
        // Power IN/Power OUT, so as long as its Data IO is run into this
        // card's data network at all, it's reachable regardless of which
        // power segment it's charging from. NOT independently verified
        // (2026-08-07) whether other battery structures (Battery Large/
        // Small, different prefabs/classes per device-index.md) also
        // match `is Battery` through some shared base class -- if a build
        // ever wires one of those alongside a real Station Battery on the
        // same data network, this scan could grab the wrong one. Flagged
        // here rather than guarded against, since it isn't decompile-
        // confirmed either way yet; the discovery log below makes a
        // wrong pick visible in-game if it ever happens.
        private void FindStationBattery(out Battery battery)
        {
            battery = null;
            var deviceList = _control.ParentComputer?.DeviceList();
            if (deviceList == null) return;

            foreach (var logicable in deviceList)
            {
                if (logicable is Battery found)
                {
                    battery = found;
                    break;
                }
            }
        }

        private bool _loggedBatteryDiscovery;

        private void LogBatteryDiscoveryOnce(Battery battery)
        {
            if (_loggedBatteryDiscovery) return;
            _loggedBatteryDiscovery = true;
            string info = battery == null
                ? "none found (Tier will stay Normal)"
                : battery.DisplayName + ", charge=" + (battery.GetLogicValue(LogicType.Ratio) * 100.0).ToString("F1") + "%";
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: HARDWARE -- Station Battery: " + info);
        }

        // Ratio, not Charge -- same gotcha as the original AreaPowerControl
        // design (logic-network-reference/devices/power-controller.md's
        // "Charge/Ratio gotcha" section): Charge folds in live input-network
        // power on top of the battery's own stored energy, inflating the
        // reading above the battery's true state of charge. Ratio
        // (PowerStored / PowerMaximum, ground-truth-database.md's Battery
        // entry) is the clean 0-1 fraction, confirmed to exist on this
        // class specifically via decompile, not assumed by analogy.
        public float StationBatteryChargeRatio
        {
            get
            {
                FindStationBattery(out var battery);
                LogBatteryDiscoveryOnce(battery);
                if (battery == null) return 100f;
                return (float)battery.GetLogicValue(LogicType.Ratio) * 100f;
            }
        }

        // ButtonEHeld/ButtonIHeld -- PATCH_PLAN.md confirmed there's no
        // vanilla Console-UI hook for these at all (ButtonEmergencyOverride
        // is a vestigial no-op, and E/I wake buttons have no vanilla
        // concept whatsoever). Wired directly to physical LogicButton
        // devices instead -- the same real hardware watcher.ic10 already
        // reads via its BtnHash/BtnEName/BtnIName constants. This build
        // names them "Outer Button"/"Inner Button" (matching the
        // Outer/Inner convention already used here for doors and gas
        // sensors) rather than watcher.ic10's exact "AirlockBtnE"/
        // "AirlockBtnI" names, so matching is by DisplayName substring,
        // not an exact hash. `Activate` is the LogicType read -- the same
        // one watcher.ic10 uses (`lbn ... Activate 0`) -- confirmed via
        // decompiling LogicButton: pressing sets it to 1 for a ~550ms
        // pulse (LogicButton.WaitThenStop), then back to 0 on release or
        // timeout, whichever comes first.
        private void FindButtons(out LogicButton buttonE, out LogicButton buttonI)
        {
            buttonE = null;
            buttonI = null;

            var deviceList = _control.ParentComputer?.DeviceList();
            if (deviceList == null) return;

            foreach (var logicable in deviceList)
            {
                if (!(logicable is LogicButton button)) continue;
                string name = button.DisplayName ?? "";
                if (buttonE == null && name.IndexOf("Outer", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    buttonE = button;
                else if (buttonI == null && name.IndexOf("Inner", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    buttonI = button;
            }

            LogButtonDiscoveryOnce(buttonE, buttonI);
        }

        private bool _loggedButtonDiscovery;
        private bool _lastButtonEHeld;
        private bool _lastButtonIHeld;

        private void LogButtonDiscoveryOnce(LogicButton buttonE, LogicButton buttonI)
        {
            if (_loggedButtonDiscovery) return;
            _loggedButtonDiscovery = true;

            string eInfo = buttonE == null ? "none found (Deep Idle can't run)" : buttonE.DisplayName;
            string iInfo = buttonI == null ? "none found" : buttonI.DisplayName;
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: BUTTONS -- E: " + eInfo + " | I: " + iInfo);
        }

        public bool ButtonEHeld
        {
            get
            {
                FindButtons(out var buttonE, out var buttonI);
                bool held = buttonE != null && buttonE.GetLogicValue(LogicType.Activate) != 0.0;
                if (held != _lastButtonEHeld)
                {
                    _lastButtonEHeld = held;
                    if (held) UnityEngine.Debug.Log("[Salty's Advanced Airlock]: Button E press detected");
                }
                return held;
            }
        }

        public bool ButtonIHeld
        {
            get
            {
                FindButtons(out var buttonE, out var buttonI);
                bool held = buttonI != null && buttonI.GetLogicValue(LogicType.Activate) != 0.0;
                if (held != _lastButtonIHeld)
                {
                    _lastButtonIHeld = held;
                    if (held) UnityEngine.Debug.Log("[Salty's Advanced Airlock]: Button I press detected");
                }
                return held;
            }
        }

        // Fallback only, not the primary plan -- PATCH_PLAN.md/README.md
        // Milestone 0.5: reusing vanilla's own Skip/Cancel (which already
        // exists and works) is the real mechanism for the Critical-tier
        // override, not a custom Button C. Stays false until/unless that
        // position changes.
        public bool ButtonCHeld => false;

        public bool HasWakeButtons
        {
            get
            {
                FindButtons(out var buttonE, out var buttonI);
                return buttonE != null || buttonI != null;
            }
        }

        // Occupancy Sensor -- confirmed via decompile (OccupancySensor:
        // IsTriggered => Activate > 0) that occupancy is read the exact
        // same way as buttons, GetLogicValue(LogicType.Activate). Not
        // name-matched (only one is expected/needed per chamber) --
        // first one found on the network. Drives both the optional
        // auto-wake role (FailsafeController's wakeRequested) and, more
        // importantly here, Low tier's re-idle decision -- see
        // IAirlockHost.PresenceDetected's doc comment.
        public bool PresenceDetected
        {
            get
            {
                var deviceList = _control.ParentComputer?.DeviceList();
                if (deviceList == null) return false;
                foreach (var logicable in deviceList)
                {
                    if (logicable is OccupancySensor sensor)
                        return sensor.GetLogicValue(LogicType.Activate) != 0.0;
                }
                return false;
            }
        }

        public bool VanillaCycleRequested => false;
        public bool PropAtmosphereMatched => false;
        public bool ExteriorPresenceDetected => false;
        public bool InteriorPresenceDetected => false;
        public bool MaintenanceModeEnabled => false;
        public bool SafeToUnlockTemperature => true;

        // NOT YET VERIFIED IN-GAME (2026-08-07) -- built from decompiled
        // evidence of vanilla's own Depressurizing()/WaitDoorClose(),
        // not from a live test. Closes and locks both doors (mirrors
        // WaitDoorClose's OnServer.Interact(door.InteractOpen, 0) and
        // AdvancedAirlockControl.OnDeviceListChanged's
        // OnServer.Interact(door, InteractableType.Lock, 1)), then
        // drives both powered vents toward vacuum the same way
        // Depressurizing() does (ExternalPressure=Zero,
        // InternalPressure=cap, InteractOnOff=1, InteractMode=1). Safe
        // to call every tick -- Interact calls are idempotent, matching
        // how the old Critical tier called this unconditionally every
        // tick it was active.
        public void ForceEvacuate()
        {
            SetDoorState(_control.ExteriorAirlock, open: false, locked: true);
            SetDoorState(_control.InteriorAirlock, open: false, locked: true);
            EvacuateVent(_control.ExteriorPoweredVent);
            EvacuateVent(_control.InteriorPoweredVent);
        }

        private static void EvacuateVent(Assets.Scripts.Objects.Pipes.IPoweredVent vent)
        {
            if (vent == null) return;
            vent.ExternalPressure = PressurekPa.Zero;
            vent.InternalPressure = VentEvacuateCap;
            OnServer.Interact(vent.InteractOnOff, 1);
            OnServer.Interact(vent.InteractMode, 1);
        }

        // NOT YET VERIFIED IN-GAME -- OnServer.Interact(door,
        // InteractableType.Lock, 0), the exact call vanilla's own
        // OnDeviceListChanged uses to unlock. Deliberately UNLOCKS
        // (doesn't just leave closed) so a fully depowered chamber can
        // still be crowbarred open by a player with no tools (project
        // owner, 2026-08-07) -- the intended manual fallback once this
        // mod's own safety margin runs out.
        public void UnlockDoors()
        {
            if (_control.ExteriorAirlock != null) OnServer.Interact(_control.ExteriorAirlock, InteractableType.Lock, 0);
            if (_control.InteriorAirlock != null) OnServer.Interact(_control.InteriorAirlock, InteractableType.Lock, 0);
        }

        // NOT YET VERIFIED IN-GAME -- counterpart to UnlockDoors above.
        // See IAirlockHost.LockDoors's doc comment for why Low tier
        // calls this on wake: vanilla's own IsOperable requires both
        // doors locked before its Pressurizing/Depressurizing cycling
        // will run, confirmed via decompile
        // (AdvancedAirlockControl.IsOperable), but whether re-locking
        // alone is sufficient for a normal cycle to resume smoothly
        // after a wake hasn't been tested live.
        public void LockDoors()
        {
            if (_control.ExteriorAirlock != null) OnServer.Interact(_control.ExteriorAirlock, InteractableType.Lock, 1);
            if (_control.InteriorAirlock != null) OnServer.Interact(_control.InteriorAirlock, InteractableType.Lock, 1);
        }

        // NOT YET VERIFIED IN-GAME -- opens both doors via the same
        // direct Interact call OpenDoor/CloseDoor use. Retained for
        // Normal tier's PropAtmosphereMatched convenience (unchanged by
        // the 2026-08-07 redesign) -- doesn't suppress any vanilla
        // auto-close timer, that's still an open question per
        // PATCH_PLAN.md.
        public void HoldBothDoorsOpen()
        {
            SetDoorState(_control.ExteriorAirlock, open: true, locked: null);
            SetDoorState(_control.InteriorAirlock, open: true, locked: null);
        }

        // NOT YET VERIFIED IN-GAME -- OnServer.Interact(door.InteractOpen,
        // 0), the same call vanilla's own WaitDoorClose uses. Doesn't
        // touch lock state, matching the "close, don't lock" distinction
        // documented on the interface.
        public void CloseDoor(DoorSide side)
        {
            SetDoorState(DoorForSide(side), open: false, locked: null);
        }

        // FIXED, 2026-08-07 (real in-game bug, project owner): closes
        // the OPPOSITE door first, unconditionally, before opening the
        // requested one -- opening a door without checking the other
        // side is not safe the way CloseDoor's "leave the other alone"
        // is. Confirmed live: a player approaching with the far door
        // still open, pressing the near side's button, opened the near
        // door too -- both open at once, connecting both sides straight
        // through the chamber, mixing gas. Vanilla's own Pressurizing()
        // never hits this case because it only ever runs after
        // WaitDoorClose() has already closed both doors; OpenDoor is
        // called directly (Low/Critical's wake path), bypassing that
        // guarantee, so it has to enforce it itself.
        //
        // Uses OnServer.Interact(door.InteractOpen, ...) directly,
        // bypassing player-interaction validation (InteractWith's own
        // IsLocked check) the same way vanilla's own system-driven calls
        // do -- so this works regardless of either door's current lock
        // state.
        // REVISED, 2026-08-07 (real in-game bug, project owner): a raw
        // "close opposite, open requested" swap isn't actually safe --
        // it prevents both doors being open at once, but doesn't
        // guarantee the chamber's current pressure matches the
        // requested side at all (e.g. Low tier's Idle phase no longer
        // evacuates on its own -- that moved to Critical -- so there's
        // no guarantee the chamber is at vacuum when a wake fires).
        // Only safe to open directly when BOTH doors are already
        // closed (Critical's own every-tick evacuate keeps this true
        // there; Low tier may or may not have it depending on what the
        // doors were doing before the wake). Otherwise, defer to
        // RequestCycleToward below, which drives vanilla's own
        // pressure-matching cycle instead of a blind swap.
        public void OpenDoor(DoorSide side)
        {
            bool bothClosed = !(_control.ExteriorAirlock?.IsOpen ?? false)
                && !(_control.InteriorAirlock?.IsOpen ?? false);
            if (bothClosed)
            {
                SetDoorState(DoorForSide(side), open: true, locked: null);
            }
            else
            {
                RequestCycleToward(side);
            }
        }

        // NOT YET VERIFIED IN-GAME -- reuses vanilla's own
        // ButtonCycleAirlock() (the exact method the Console UI's own
        // cycle button calls, confirmed via decompile) rather than
        // forcing a door open directly, since chamber pressure isn't
        // always guaranteed to already match the requested side (see
        // OpenDoor above). Gate NARROWED 2026-08-07 (project owner:
        // "make sure one can cancel each step... separately just like
        // the kit console button") -- only blocks the call when
        // already fully arrived at the requested side (Pressurized*),
        // not for the whole "heading that way" set. Vanilla's own
        // button, called again mid-transition, CANCELS/REVERSES that
        // transition (confirmed via decompile) -- that's the intended
        // per-step cancel behavior, so repeated presses during a
        // transition need to keep reaching ButtonCycleAirlock(), not be
        // suppressed. (An earlier version's broader gate blocked exactly
        // this.) Relies on the caller (FailsafeController) to already be
        // edge-triggering presses -- see buttonEPressed/buttonIPressed
        // in ApplyTierEffects -- so this doesn't need its own
        // once-per-press bookkeeping.
        public void RequestCycleToward(DoorSide side)
        {
            var state = _control.AirlockControlState;
            if (side == DoorSide.Exterior && state == AdvancedAirlockState.PressurizedExternal) return;
            if (side == DoorSide.Interior && state == AdvancedAirlockState.PressurizedInternal) return;
            _control.ButtonCycleAirlock();
        }

        private Assets.Scripts.Objects.Structures.Door DoorForSide(DoorSide side) =>
            side == DoorSide.Exterior ? _control.ExteriorAirlock : _control.InteriorAirlock;

        private static void SetDoorState(Assets.Scripts.Objects.Structures.Door door, bool open, bool? locked)
        {
            if (door == null) return;
            OnServer.Interact(door.InteractOpen, open ? 1 : 0);
            if (locked.HasValue) OnServer.Interact(door, InteractableType.Lock, locked.Value ? 1 : 0);
        }

        // ColorGreen/ColorYellow/ColorRed = 2/5/4 -- confirmed live
        // (2026-08-06) from GameManager.CustomColors while closing out
        // the IC10 build's own LED indicator (airlock-ic10-scripts/
        // ic10_airlock_code_notes.md), same ordinals reused here, not
        // re-derived. Diode is the LED's real class (confirmed via
        // in-game NETDUMP, PrefabHash 1944485013) -- Color is part of
        // the shared DynamicThing write surface every device has
        // (base-behavior.md), not something specific to Diode, so no
        // separate decompile needed to confirm the write itself works.
        private void FindLed(out Assets.Scripts.Objects.Structures.Diode led)
        {
            led = null;
            var deviceList = _control.ParentComputer?.DeviceList();
            if (deviceList == null) return;
            foreach (var logicable in deviceList)
            {
                if (logicable is Assets.Scripts.Objects.Structures.Diode found)
                {
                    led = found;
                    return;
                }
            }
        }

        public void SetWarningIndicator(Tier tier)
        {
            // Logged on change (not every tick) so real Tier tracking is
            // visible/verifiable in-game via the log too, not just the
            // LED.
            if (_lastLoggedTier == tier) return;
            _lastLoggedTier = tier;
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: Tier changed to " + tier);

            FindLed(out var led);
            if (led == null) return;
            double color = tier switch
            {
                Tier.Normal => 2.0,   // Green
                Tier.Low => 5.0,      // Yellow
                Tier.Critical => 4.0, // Red
                _ => 2.0,
            };
            led.SetLogicValue(LogicType.Color, color);
        }

        public void SetDownstreamPower(bool on)
        {
            FindDownstreamController(out var downstream);
            if (downstream == null) return;
            OnServer.Interact(downstream.InteractOnOff, on ? 1 : 0);
        }

        public void ExtendVentRelief(DoorSide side)
        {
        }
    }
}
