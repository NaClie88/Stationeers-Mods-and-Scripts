using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Cysharp.Threading.Tasks;

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

        // REDESIGNED, 2026-08-08 (project owner, sourced real
        // Stationeers safety mechanics) -- this is no longer "does the
        // outer room's air match the inner room's air." Toxicity and
        // breathability in this game are governed by PARTIAL PRESSURE
        // (ratio x total pressure) against fixed physiological
        // thresholds, not by whether two sides happen to have the same
        // composition as each other. A room could "match" another room
        // and still be unsafe for a suit-less player if both happen to
        // be toxic together. So: pressure stays a *relative*,
        // configurable check between the two sides (there's no single
        // universal "safe" pressure -- different builds run at
        // different setpoints, and the real risk here is a violent
        // equalization, not the absolute number) -- everything else
        // (oxygen, toxic gases, temperature) is now an *absolute*
        // per-side safety check instead.
        //
        // Sourced thresholds (project owner, 2026-08-08):
        // - Oxygen: safe to breathe without a suit at partial pressure
        //   >= 16 kPa (12-16 kPa is a low-oxygen warning zone, not yet
        //   damaging; below 5-6 kPa causes unconsciousness).
        // - Pollutant: damage begins around 1 kPa partial pressure;
        //   recommended zero-damage safety margin is below 0.5 kPa.
        // - Volatiles (Methane's in-game classification): same
        //   mechanic, damage above ~1 kPa partial pressure, 0.5 kPa
        //   safety margin.
        // - CO2 and Oxygen itself are NOT modeled as toxic in this
        //   game (per project owner) -- not checked here. Nitrous
        //   Oxide dropped entirely (2026-08-08) -- not in the sourced
        //   toxic-gas list, the old IC10-ported ratio-matching check
        //   for it no longer applies to anything.
        public double MinSafeOxygenPartialPressureKPa { get; set; } = 16.0;
        public double MaxSafePollutantPartialPressureKPa { get; set; } = 0.5;
        public double MaxSafeVolatilesPartialPressureKPa { get; set; } = 0.5;

        // Still relative/configurable, not absolute -- see the redesign
        // note above for why pressure specifically stays this way.
        // Hysteresis pair, not a single cutoff (2026-08-08, real
        // in-game bug: confirmed chattering true/false every tick or
        // two with the diff sitting right at a hard boundary) --
        // PressureMatchTolerance is the narrower "enter" threshold,
        // PressureUnmatchTolerance the wider "exit" one, same
        // enter-narrow/exit-wide shape as the Tier hysteresis bands.
        public double PressureMatchTolerance { get; set; } = 2.0;
        public double PressureUnmatchTolerance { get; set; } = 4.0;

        private bool? _lastPropMatched;

        public bool PropAtmosphereMatched
        {
            get
            {
                // FIXED, 2026-08-08 -- real in-game bug: with no
                // awareness of vanilla's own cycle state, this could
                // fire (and HoldBothDoorsOpen() force both doors open)
                // WHILE a vanilla Pressurizing/Depressurizing coroutine
                // was actively pumping the chamber -- connecting the
                // target room straight into the vent's draw and letting
                // it keep pulling from the whole room instead of just
                // the sealed chamber it was supposed to be limited to.
                // Only ever consider propped-open once the airlock is
                // genuinely settled (arrived at one side, or Disabled)
                // -- never while actively mid-transition.
                var state = _control.AirlockControlState;
                bool cycleInProgress = state == AdvancedAirlockState.PressurizingInternal
                    || state == AdvancedAirlockState.DepressurizingInternal
                    || state == AdvancedAirlockState.PressurizingExternal
                    || state == AdvancedAirlockState.DepressurizingExternal;
                if (cycleInProgress) return false;

                FindGasSensors(out var outer, out var inner);
                if (outer == null || inner == null) return false;

                bool outerSafe = IsSideSafeForSuitlessPlayer(outer, out var outerReason);
                bool innerSafe = IsSideSafeForSuitlessPlayer(inner, out var innerReason);

                // Hysteresis, not a hard cutoff (2026-08-08, real
                // in-game bug: confirmed chattering true/false/true
                // every tick or two with the pressure diff sitting
                // right at the PressureMatchTolerance boundary, which
                // likely never let the doors visually finish opening
                // before being told to close again). Enter propped-open
                // at PressureMatchTolerance; once active, stay active
                // until the diff grows past PressureUnmatchTolerance
                // instead -- same "enter narrow, exit wide" shape as
                // the Tier hysteresis bands, just for this check.
                double diff = System.Math.Abs(outer.GetLogicValue(LogicType.Pressure) - inner.GetLogicValue(LogicType.Pressure));
                double tolerance = (_lastPropMatched == true) ? PressureUnmatchTolerance : PressureMatchTolerance;
                bool pressureMatches = diff <= tolerance;
                bool matched = outerSafe && innerSafe && pressureMatches;

                if (matched != _lastPropMatched)
                {
                    _lastPropMatched = matched;
                    UnityEngine.Debug.Log("[Salty's Advanced Airlock]: PROP-MATCH=" + matched
                        + " outerSafe=" + outerSafe + "(" + outerReason + ")"
                        + " innerSafe=" + innerSafe + "(" + innerReason + ")"
                        + " pressureDiff=" + diff.ToString("F2") + ",tolerance=" + tolerance.ToString("F1") + ",ok=" + pressureMatches);
                }
                return matched;
            }
        }

        // Partial pressure = ratio x total pressure -- the actual
        // mechanic governing toxicity/breathability, not the raw ratio
        // alone (a low-pressure room can have a high toxic-gas ratio
        // and still be safe; a high-pressure room can have a low ratio
        // and still be lethal).
        // EXTRA fail-safe, not the primary safety mechanism (project
        // owner, 2026-08-08): "the player should be engineering all
        // their processes so this edge case does not happen... all we
        // are doing with this is preserving resources of the gas still
        // in the room." Catches a slow leak (cold snap, a ruptured
        // pipe) that can leave a room numerically "safe" by the
        // instantaneous checks above for a long time while still
        // actively draining -- larger rooms leak slower for the same
        // severity, so a fixed kPa/check threshold won't scale
        // perfectly to every room size. Acceptable given this is
        // explicitly secondary, not the primary engineering control,
        // and by the time a build is using this convenience feature at
        // all it's expected to be well-established rather than
        // leak-prone. Tracked per-check (this project's own cadence,
        // ~250ms at TicksPerCheck=15), not per real second, so it
        // doesn't depend on simulation speed -- same convention as
        // WakeHoldTicks/ReidleDelayTicks.
        public double MaxSafePressureDeclineKPaPerCheck { get; set; } = 0.5;

        private readonly System.Collections.Generic.Dictionary<GasSensor, double> _lastPressureBySensor = new();

        private bool IsSideSafeForSuitlessPlayer(GasSensor sensor, out string reason)
        {
            double pressure = sensor.GetLogicValue(LogicType.Pressure);
            double o2Partial = sensor.GetLogicValue(LogicType.RatioOxygen) * pressure;
            double pollutantPartial = sensor.GetLogicValue(LogicType.RatioPollutant) * pressure;
            double volatilesPartial = sensor.GetLogicValue(LogicType.RatioMethane) * pressure;

            if (o2Partial < MinSafeOxygenPartialPressureKPa) { reason = "O2 " + o2Partial.ToString("F1") + "kPa < min"; return false; }
            if (pollutantPartial > MaxSafePollutantPartialPressureKPa) { reason = "Pollutant " + pollutantPartial.ToString("F2") + "kPa > max"; return false; }
            if (volatilesPartial > MaxSafeVolatilesPartialPressureKPa) { reason = "Volatiles " + volatilesPartial.ToString("F2") + "kPa > max"; return false; }
            if (!InSafeTemperatureRange(sensor)) { reason = "temperature out of range"; return false; }

            if (_lastPressureBySensor.TryGetValue(sensor, out var lastPressure))
            {
                double decline = lastPressure - pressure;
                _lastPressureBySensor[sensor] = pressure;
                if (decline > MaxSafePressureDeclineKPaPerCheck)
                {
                    reason = "pressure dropping " + decline.ToString("F2") + "kPa/check (possible leak)";
                    return false;
                }
            }
            else
            {
                _lastPressureBySensor[sensor] = pressure;
            }

            reason = "safe";
            return true;
        }

        // UNCONFIRMED (2026-08-08) -- unlike the oxygen/toxicity
        // thresholds above, this range was NOT sourced from the
        // project owner, just a placeholder guess (~-73C to ~77C),
        // same unvalidated status as this project's other configurable
        // defaults (WakeHoldTicks, ReidleDelayTicks). Flag for
        // correction if real numbers are available. Settable either
        // way so a build with a genuine thermal hazard can tune it
        // without recompiling.
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

                bool outerSafe = outer == null || InSafeTemperatureRange(outer);
                bool innerSafe = inner == null || InSafeTemperatureRange(inner);
                return outerSafe && innerSafe;
            }
        }

        private bool InSafeTemperatureRange(GasSensor sensor)
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

        // TEMP TROUBLESHOOTING (2026-08-08, project owner -- remove
        // once Low tier's phase behavior is fully verified, or leave
        // toggleable): flashes the LED while genuinely idling in Low
        // tier (downstream power actually off, not just Tier==Low --
        // Active phase also reports Tier.Low but keeps power on) so
        // idle-vs-active is visible from across the room, not just via
        // log. Set false to go back to a solid Tier color.
        public bool FlashLedWhileIdling { get; set; } = true;

        private bool _ledFlashOn;

        public void SetWarningIndicator(Tier tier)
        {
            // Logged on change (not every tick) so real Tier tracking is
            // visible/verifiable in-game via the log too, not just the
            // LED.
            if (_lastLoggedTier != tier)
            {
                _lastLoggedTier = tier;
                UnityEngine.Debug.Log("[Salty's Advanced Airlock]: Tier changed to " + tier);
            }

            FindLed(out var led);
            if (led == null) return;

            bool idling = false;
            if (FlashLedWhileIdling && tier == Tier.Low)
            {
                FindDownstreamController(out var downstream);
                idling = downstream != null && downstream.GetLogicValue(LogicType.On) == 0.0;
            }

            double color;
            if (idling)
            {
                _ledFlashOn = !_ledFlashOn;
                color = _ledFlashOn ? 5.0 /* Yellow */ : 7.0 /* Black, i.e. off */;
            }
            else
            {
                color = tier switch
                {
                    Tier.Normal => 2.0,   // Green
                    Tier.Low => 5.0,      // Yellow
                    Tier.Critical => 4.0, // Red
                    _ => 2.0,
                };
            }
            led.SetLogicValue(LogicType.Color, color);
        }

        public void SetDownstreamPower(bool on)
        {
            FindDownstreamController(out var downstream);
            if (downstream == null) return;
            OnServer.Interact(downstream.InteractOnOff, on ? 1 : 0);
        }

        // FIXED, 2026-08-08 (real near-miss reported by project owner:
        // "i genuinely almost blew up an in line tank"). Never
        // implemented before this -- meant nothing was ever relieving
        // each Active Vent's inline tank, so it just accumulated
        // pressure across every single cycle with nothing to bleed it
        // off, eventually reaching a dangerous over-pressure state.
        //
        // REVISED same day, target-driven instead of a blind fixed-
        // duration pulse: the original design note (GAP_ANALYSIS.md)
        // assumed live tank pressure wasn't readable at all -- wrong,
        // confirmed via decompile that ActiveVent (the real class
        // behind IPoweredVent) exposes it directly via
        // LogicType.PressureOutput (reads
        // ConnectedPipeNetwork.Atmosphere.PressureGassesAndLiquids).
        // Project owner explicitly wants elevated tank pressure
        // preserved for faster cycling, only relieved down to a safe
        // cap, not drained blindly on every door-open regardless of
        // whether it's even needed -- so this now checks first, only
        // engages the vent if actually over MaxSafeTankPressureKPa
        // (10 MPa, project owner, comfortably under the game's actual
        // structural burst limit of ~60.8 MPa / MAXPressureGasPipe),
        // and keeps relieving (checking periodically, not just once)
        // until back at or below it. MaxVentReliefDurationMs is a
        // safety bound so this can't run forever if something
        // unexpected happens (door closes again, network changes) --
        // not expected to normally be hit.
        //
        // Runs the SAME-side vent in "pressurize" mode (Mode=0,
        // InteractMode -- confirmed via decompile/EvacuateVent above
        // that Mode=1 is depressurize/evacuate, so Mode=0 is the other
        // direction: draws FROM the network/tank INTO the chamber),
        // same reasoning as before for why this moment is safe
        // regardless of magnitude: the door is open, venting excess
        // into the now-connected room is harmless.
        public double MaxSafeTankPressureKPa { get; set; } = 10000.0; // 10 MPa
        public int VentReliefCheckIntervalMs { get; set; } = 500;
        public int MaxVentReliefDurationMs { get; set; } = 30000;

        public void ExtendVentRelief(DoorSide side)
        {
            var vent = side == DoorSide.Exterior ? _control.ExteriorPoweredVent : _control.InteriorPoweredVent;
            if (vent == null) return;
            RelieveVentAsync(vent).Forget();
        }

        private async UniTaskVoid RelieveVentAsync(Assets.Scripts.Objects.Pipes.IPoweredVent vent)
        {
            if (!(vent is Assets.Scripts.Objects.Pipes.ILogicable logicable)) return;

            double tankPressure = logicable.GetLogicValue(LogicType.PressureOutput);
            if (tankPressure <= MaxSafeTankPressureKPa) return;

            OnServer.Interact(vent.InteractMode, 0);
            OnServer.Interact(vent.InteractOnOff, 1);

            int elapsedMs = 0;
            while (elapsedMs < MaxVentReliefDurationMs)
            {
                await UniTask.Delay(VentReliefCheckIntervalMs, ignoreTimeScale: false);
                elapsedMs += VentReliefCheckIntervalMs;
                tankPressure = logicable.GetLogicValue(LogicType.PressureOutput);
                if (tankPressure <= MaxSafeTankPressureKPa) break;
            }

            OnServer.Interact(vent.InteractOnOff, 0);
        }
    }
}
