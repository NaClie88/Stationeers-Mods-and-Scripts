using AirlockCardMod;
using Xunit;

namespace AirlockCardMod.Tests;

public class TierHysteresisTests
{
    // Mirrors the IC10 dry-run's tick-by-tick trace: 100->91->90->89,
    // 92.9->93->94, 12->11->10->9, 10->12->13->14 - same boundary
    // values that caught the off-by-one-percentage-point bug in
    // watcher.ic10, run here against the C# port instead.
    private static (FakeAirlockHost host, FailsafeController ctrl) Make()
    {
        var host = new FakeAirlockHost();
        return (host, new FailsafeController(host));
    }

    [Theory]
    [InlineData(100, Tier.Normal)]
    [InlineData(91, Tier.Normal)]
    [InlineData(90, Tier.Low)]   // <=90 crosses into Low
    [InlineData(89, Tier.Low)]
    public void NormalToLow_boundary(float charge, Tier expected)
    {
        var (host, ctrl) = Make();
        host.DedicatedBatteryChargeRatio = charge;
        ctrl.UpdateTier();
        Assert.Equal(expected, ctrl.CurrentTier);
    }

    [Fact]
    public void LowToNormal_requiresStrictlyAbove93_not90()
    {
        var (host, ctrl) = Make();
        host.DedicatedBatteryChargeRatio = 90; // enter Low
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);

        host.DedicatedBatteryChargeRatio = 92.9f; // still below 93 - stays Low
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);

        host.DedicatedBatteryChargeRatio = 93; // >= 93 recovers
        ctrl.UpdateTier();
        Assert.Equal(Tier.Normal, ctrl.CurrentTier);
    }

    [Fact]
    public void LowToCritical_and_back_respectsHysteresisGap()
    {
        var (host, ctrl) = Make();
        host.DedicatedBatteryChargeRatio = 90; // Normal -> Low
        ctrl.UpdateTier();

        host.DedicatedBatteryChargeRatio = 11;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier); // 11 > 10, still Low

        host.DedicatedBatteryChargeRatio = 10;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Critical, ctrl.CurrentTier); // <=10 crosses

        host.DedicatedBatteryChargeRatio = 12;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Critical, ctrl.CurrentTier); // 12 not > 13, stays Critical

        host.DedicatedBatteryChargeRatio = 13;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Critical, ctrl.CurrentTier); // not strictly > 13

        host.DedicatedBatteryChargeRatio = 14;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier); // > 13 recovers
    }

    [Fact]
    public void CriticalRecoversToLow_neverDirectlyToNormal()
    {
        var (host, ctrl) = Make();
        host.DedicatedBatteryChargeRatio = 5;
        ctrl.UpdateTier(); // Normal->Low
        ctrl.UpdateTier(); // Low->Critical
        Assert.Equal(Tier.Critical, ctrl.CurrentTier);

        host.DedicatedBatteryChargeRatio = 100;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier); // one tier per tick, matches IC10 design
    }
}

public class CriticalTierTests
{
    private static (FakeAirlockHost, FailsafeController) MakeInCritical()
    {
        var host = new FakeAirlockHost { DedicatedBatteryChargeRatio = 5 };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier(); // Normal -> Low
        ctrl.UpdateTier(); // Low -> Critical
        Assert.Equal(Tier.Critical, ctrl.CurrentTier);
        return (host, ctrl);
    }

    [Fact]
    public void ForcesDownstreamPowerOn_withZeroButtonsPressed()
    {
        var (host, ctrl) = MakeInCritical();
        ctrl.ApplyTierEffects();
        Assert.Equal(true, host.LastDownstreamPower);
        Assert.Equal(1, host.ForceEvacuateCalls);
        Assert.Equal(1, host.UnlockDoorsCalls);
    }

    [Fact]
    public void ButtonCHeld_skipsEvacuateAndUnlock_butPowerStaysOn()
    {
        var (host, ctrl) = MakeInCritical();
        host.ButtonCHeld = true;
        ctrl.ApplyTierEffects();

        Assert.Equal(true, host.LastDownstreamPower); // power still forced on
        Assert.Equal(0, host.ForceEvacuateCalls);       // but evacuation skipped
        Assert.Equal(0, host.UnlockDoorsCalls);
    }

    [Fact]
    public void UnsafeTemperature_evacuatesButDoesNotUnlock()
    {
        var (host, ctrl) = MakeInCritical();
        host.SafeToUnlockTemperature = false;
        ctrl.ApplyTierEffects();

        Assert.Equal(1, host.ForceEvacuateCalls); // evacuation is unconditional
        Assert.Equal(0, host.UnlockDoorsCalls);   // unlock gated on temperature
    }

    [Fact]
    public void PropAtmosphereMatched_isIgnoredInCritical()
    {
        var (host, ctrl) = MakeInCritical();
        host.PropAtmosphereMatched = true;
        ctrl.ApplyTierEffects();
        Assert.Equal(0, host.HoldBothDoorsOpenCalls); // Propped-Open never applies in Critical
    }

    [Fact]
    public void MaintenanceMode_suspendsEverythingExceptIndicator()
    {
        var (host, ctrl) = MakeInCritical();
        host.MaintenanceModeEnabled = true;
        ctrl.ApplyTierEffects();

        Assert.Equal(0, host.ForceEvacuateCalls);
        Assert.Equal(0, host.UnlockDoorsCalls);
        Assert.Empty(host.DownstreamPowerHistory);
        Assert.Single(host.WarningIndicatorHistory); // indicator still updates
        Assert.Equal(Tier.Critical, host.WarningIndicatorHistory[0]);
    }
}

public class NormalTierTests
{
    private static (FakeAirlockHost, FailsafeController) Make()
    {
        var host = new FakeAirlockHost { DedicatedBatteryChargeRatio = 100 };
        return (host, new FailsafeController(host));
    }

    [Fact]
    public void ForcesDownstreamPowerOn_continuously_notDeepIdle()
    {
        var (host, ctrl) = Make();
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects();
        Assert.Equal(true, host.LastDownstreamPower);

        // Run several more ticks with nothing happening - should stay on,
        // never idle down the way Low tier would.
        for (int i = 0; i < 30; i++)
        {
            ctrl.UpdateTier();
            ctrl.ApplyTierEffects();
        }
        Assert.All(host.DownstreamPowerHistory, on => Assert.True(on));
    }

    [Fact]
    public void PropAtmosphereMatched_holdsBothDoorsOpen()
    {
        var (host, ctrl) = Make();
        host.PropAtmosphereMatched = true;
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects();
        Assert.Equal(1, host.HoldBothDoorsOpenCalls);
    }

    [Fact]
    public void PropBreaking_closesTheNonPreferredDoor_defaultsToExterior()
    {
        var (host, ctrl) = Make();
        host.PropAtmosphereMatched = true;
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects(); // propped open, no presence data

        host.PropAtmosphereMatched = false;
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects(); // breaks this tick

        Assert.Single(host.ClosedDoors);
        Assert.Equal(DoorSide.Exterior, host.ClosedDoors[0]); // safety-first default
    }

    [Fact]
    public void PropBreaking_prefersClosingOppositeOfLastUsedDoor()
    {
        var (host, ctrl) = Make();
        host.PropAtmosphereMatched = true;
        host.InteriorPresenceDetected = true; // someone just used the interior side
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects();

        host.InteriorPresenceDetected = false;
        host.PropAtmosphereMatched = false;
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects();

        Assert.Single(host.ClosedDoors);
        Assert.Equal(DoorSide.Exterior, host.ClosedDoors[0]); // keep Interior open, close Exterior
    }
}

public class LowTierDeepIdleTests
{
    private static (FakeAirlockHost, FailsafeController) MakeInLow()
    {
        var host = new FakeAirlockHost { DedicatedBatteryChargeRatio = 90 };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);
        return (host, ctrl);
    }

    [Fact]
    public void NoWakeSourceEver_powerNeverTurnsOn()
    {
        // Distinct from the expiry test below: this one never had power
        // on in the first place (wakeHoldRemaining starts at 0), so it
        // only proves the "stays off with nothing requesting a wake"
        // case, not the countdown-then-cut behavior.
        var (host, ctrl) = MakeInLow();
        for (int i = 0; i < 5; i++) ctrl.ApplyTierEffects();
        Assert.All(host.DownstreamPowerHistory, on => Assert.False(on));
    }

    [Fact]
    public void ButtonPress_holdsPowerForWakeHoldWindow_thenIdles()
    {
        var (host, ctrl) = MakeInLow();
        ctrl.WakeHoldTicks = 3;

        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects();
        Assert.Equal(true, host.LastDownstreamPower);

        host.ButtonEHeld = false;
        ctrl.ApplyTierEffects(); // tick 1 of hold
        Assert.Equal(true, host.LastDownstreamPower);
        ctrl.ApplyTierEffects(); // tick 2
        Assert.Equal(true, host.LastDownstreamPower);
        ctrl.ApplyTierEffects(); // tick 3
        Assert.Equal(true, host.LastDownstreamPower);
        ctrl.ApplyTierEffects(); // hold exhausted
        Assert.Equal(false, host.LastDownstreamPower);
    }

    [Theory]
    [InlineData(true, false, false, false, false)]   // ButtonE
    [InlineData(false, true, false, false, false)]   // ButtonI
    [InlineData(false, false, true, false, false)]   // ButtonC
    [InlineData(false, false, false, true, false)]   // VanillaCycleRequested
    [InlineData(false, false, false, false, true)]   // PresenceDetected
    public void AnyWakeSource_forcesPowerOn(bool e, bool i, bool c, bool vanilla, bool presence)
    {
        var (host, ctrl) = MakeInLow();
        host.ButtonEHeld = e;
        host.ButtonIHeld = i;
        host.ButtonCHeld = c;
        host.VanillaCycleRequested = vanilla;
        host.PresenceDetected = presence;
        ctrl.ApplyTierEffects();
        Assert.Equal(true, host.LastDownstreamPower);
    }

    [Fact]
    public void NoWakeButtonsWired_fallsBackToAlwaysOn()
    {
        var (host, ctrl) = MakeInLow();
        host.HasWakeButtons = false;
        for (int i = 0; i < 10; i++) ctrl.ApplyTierEffects();
        Assert.All(host.DownstreamPowerHistory, on => Assert.True(on));
    }

    [Fact]
    public void NoDownstreamController_fallsBackToAlwaysOn()
    {
        var (host, ctrl) = MakeInLow();
        host.HasDownstreamController = false;
        for (int i = 0; i < 10; i++) ctrl.ApplyTierEffects();
        Assert.All(host.DownstreamPowerHistory, on => Assert.True(on));
    }

    [Fact]
    public void PropMatched_holdsDoorsOpen_evenInLowTier()
    {
        // Explicit divergence from the original cycle.ic10, which only
        // checked this in Normal - documented in the source as an
        // intentional 2026-08-05 design change.
        var (host, ctrl) = MakeInLow();
        host.PropAtmosphereMatched = true;
        ctrl.ApplyTierEffects();
        Assert.Equal(1, host.HoldBothDoorsOpenCalls);
    }

    [Fact]
    public void AllowPowerDownWhilePropped_steadyMatch_doesNotForceWake()
    {
        var (host, ctrl) = MakeInLow();
        ctrl.WakeHoldTicks = 2;
        host.AllowPowerDownWhilePropped = true;
        host.PropAtmosphereMatched = true;

        for (int i = 0; i < 5; i++) ctrl.ApplyTierEffects();

        // Doors stay held open regardless...
        Assert.True(host.HoldBothDoorsOpenCalls > 0);
        // ...but downstream power is allowed to idle down despite the
        // steady match, since AllowPowerDownWhilePropped opts out of
        // treating "still matched" as its own wake reason.
        Assert.Equal(false, host.LastDownstreamPower);
    }

    [Fact]
    public void AllowPowerDownWhilePropped_mismatchAppearing_stillForcesWake()
    {
        var (host, ctrl) = MakeInLow();
        ctrl.WakeHoldTicks = 2;
        host.AllowPowerDownWhilePropped = true;
        host.PropAtmosphereMatched = true;

        for (int i = 0; i < 5; i++) ctrl.ApplyTierEffects();
        Assert.Equal(false, host.LastDownstreamPower); // idled down while matched

        host.PropAtmosphereMatched = false; // mismatch just appeared
        ctrl.ApplyTierEffects();
        Assert.Equal(true, host.LastDownstreamPower); // forces a wake regardless of the setting
    }
}

public class DoorOpenedTests
{
    [Fact]
    public void OnDoorOpened_extendsVentRelief()
    {
        var host = new FakeAirlockHost();
        var ctrl = new FailsafeController(host);
        ctrl.OnDoorOpened(DoorSide.Interior);
        Assert.Single(host.VentReliefCalls);
        Assert.Equal(DoorSide.Interior, host.VentReliefCalls[0]);
    }

    [Fact]
    public void OnDoorOpened_suppressedInMaintenanceMode()
    {
        var host = new FakeAirlockHost { MaintenanceModeEnabled = true };
        var ctrl = new FailsafeController(host);
        ctrl.OnDoorOpened(DoorSide.Exterior);
        Assert.Empty(host.VentReliefCalls);
    }
}
