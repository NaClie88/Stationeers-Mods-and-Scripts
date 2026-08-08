using System.Collections.Generic;
using AirlockCardMod;

namespace AirlockCardMod.Tests;

// Hand-written test double for IAirlockHost. All inputs are plain
// mutable fields the test sets directly; all commands the controller
// issues are recorded so tests can assert on them without needing a
// mocking framework.
public sealed class FakeAirlockHost : IAirlockHost
{
    public float StationBatteryChargeRatio { get; set; } = 100f;
    public bool BasePowerBrownout { get; set; }
    public bool ButtonEHeld { get; set; }
    public bool ButtonIHeld { get; set; }
    public bool ButtonCHeld { get; set; }
    public bool HasWakeButtons { get; set; } = true;
    public bool HasDownstreamController { get; set; } = true;
    public bool VanillaCycleRequested { get; set; }
    public bool PresenceDetected { get; set; }
    public bool PropAtmosphereMatched { get; set; }
    public bool ExteriorPresenceDetected { get; set; }
    public bool InteriorPresenceDetected { get; set; }
    public bool MaintenanceModeEnabled { get; set; }
    public bool SafeToUnlockTemperature { get; set; } = true;

    public int ForceEvacuateCalls;
    public int UnlockDoorsCalls;
    public int LockDoorsCalls;
    public int HoldBothDoorsOpenCalls;
    public List<DoorSide> ClosedDoors { get; } = new();
    public List<DoorSide> OpenedDoors { get; } = new();
    public List<DoorSide> RequestedCycles { get; } = new();
    public List<Tier> WarningIndicatorHistory { get; } = new();
    public List<bool> DownstreamPowerHistory { get; } = new();
    public List<DoorSide> VentReliefCalls { get; } = new();

    public bool? LastDownstreamPower =>
        DownstreamPowerHistory.Count == 0 ? null : DownstreamPowerHistory[^1];

    public void ForceEvacuate() => ForceEvacuateCalls++;
    public void UnlockDoors() => UnlockDoorsCalls++;
    public void LockDoors() => LockDoorsCalls++;
    public void HoldBothDoorsOpen() => HoldBothDoorsOpenCalls++;
    public void CloseDoor(DoorSide side) => ClosedDoors.Add(side);
    public void OpenDoor(DoorSide side) => OpenedDoors.Add(side);
    public void RequestCycleToward(DoorSide side) => RequestedCycles.Add(side);
    public void SetWarningIndicator(Tier tier) => WarningIndicatorHistory.Add(tier);
    public void SetDownstreamPower(bool on) => DownstreamPowerHistory.Add(on);
    public void ExtendVentRelief(DoorSide side) => VentReliefCalls.Add(side);
}
