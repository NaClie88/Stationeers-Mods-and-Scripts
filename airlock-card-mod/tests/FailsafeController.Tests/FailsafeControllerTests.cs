using AirlockCardMod;
using Xunit;

namespace AirlockCardMod.Tests;

public class TierTriggerTests
{
    // Replaces the old percentage/hysteresis boundary tests -- Tier is
    // now a direct binary reflection of BasePowerBrownout, no staging
    // (2026-08-07 redesign, see FailsafeController.cs's IAirlockHost
    // interface for why graceful percentage-based staging isn't
    // possible in vanilla).
    [Fact]
    public void NoBrownout_staysNormal()
    {
        var host = new FakeAirlockHost { BasePowerBrownout = false };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Normal, ctrl.CurrentTier);
    }

    [Fact]
    public void Brownout_entersLowImmediately()
    {
        var host = new FakeAirlockHost { BasePowerBrownout = true };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);
    }

    [Fact]
    public void BrownoutClearing_returnsToNormal()
    {
        var host = new FakeAirlockHost { BasePowerBrownout = true };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);

        host.BasePowerBrownout = false;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Normal, ctrl.CurrentTier);
    }

    [Fact]
    public void BrownoutRecurring_resumesFromIdle_notMidActive()
    {
        // A later brownout should always start the full evacuate
        // sequence fresh, never resume mid-Active as if the earlier
        // wake was still in progress -- deliberately conservative.
        var host = new FakeAirlockHost { BasePowerBrownout = true };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects(); // Idle tick: ForceEvacuate/UnlockDoors run

        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects(); // wakes -> Active
        host.ButtonEHeld = false;

        host.BasePowerBrownout = false;
        ctrl.UpdateTier(); // clears entirely

        host.BasePowerBrownout = true;
        ctrl.UpdateTier(); // brownout again
        host.ForceEvacuateCalls = 0;
        ctrl.ApplyTierEffects();
        Assert.Equal(1, host.ForceEvacuateCalls); // ran again, i.e. back in Idle phase
    }
}

public class NormalTierTests
{
    private static (FakeAirlockHost, FailsafeController) Make()
    {
        var host = new FakeAirlockHost { BasePowerBrownout = false };
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

public class LowTierIdlePhaseTests
{
    // Idle phase absorbed the old Critical tier's evacuate/unlock/
    // Button-C-override behavior (2026-08-07 redesign) -- every
    // brownout is treated with the same maximum urgency a confirmed
    // near-total failure used to get, since vanilla gives no way to
    // measure how close to real failure a brownout actually is.
    private static (FakeAirlockHost, FailsafeController) MakeInLow()
    {
        var host = new FakeAirlockHost { BasePowerBrownout = true };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);
        return (host, ctrl);
    }

    [Fact]
    public void NoWakeSource_evacuatesAndUnlocks_powerStaysOff()
    {
        var (host, ctrl) = MakeInLow();
        ctrl.ApplyTierEffects();
        Assert.Equal(1, host.ForceEvacuateCalls);
        Assert.Equal(1, host.UnlockDoorsCalls);
        Assert.Equal(false, host.LastDownstreamPower);
    }

    [Fact]
    public void EveryIdleTick_reRunsEvacuateAndUnlock()
    {
        // Matches the old Critical tier's own every-tick call pattern --
        // ForceEvacuate/UnlockDoors are safe to call repeatedly.
        var (host, ctrl) = MakeInLow();
        for (int i = 0; i < 5; i++) ctrl.ApplyTierEffects();
        Assert.Equal(5, host.ForceEvacuateCalls);
        Assert.Equal(5, host.UnlockDoorsCalls);
    }

    [Fact]
    public void ButtonCHeld_skipsEvacuateAndUnlock_butPowerStillReflectsWakeState()
    {
        var (host, ctrl) = MakeInLow();
        host.ButtonCHeld = true;
        ctrl.ApplyTierEffects();

        Assert.Equal(0, host.ForceEvacuateCalls);
        Assert.Equal(0, host.UnlockDoorsCalls);
        Assert.Equal(true, host.LastDownstreamPower); // ButtonCHeld is itself a wake source
    }

    [Fact]
    public void UnsafeTemperature_evacuatesButDoesNotUnlock()
    {
        var (host, ctrl) = MakeInLow();
        host.SafeToUnlockTemperature = false;
        ctrl.ApplyTierEffects();

        Assert.Equal(1, host.ForceEvacuateCalls); // evacuation is unconditional
        Assert.Equal(0, host.UnlockDoorsCalls);   // unlock gated on temperature
    }

    [Fact]
    public void PropAtmosphereMatched_noLongerHoldsDoorsOpenInLowTier()
    {
        // Deliberate simplification (2026-08-07): during a real
        // brownout the chamber should be at vacuum, not held open
        // matched to atmosphere -- PropAtmosphereMatched only matters
        // in Normal tier now.
        var (host, ctrl) = MakeInLow();
        host.PropAtmosphereMatched = true;
        ctrl.ApplyTierEffects();
        Assert.Equal(0, host.HoldBothDoorsOpenCalls);
    }

    [Fact]
    public void MaintenanceMode_suspendsEverythingExceptIndicator()
    {
        var (host, ctrl) = MakeInLow();
        host.MaintenanceModeEnabled = true;
        ctrl.ApplyTierEffects();

        Assert.Equal(0, host.ForceEvacuateCalls);
        Assert.Equal(0, host.UnlockDoorsCalls);
        Assert.Empty(host.DownstreamPowerHistory);
        Assert.Single(host.WarningIndicatorHistory); // indicator still updates
        Assert.Equal(Tier.Low, host.WarningIndicatorHistory[0]);
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
}

public class LowTierWakeAndReidleTests
{
    private static (FakeAirlockHost, FailsafeController) MakeInLow()
    {
        var host = new FakeAirlockHost { BasePowerBrownout = true };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        return (host, ctrl);
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
    public void ButtonE_wakesAndOpensOnlyExteriorDoor_andRelocks()
    {
        var (host, ctrl) = MakeInLow();
        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects();

        Assert.Single(host.OpenedDoors);
        Assert.Equal(DoorSide.Exterior, host.OpenedDoors[0]);
        Assert.Equal(1, host.LockDoorsCalls); // re-locked for vanilla's IsOperable
    }

    [Fact]
    public void ButtonI_wakesAndOpensOnlyInteriorDoor()
    {
        var (host, ctrl) = MakeInLow();
        host.ButtonIHeld = true;
        ctrl.ApplyTierEffects();

        Assert.Single(host.OpenedDoors);
        Assert.Equal(DoorSide.Interior, host.OpenedDoors[0]);
    }

    [Fact]
    public void OnceActive_powerStaysOn_regardlessOfButtonState_untilChamberVacatedAndDelayExpires()
    {
        var (host, ctrl) = MakeInLow();
        ctrl.ReidleDelayTicks = 2;

        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects(); // wake -> Active (this tick's own ForceEvacuate() already ran as Idle's last act)
        host.ButtonEHeld = false;
        host.ForceEvacuateCalls = 0;

        // Power stays on with no button held and nobody detected yet --
        // Active phase doesn't idle off on a fixed timer anymore.
        for (int i = 0; i < 5; i++) ctrl.ApplyTierEffects();
        Assert.All(host.DownstreamPowerHistory, on => Assert.True(on));
        Assert.Equal(0, host.ForceEvacuateCalls); // never re-entered Idle

        host.PresenceDetected = true; // someone genuinely entered
        ctrl.ApplyTierEffects();
        host.PresenceDetected = false; // and left

        ctrl.ApplyTierEffects(); // reidle tick 1
        Assert.Equal(true, host.LastDownstreamPower);
        ctrl.ApplyTierEffects(); // reidle tick 2, delay expires -> back to Idle
        Assert.Equal(true, host.LastDownstreamPower); // Idle's own forceOn(wakeRequested=false)+wake-hold still covers this tick

        host.ForceEvacuateCalls = 0;
        ctrl.ApplyTierEffects();
        Assert.Equal(1, host.ForceEvacuateCalls); // back in Idle phase, evacuating again
    }

    [Fact]
    public void NeverConfirmedOccupied_staysActiveIndefinitely()
    {
        // e.g. button pressed but nobody actually walked in -- shouldn't
        // snap back to Idle just because PresenceDetected never went
        // true, since that would fight a player who's simply slow to
        // walk through.
        var (host, ctrl) = MakeInLow();
        ctrl.ReidleDelayTicks = 1;
        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects(); // wake tick's own ForceEvacuate() already ran as Idle's last act
        host.ButtonEHeld = false;
        host.ForceEvacuateCalls = 0;

        for (int i = 0; i < 10; i++) ctrl.ApplyTierEffects();
        Assert.Equal(0, host.ForceEvacuateCalls);
        Assert.All(host.DownstreamPowerHistory, on => Assert.True(on));
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
