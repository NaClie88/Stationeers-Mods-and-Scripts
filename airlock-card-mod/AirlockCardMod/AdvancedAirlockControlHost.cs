using Assets.Scripts.Objects.Motherboards;

namespace AirlockCardMod
{
    // Milestone 2 first cut. Every member below reports the safe
    // default documented on IAirlockHost itself (see
    // src/FailsafeController.cs) -- nothing here has a dedicated
    // battery, physical E/I/C buttons, a downstream APC, presence
    // sensors, or a temperature sensor wired yet, because none of
    // those have a confirmed vanilla hook (PATCH_PLAN.md). That makes
    // the whole fail-safe layer a deliberate no-op end to end: Tier
    // stays Normal forever, SetDownstreamPower(true) gets called every
    // throttled tick, nothing else happens. The point of this first
    // cut is proving the Harmony attachment itself runs cleanly
    // in-game without disturbing vanilla behavior -- real sensor and
    // button wiring is follow-up work once that's confirmed stable.
    internal sealed class AdvancedAirlockControlHost : IAirlockHost
    {
        private readonly AdvancedAirlockControl _control;

        public AdvancedAirlockControlHost(AdvancedAirlockControl control)
        {
            _control = control;
        }

        public float DedicatedBatteryChargeRatio => 100f;

        public bool ButtonEHeld => false;
        public bool ButtonIHeld => false;
        public bool ButtonCHeld => false;

        public bool HasWakeButtons => false;
        public bool HasDownstreamController => false;
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
            // Unreachable in this first cut -- Critical tier can never
            // be entered while DedicatedBatteryChargeRatio is
            // hardcoded to 100.
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
            // Real indicator wiring is follow-up work.
        }

        public void SetDownstreamPower(bool on)
        {
            // No downstream APC reference wired yet -- nothing to
            // actually switch.
        }

        public void ExtendVentRelief(DoorSide side)
        {
        }
    }
}
