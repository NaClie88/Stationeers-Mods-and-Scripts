using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace AirlockCardMod
{
    // Milestone 2 real-hardware wiring, first slice (2026-08-06):
    // dedicated battery + downstream Power Controller are now real,
    // everything else (buttons, presence sensors, temperature,
    // ForceEvacuate/UnlockDoors/HoldBothDoorsOpen/CloseDoor) still
    // reports the safe default documented on IAirlockHost itself (see
    // src/FailsafeController.cs) -- none of those have a confirmed
    // vanilla hook yet (PATCH_PLAN.md). Safe to wire the battery/
    // downstream-power pair alone: with HasWakeButtons still false,
    // FailsafeController's own Low-tier branch always forces
    // SetDownstreamPower(true) regardless of Tier (see
    // ApplyTierEffects -- Deep Idle specifically requires
    // HasWakeButtons, which nothing here can make true yet), so this
    // slice can only ever turn a downstream controller ON, never off
    // -- observable and reversible, not destructive, while Tier
    // tracking against a real battery gets proven out.
    internal sealed class AdvancedAirlockControlHost : IAirlockHost
    {
        private readonly AdvancedAirlockControl _control;
        private bool _loggedDiscovery;
        private Tier? _lastLoggedTier;

        public AdvancedAirlockControlHost(AdvancedAirlockControl control)
        {
            _control = control;
        }

        // Finds AreaPowerControl ("Power Controller"/"Area Power
        // Controller" -- confirmed the same class,
        // logic-network-reference/devices/power-controller.md)
        // instances reachable on the Console's own data network.
        //
        // CORRECTED, 2026-08-06 (in-game): the first version of this
        // scanned AdvancedAirlockControl.LinkedDevices -- confirmed
        // empirically in-game to always find nothing, even with real
        // Power Controllers correctly data-cabled to the Console.
        // Traced why via decompilation: LinkedDevices is populated
        // through AirlockControlBase.CanDeviceLink(Device device) =>
        // device is IAirlockDevice, a hard filter. Confirmed
        // IAirlockDevice is implemented only by Console, GasSensor,
        // Speaker, ActiveVent, PoweredVent, Door, WallLight --
        // AreaPowerControl was never going to pass this filter no
        // matter how it was wired, so this wasn't a wiring problem.
        // The real, unfiltered device list -- the same one IC10 chips
        // themselves read from -- is
        // Motherboard.ParentComputer.DeviceList() (IComputer's own
        // API, public field ParentComputer set by the Console the
        // card is installed in). Same "first found / second found"
        // role-assignment pattern as before (mirroring
        // AdvancedAirlockControl's own ExteriorPoweredVent/
        // InteriorPoweredVent assignment), just against the right
        // list this time. No explicit UI to assign roles yet: wire
        // one Power Controller for battery monitoring only, or two
        // (first = battery, second = downstream) for the full Deep
        // Idle behavior once buttons are wired too. Revisit once a
        // Console settings surface exists (see console-ui-mod).
        private void FindPowerControllers(out AreaPowerControl battery, out AreaPowerControl downstream)
        {
            battery = null;
            downstream = null;

            var deviceList = _control.ParentComputer?.DeviceList();
            if (deviceList == null) return;

            foreach (var logicable in deviceList)
            {
                if (!(logicable is AreaPowerControl apc)) continue;
                if (battery == null) battery = apc;
                else if (downstream == null)
                {
                    downstream = apc;
                    break;
                }
            }
        }

        public float DedicatedBatteryChargeRatio
        {
            get
            {
                FindPowerControllers(out var battery, out var downstream);
                LogDiscoveryOnce(battery, downstream);

                if (battery == null) return 100f;
                var cell = battery.Battery;
                if (cell == null) return 100f;
                return cell.PowerRatio * 100f;
            }
        }

        // One-time confirmation of what got auto-discovered, so wiring
        // can be verified in-game without a debugger attached. See
        // PATCH_PLAN.md's diagnostic-log technique.
        private void LogDiscoveryOnce(AreaPowerControl battery, AreaPowerControl downstream)
        {
            if (_loggedDiscovery) return;
            _loggedDiscovery = true;

            string batteryInfo = battery == null
                ? "none found (Tier will stay Normal)"
                : (battery.Battery == null
                    ? battery.DisplayName + " (no battery cell inserted -- Tier will stay Normal)"
                    : battery.DisplayName + ", charge=" + (battery.Battery.PowerRatio * 100f).ToString("F1") + "%");
            string downstreamInfo = downstream == null ? "none found" : downstream.DisplayName;

            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: HARDWARE -- dedicated battery: " + batteryInfo
                + " | downstream controller: " + downstreamInfo);
        }

        public bool ButtonEHeld => false;
        public bool ButtonIHeld => false;
        public bool ButtonCHeld => false;

        public bool HasWakeButtons => false;

        public bool HasDownstreamController
        {
            get
            {
                FindPowerControllers(out _, out var downstream);
                return downstream != null;
            }
        }

        public bool VanillaCycleRequested => false;
        public bool PresenceDetected => false;
        public bool PropAtmosphereMatched => false;
        public bool ExteriorPresenceDetected => false;
        public bool InteriorPresenceDetected => false;
        public bool AllowPowerDownWhilePropped => false;
        public bool MaintenanceModeEnabled => false;
        public bool SafeToUnlockTemperature => true;

        public void ForceEvacuate()
        {
            // Not wired yet -- needs vanilla's own evacuate mechanism
            // (PATCH_PLAN.md: AirlockControlState/SetFlag), and can't
            // currently be exercised for real anyway since neither
            // ButtonCHeld nor a genuinely drained real battery has
            // been tested against this Critical-tier path yet.
        }

        public void UnlockDoors()
        {
        }

        public void HoldBothDoorsOpen()
        {
        }

        public void CloseDoor(DoorSide side)
        {
        }

        public void SetWarningIndicator(Tier tier)
        {
            // Real indicator wiring is follow-up work. Logged on
            // change (not every tick) so real Tier tracking against a
            // live battery is visible/verifiable in-game.
            if (_lastLoggedTier == tier) return;
            _lastLoggedTier = tier;
            UnityEngine.Debug.Log("[Salty's Advanced Airlock]: Tier changed to " + tier);
        }

        public void SetDownstreamPower(bool on)
        {
            FindPowerControllers(out _, out var downstream);
            if (downstream == null) return;
            OnServer.Interact(downstream.InteractOnOff, on ? 1 : 0);
        }

        public void ExtendVentRelief(DoorSide side)
        {
        }
    }
}
