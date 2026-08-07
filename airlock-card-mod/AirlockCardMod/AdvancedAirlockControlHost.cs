using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace AirlockCardMod
{
    // Milestone 2, real-hardware wiring. Battery/downstream (2026-08-06)
    // and buttons (2026-08-06) are confirmed working in-game. This pass
    // (2026-08-07) wires the brownout-triggered redesign: BasePowerBrownout
    // (Cable Analyser on the always-on backbone, replacing the old
    // percentage-based battery read -- see FailsafeController.cs's
    // IAirlockHost.BasePowerBrownout doc comment for why), PresenceDetected
    // (Occupancy Sensor), and the door/vent primitives ForceEvacuate/
    // UnlockDoors/LockDoors/OpenDoor/CloseDoor. The door/vent primitives are
    // built from decompiled evidence of vanilla's own AdvancedAirlockControl
    // (Pressurizing/Depressurizing/WaitDoorClose/AirlockControlState's
    // IsOperable check) but have NOT been exercised in-game yet -- flagged
    // per-method below where confidence is lower than the already-proven
    // patterns (buttons, power controller discovery).
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
        // 2b). No separate "battery" role anymore -- see
        // BasePowerBrownout below for why that concept was dropped
        // entirely (2026-08-07, project owner).
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
        // can't currently get enough power to meet its own demand.
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
                ? "none found (BasePowerBrownout will always read false -- Low power mode can never trigger)"
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

        // NOT YET VERIFIED IN-GAME -- OnServer.Interact(door.InteractOpen,
        // 1), the same call vanilla's own Pressurizing() uses to open
        // whichever side just finished its cycle. Called directly,
        // bypassing player-interaction validation (InteractWith's own
        // IsLocked check) the same way vanilla's own system-driven calls
        // do -- so this works regardless of the door's current lock
        // state.
        public void OpenDoor(DoorSide side)
        {
            SetDoorState(DoorForSide(side), open: true, locked: null);
        }

        private Assets.Scripts.Objects.Structures.Door DoorForSide(DoorSide side) =>
            side == DoorSide.Exterior ? _control.ExteriorAirlock : _control.InteriorAirlock;

        private static void SetDoorState(Assets.Scripts.Objects.Structures.Door door, bool open, bool? locked)
        {
            if (door == null) return;
            OnServer.Interact(door.InteractOpen, open ? 1 : 0);
            if (locked.HasValue) OnServer.Interact(door, InteractableType.Lock, locked.Value ? 1 : 0);
        }

        public void SetWarningIndicator(Tier tier)
        {
            // Real indicator wiring is follow-up work. Logged on
            // change (not every tick) so real Tier tracking is
            // visible/verifiable in-game.
            if (_lastLoggedTier == tier) return;
            _lastLoggedTier = tier;
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: Tier changed to " + tier);
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
