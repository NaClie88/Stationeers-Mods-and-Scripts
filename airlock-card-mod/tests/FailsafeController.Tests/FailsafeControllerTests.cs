using AirlockCardMod;
using Xunit;

namespace AirlockCardMod.Tests;

public class TierStagingTests
{
    // Percentage-based staging restored (2026-08-07, project owner) --
    // reads a dedicated Station Battery instead of an AreaPowerControl,
    // which is what makes graceful hysteresis staging trustworthy again
    // (see FailsafeController.cs's IAirlockHost.StationBatteryChargeRatio
    // doc comment for why). Same threshold defaults as the original
    // discarded design: 90/93 for Normal<->Low, 10/13 for Low<->Critical.
    [Fact]
    public void HighCharge_staysNormal()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 100f };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Normal, ctrl.CurrentTier);
    }

    [Fact]
    public void ChargeAtOrBelowNormalToLowThreshold_entersLow()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 90f };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);
    }

    [Fact]
    public void LowTier_chargeInHysteresisBand_staysLow()
    {
        // 91 is above NormalToLow(90) but below LowToNormal(93) -- the
        // hysteresis band exists specifically so this doesn't bounce.
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 50f };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);

        host.StationBatteryChargeRatio = 91f;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);
    }

    [Fact]
    public void LowTier_chargeRecoversAboveLowToNormalThreshold_returnsToNormal()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 50f };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);

        host.StationBatteryChargeRatio = 93f;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Normal, ctrl.CurrentTier);
    }

    [Fact]
    public void LowTier_chargeAtOrBelowLowToCriticalThreshold_entersCritical()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 50f };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);

        host.StationBatteryChargeRatio = 10f;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Critical, ctrl.CurrentTier);
    }

    [Fact]
    public void CriticalTier_chargeRecovering_returnsToLow_notDirectlyToNormal()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 50f };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        host.StationBatteryChargeRatio = 10f;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Critical, ctrl.CurrentTier);

        // Recovering all the way to a healthy charge in one tick still
        // only advances one tier -- has to pass back through Low, same
        // as the original design's hysteresis chain.
        host.StationBatteryChargeRatio = 100f;
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);
    }

    [Fact]
    public void LeavingLowTier_resetsToIdlePhase_notResumedActive()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 50f };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects(); // Idle tick

        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects(); // wakes -> Active
        host.ButtonEHeld = false;

        host.StationBatteryChargeRatio = 100f;
        ctrl.UpdateTier(); // recovers to Normal, leaving Low (resets sub-state)

        host.StationBatteryChargeRatio = 50f;
        ctrl.UpdateTier(); // back into Low

        // If this were still resumed mid-Active, power would stay on
        // unconditionally forever. A fresh Idle entry with no wake
        // source lets the wake-hold countdown run out and cut power.
        for (int i = 0; i < ctrl.WakeHoldTicks + 1; i++) ctrl.ApplyTierEffects();
        Assert.Equal(false, host.LastDownstreamPower);
    }
}

public class NormalTierTests
{
    private static (FakeAirlockHost, FailsafeController) Make()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 100f };
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
    public void ButtonE_requestsCycleTowardExterior()
    {
        var (host, ctrl) = Make();
        host.ButtonEHeld = true;
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects();
        Assert.Single(host.RequestedCycles);
        Assert.Equal(DoorSide.Exterior, host.RequestedCycles[0]);
    }

    [Fact]
    public void ButtonI_requestsCycleTowardInterior()
    {
        var (host, ctrl) = Make();
        host.ButtonIHeld = true;
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects();
        Assert.Single(host.RequestedCycles);
        Assert.Equal(DoorSide.Interior, host.RequestedCycles[0]);
    }

    [Fact]
    public void NoButtonsHeld_neverRequestsACycle()
    {
        var (host, ctrl) = Make();
        for (int i = 0; i < 10; i++)
        {
            ctrl.UpdateTier();
            ctrl.ApplyTierEffects();
        }
        Assert.Empty(host.RequestedCycles);
    }

    [Fact]
    public void ButtonHeldAcrossMultipleTicks_requestsCycleOnlyOnce()
    {
        // A LogicButton's Activate pulse can span more than one
        // ApplyTierEffects tick -- must not fire the cycle request
        // again on every tick the button still reads held, only on the
        // rising edge (2026-08-07, project owner: repeated ticks within
        // one physical press shouldn't double-fire vanilla's cycle
        // button, since a second real call mid-transition cancels it).
        var (host, ctrl) = Make();
        host.ButtonEHeld = true;
        ctrl.UpdateTier();
        ctrl.ApplyTierEffects();
        ctrl.ApplyTierEffects();
        ctrl.ApplyTierEffects();
        Assert.Single(host.RequestedCycles);

        // Release and press again -- a genuinely new press should fire again.
        host.ButtonEHeld = false;
        ctrl.ApplyTierEffects();
        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects();
        Assert.Equal(2, host.RequestedCycles.Count);
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

public class CriticalTierTests
{
    // Restored as its own tier (2026-08-07, project owner) -- this is
    // exactly the evacuate/unlock/Button-C-override behavior a brief
    // brownout-only redesign had temporarily folded into Low tier's
    // Idle phase, moved back out once percentage staging on a
    // trustworthy Station Battery could distinguish "a real crisis"
    // from "just getting low" again. Reaches Critical via the charge
    // chain directly -- one tick to Low, one more to Critical, same
    // one-tier-per-tick rule as everywhere else (a standalone Cable
    // Analyser brownout override existed briefly for a faster path in
    // here, then was reverted, 2026-08-08).
    private static (FakeAirlockHost, FailsafeController) MakeInCritical()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 5f };
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier(); // Normal -> Low
        ctrl.UpdateTier(); // Low -> Critical
        Assert.Equal(Tier.Critical, ctrl.CurrentTier);
        return (host, ctrl);
    }

    [Fact]
    public void ForcesPowerOn_evacuatesAndUnlocks()
    {
        var (host, ctrl) = MakeInCritical();
        ctrl.ApplyTierEffects();
        Assert.Equal(1, host.ForceEvacuateCalls);
        Assert.Equal(1, host.UnlockDoorsCalls);
        Assert.Equal(true, host.LastDownstreamPower);
    }

    [Fact]
    public void EveryTick_reRunsEvacuateAndUnlock()
    {
        // Matches the original design's Critical tier -- ForceEvacuate/
        // UnlockDoors are safe to call repeatedly.
        var (host, ctrl) = MakeInCritical();
        for (int i = 0; i < 5; i++) ctrl.ApplyTierEffects();
        Assert.Equal(5, host.ForceEvacuateCalls);
        Assert.Equal(5, host.UnlockDoorsCalls);
    }

    [Fact]
    public void ButtonCHeld_skipsEvacuateAndUnlock_powerStaysOn()
    {
        var (host, ctrl) = MakeInCritical();
        host.ButtonCHeld = true;
        ctrl.ApplyTierEffects();

        Assert.Equal(0, host.ForceEvacuateCalls);
        Assert.Equal(0, host.UnlockDoorsCalls);
        Assert.Equal(true, host.LastDownstreamPower);
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
    public void PropAtmosphereMatched_notConsultedInCriticalTier()
    {
        var (host, ctrl) = MakeInCritical();
        host.PropAtmosphereMatched = true;
        ctrl.ApplyTierEffects();
        Assert.Equal(0, host.HoldBothDoorsOpenCalls);
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

    [Fact]
    public void NoWakeButtonsOrController_stillEvacuates()
    {
        // Unlike Low tier, Critical never checks HasWakeButtons/
        // HasDownstreamController -- there's no wake-and-idle option to
        // fall back from in the first place, so those flags don't
        // change anything here.
        var (host, ctrl) = MakeInCritical();
        host.HasWakeButtons = false;
        host.HasDownstreamController = false;
        ctrl.ApplyTierEffects();
        Assert.Equal(1, host.ForceEvacuateCalls);
        Assert.Equal(true, host.LastDownstreamPower);
    }
}

public class LowTierIdleSavingTests
{
    // Low tier is back to its original meaning (2026-08-07, project
    // owner): "battery genuinely getting low," pure downstream power
    // idle-saving, no evacuation at all -- that moved to the restored
    // Critical tier above.
    private static (FakeAirlockHost, FailsafeController) MakeInLow()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 50f }; // between thresholds
        var ctrl = new FailsafeController(host);
        ctrl.UpdateTier();
        Assert.Equal(Tier.Low, ctrl.CurrentTier);
        return (host, ctrl);
    }

    [Fact]
    public void NoWakeSource_powerIdlesOff_noEvacuation()
    {
        var (host, ctrl) = MakeInLow();
        ctrl.ApplyTierEffects();
        Assert.Equal(0, host.ForceEvacuateCalls);
        Assert.Equal(0, host.UnlockDoorsCalls);
        Assert.Equal(false, host.LastDownstreamPower);
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
    public void MaintenanceMode_suspendsEverythingExceptIndicator()
    {
        var (host, ctrl) = MakeInLow();
        host.MaintenanceModeEnabled = true;
        ctrl.ApplyTierEffects();

        Assert.Empty(host.DownstreamPowerHistory);
        Assert.Single(host.WarningIndicatorHistory); // indicator still updates
        Assert.Equal(Tier.Low, host.WarningIndicatorHistory[0]);
    }
}

public class LowTierWakeAndReidleTests
{
    private static (FakeAirlockHost, FailsafeController) MakeInLow()
    {
        var host = new FakeAirlockHost { StationBatteryChargeRatio = 50f };
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
    public void WhileActive_furtherButtonPresses_stillRequestACycle()
    {
        // Real in-game bug, 2026-08-07: only the wake-triggering press
        // was ever routed anywhere; every press after that (while
        // already Active) had no door effect at all, breaking both
        // "cancel the current step" and "send it back to the other
        // side" once awake.
        var (host, ctrl) = MakeInLow();
        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects(); // wake -> Active
        host.ButtonEHeld = false;
        ctrl.ApplyTierEffects(); // release sampled
        host.RequestedCycles.Clear();

        host.ButtonIHeld = true;
        ctrl.ApplyTierEffects(); // a second, later press while Active
        Assert.Single(host.RequestedCycles);
        Assert.Equal(DoorSide.Interior, host.RequestedCycles[0]);
    }

    [Fact]
    public void OnceActive_powerStaysOn_regardlessOfButtonState_untilChamberVacatedAndDelayExpires()
    {
        var (host, ctrl) = MakeInLow();
        ctrl.ReidleDelayTicks = 2;

        host.ButtonEHeld = true;
        ctrl.ApplyTierEffects(); // wake -> Active
        host.ButtonEHeld = false;

        // Power stays on with no button held and nobody detected yet --
        // Active phase doesn't idle off on a fixed timer.
        for (int i = 0; i < 5; i++) ctrl.ApplyTierEffects();
        Assert.All(host.DownstreamPowerHistory, on => Assert.True(on));
        Assert.Equal(0, host.ForceEvacuateCalls); // Low tier never evacuates

        host.PresenceDetected = true; // someone genuinely entered
        ctrl.ApplyTierEffects();
        host.PresenceDetected = false; // and left

        ctrl.ApplyTierEffects(); // reidle tick 1
        Assert.Equal(true, host.LastDownstreamPower);
        ctrl.ApplyTierEffects(); // reidle tick 2, delay expires -> back to Idle
        Assert.Equal(true, host.LastDownstreamPower); // Active's own forceOn(true) already ran this tick, so the flip to Idle takes effect starting next tick

        ctrl.ApplyTierEffects(); // back in Idle phase now
        Assert.Equal(0, host.ForceEvacuateCalls); // Idle never evacuates in Low tier
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
        ctrl.ApplyTierEffects(); // wake -> Active
        host.ButtonEHeld = false;

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
