using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace AirlockCardMod
{
    // Milestone 2, real-hardware wiring. Downstream controller/buttons
    // (2026-08-06) are confirmed working in-game. Percentage-based Tier
    // staging runs on a dedicated Station Battery
    // (Assets.Scripts.Objects.Electrical.Battery -- ground truth
    // confirmed, logic-network-reference/ground-truth-database.md's
    // Battery entry), driving StationBatteryChargeRatio -- see
    // FailsafeController.cs's IAirlockHost interface for the full
    // reasoning. A Cable Analyser-driven brownout override
    // (BasePowerBrownout) was tried as a secondary immediate-escalation
    // signal and then reverted (2026-08-08, project owner) -- too
    // aggressive once the Station Battery gave a genuinely trustworthy
    // reading on its own. PresenceDetected (Occupancy Sensor) and the
    // door/vent primitives ForceEvacuate/UnlockDoors/LockDoors/OpenDoor/
    // CloseDoor are built from decompiled evidence of vanilla's own
    // AdvancedAirlockControl (Pressurizing/Depressurizing/WaitDoorClose/
    // AirlockControlState's IsOperable check) -- confirmed working
    // in-game (2026-08-07/08) via RequestCycleToward's vanilla-button
    // pass-through, still watch for edge cases per STATE_TABLE.md.
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

        // REMOVED, 2026-08-08 (project owner) -- a Cable Analyser-driven
        // BasePowerBrownout (FindCableAnalyser + the RequiredLoad >
        // PotentialLoad check) briefly lived here as a secondary,
        // immediate Critical override. Reverted: now that the Station
        // Battery below gives a genuinely trustworthy early-warning
        // charge reading, forcing Critical on every transient
        // demand/supply blip was too aggressive -- see
        // IAirlockHost.StationBatteryChargeRatio's doc comment and
        // GAP_ANALYSIS.md's "Design history" for the full reasoning.

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

        // Outer/Inner Gas Sensor pair -- same DisplayName-substring
        // convention already used for buttons (this build names
        // devices "Outer"/"Inner", not exact hashes). Both feed
        // PropAtmosphereMatched AND SafeToUnlockTemperature below --
        // GasSensor's own LogicType surface includes Temperature
        // directly alongside Pressure (logic-network-reference/
        // device-index.md), so no separate temperature sensor is
        // needed, confirmed 2026-08-08 (project owner).
        private void FindGasSensors(out GasSensor outer, out GasSensor inner)
        {
            outer = null;
            inner = null;
            var deviceList = _control.ParentComputer?.DeviceList();
            if (deviceList == null) return;

            foreach (var logicable in deviceList)
            {
                if (!(logicable is GasSensor sensor)) continue;
                string name = sensor.DisplayName ?? "";
                if (outer == null && name.IndexOf("Outer", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    outer = sensor;
                else if (inner == null && name.IndexOf("Inner", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    inner = sensor;
            }

            LogGasSensorDiscoveryOnce(outer, inner);
        }

        private bool _loggedGasSensorDiscovery;

        private void LogGasSensorDiscoveryOnce(GasSensor outer, GasSensor inner)
        {
            if (_loggedGasSensorDiscovery) return;
            _loggedGasSensorDiscovery = true;
            string outerInfo = outer == null ? "none found" : outer.DisplayName;
            string innerInfo = inner == null ? "none found" : inner.DisplayName;
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: GAS SENSORS -- Outer: " + outerInfo + " | Inner: " + innerInfo);
        }

        // Ported from airlock-ic10-scripts/gas_sensor.ic10's tolerance
        // chain (Custom Airlock V2 lineage) -- same fields, same
        // tolerances, not re-derived: Pressure (0.1), Temperature
        // (0.02), RatioOxygen/RatioPollutant/RatioMethane/
        // RatioNitrousOxide (0.005 each). RatioMethane confirmed the
        // real LogicType name via decompile (value 18) despite
        // device-index.md listing it as "RatioVolatiles" -- that's a
        // display-name/enum-name mismatch in that source, not a
        // different field. No sensor found on either side -> false,
        // same graceful degradation as everywhere else (matches
        // skipping gas_sensor.ic10's chip entirely in the IC10 build).
        private bool? _lastPropMatched;

        public bool PropAtmosphereMatched
        {
            get
            {
                FindGasSensors(out var outer, out var inner);
                if (outer == null || inner == null) return false;

                // TEMP DIAGNOSTIC (2026-08-08) -- first live test showed
                // Normal tier not entering Propped-Open with matched
                // atmosphere; logging each field's diff on any
                // true/false transition so a real tolerance failure is
                // visible instead of a flat "didn't happen."
                (bool ok, double diff)[] checks =
                {
                    (WithinTolerance(outer, inner, LogicType.Pressure, 0.1, out var dP), dP),
                    (WithinTolerance(outer, inner, LogicType.Temperature, 0.02, out var dT), dT),
                    (WithinTolerance(outer, inner, LogicType.RatioOxygen, 0.005, out var dO2), dO2),
                    (WithinTolerance(outer, inner, LogicType.RatioPollutant, 0.005, out var dPo), dPo),
                    (WithinTolerance(outer, inner, LogicType.RatioMethane, 0.005, out var dMe), dMe),
                    (WithinTolerance(outer, inner, LogicType.RatioNitrousOxide, 0.005, out var dN2O), dN2O),
                };
                bool matched = checks[0].ok && checks[1].ok && checks[2].ok && checks[3].ok && checks[4].ok && checks[5].ok;

                if (matched != _lastPropMatched)
                {
                    _lastPropMatched = matched;
                    UnityEngine.Debug.Log("[Salty's Advanced Airlock]: PROP-MATCH=" + matched
                        + " Pressure(diff=" + checks[0].diff.ToString("F3") + ",ok=" + checks[0].ok + ")"
                        + " Temperature(diff=" + checks[1].diff.ToString("F3") + ",ok=" + checks[1].ok + ")"
                        + " O2(diff=" + checks[2].diff.ToString("F4") + ",ok=" + checks[2].ok + ")"
                        + " Pollutant(diff=" + checks[3].diff.ToString("F4") + ",ok=" + checks[3].ok + ")"
                        + " Methane(diff=" + checks[4].diff.ToString("F4") + ",ok=" + checks[4].ok + ")"
                        + " N2O(diff=" + checks[5].diff.ToString("F4") + ",ok=" + checks[5].ok + ")");
                }
                return matched;
            }
        }

        private static bool WithinTolerance(GasSensor a, GasSensor b, LogicType type, double tolerance, out double diff)
        {
            diff = System.Math.Abs(a.GetLogicValue(type) - b.GetLogicValue(type));
            return diff <= tolerance;
        }

        // Reasonable default safe-to-open range in Kelvin (~-73C to
        // ~77C) -- NOT sourced from a specific vanilla safety
        // threshold, a rough starting guess same status as this
        // project's other unvalidated defaults (WakeHoldTicks,
        // ReidleDelayTicks). Settable so a build with a genuine thermal
        // hazard nearby can tune it without recompiling.
        public double MinSafeTemperatureKelvin { get; set; } = 200.0;
        public double MaxSafeTemperatureKelvin { get; set; } = 350.0;

        // Only gates Critical tier's UNLOCK step, never the
        // evacuate/depressurize step -- see IAirlockHost.
        // SafeToUnlockTemperature's doc comment. Checks whichever Gas
        // Sensors are actually found (same Outer/Inner pair
        // PropAtmosphereMatched uses) -- both need to be in range if
        // both are wired; a missing sensor just isn't checked (not
        // treated as unsafe), matching graceful degradation elsewhere.
        // Defaults true (original unconditional-unlock behavior) only
        // if NEITHER sensor is found at all.
        public bool SafeToUnlockTemperature
        {
            get
            {
                FindGasSensors(out var outer, out var inner);
                if (outer == null && inner == null) return true;

                bool outerSafe = outer == null || InSafeRange(outer);
                bool innerSafe = inner == null || InSafeRange(inner);
                return outerSafe && innerSafe;
            }
        }

        private bool InSafeRange(GasSensor sensor)
        {
            double t = sensor.GetLogicValue(LogicType.Temperature);
            return t >= MinSafeTemperatureKelvin && t <= MaxSafeTemperatureKelvin;
        }

        public bool ExteriorPresenceDetected => false;
        public bool InteriorPresenceDetected => false;
        public bool MaintenanceModeEnabled => false;

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
        private bool _loggedHoldBothDoorsOpen;

        public void HoldBothDoorsOpen()
        {
            if (!_loggedHoldBothDoorsOpen)
            {
                _loggedHoldBothDoorsOpen = true;
                UnityEngine.Debug.Log("[Salty's Advanced Airlock]: HoldBothDoorsOpen() called for the first time");
            }
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

        // REVISED, 2026-08-07 (project owner) -- no gate at all now,
        // every edge-triggered press just forwards straight to
        // vanilla's own ButtonCycleAirlock() (the exact method the
        // Console UI's own cycle button calls). The previous "blocked
        // if already at the requested side" check was wrong: pressing
        // your own side's button while already there is a real,
        // wanted action -- a courtesy send-back, cycling the airlock
        // away toward the other side for the next person waiting
        // there. Vanilla's own switch already does exactly the right
        // thing from every state with no help needed: from a
        // Pressurized* state it starts moving away (the send-back);
        // mid-transition it cancels/reverses that step (confirmed via
        // decompile) -- this is genuinely just "press the button,"
        // full stop, matching the real Console button 1:1.
        public void RequestCycleToward(DoorSide side)
        {
            // TEMP DIAGNOSTIC (2026-08-07) -- kept from the previous
            // pass while a separate "rapid re-press" question is still
            // open (does edge-detection miss fast repeated clicks).
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: CYCLE-REQUEST side=" + side
                + " state=" + _control.AirlockControlState
                + " IsOperable=" + _control.IsOperable
                + " extLocked=" + (_control.ExteriorAirlock?.IsLocked.ToString() ?? "null")
                + " intLocked=" + (_control.InteriorAirlock?.IsLocked.ToString() ?? "null")
                + " extOpen=" + (_control.ExteriorAirlock?.IsOpen.ToString() ?? "null")
                + " intOpen=" + (_control.InteriorAirlock?.IsOpen.ToString() ?? "null"));

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
