using System;
using System.Runtime.CompilerServices;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidDualBattery.Patches
{
    // A wireless battery in one droid slot backs up the other slot's battery, like an area power
    // controller charging the battery it will fall back on. While the wireless battery holds 50% or
    // more, it moves up to 300 per power tick into the other slot's (non-wireless) battery. That's
    // the same amount an Omni transmitter feeds one wireless battery per tick (_batteryChargeRate).
    // Energy is moved, not created, and never takes the wireless battery below 50%.
    //
    // BatteryCell.OnPowerTick runs for every battery, including ones in a droid's slots, on the
    // power tick, off Unity's main thread. So: no UnityEngine.Object APIs (ReferenceEquals, not
    // ==), and catch everything, because an escaping exception skips electricity, logic and
    // atmosphere ticks world-wide.
    [HarmonyPatch(typeof(BatteryCell), nameof(BatteryCell.OnPowerTick))]
    public static class WirelessTopUpPatch
    {
        private const float TransferPerTick = 300f;
        private const float ReserveRatio = 0.5f;

        // Whether each wireless battery was topping up last tick, only so verbose logging can
        // report a start/stop once instead of every tick.
        private static readonly ConditionalWeakTable<BatteryCell, StrongBox<bool>> _active =
            new ConditionalWeakTable<BatteryCell, StrongBox<bool>>();

        private static bool _failed;

        public static void Postfix(BatteryCell __instance)
        {
            if (_failed || !(__instance is WirelessBattery wireless) || !GameManager.RunSimulation)
            {
                return;
            }
            try
            {
                Report(wireless, TopUp(wireless));
            }
            catch (Exception e)
            {
                // Logged once, then off for the session, so a bad tick can't spam the log.
                _failed = true;
                SaltysDroidDualBattery.Log("Wireless top-up threw -- disabled for this session");
                SaltysDroidDualBattery.Log(e.ToString());
            }
        }

        // Moves charge into the other slot's battery; returns the amount moved.
        private static float TopUp(WirelessBattery wireless)
        {
            Slot slot = wireless.ParentSlot;
            if (!(slot?.Parent is Human human))
            {
                return 0f;
            }
            Slot alpha = human.UniformSlot;
            Slot beta = ExtraBatterySlot.Get(human); // only droids have it
            if (alpha == null || beta == null)
            {
                return 0f;
            }

            Slot other;
            if (ReferenceEquals(slot, alpha))
            {
                other = beta;
            }
            else if (ReferenceEquals(slot, beta))
            {
                other = alpha;
            }
            else
            {
                return 0f; // in a hand, or some other slot
            }

            BatteryCell target = other.Get<BatteryCell>();
            if (ReferenceEquals(target, null) || target is WirelessBattery)
            {
                return 0f;
            }

            float spare = wireless.PowerStored - wireless.PowerMaximum * ReserveRatio;
            float amount = Math.Min(TransferPerTick, Math.Min(target.PowerDelta, spare));
            if (amount <= 0f)
            {
                return 0f;
            }

            // AddPowerSafe is the call the Omni uses on this same tick; it returns what didn't fit.
            float moved = amount - target.AddPowerSafe(amount);
            wireless.PowerStored -= moved;
            return moved;
        }

        private static void Report(WirelessBattery wireless, float moved)
        {
            if (SaltysDroidDualBattery.VerboseLogging == null || !SaltysDroidDualBattery.VerboseLogging.Value)
            {
                return;
            }
            StrongBox<bool> active = _active.GetOrCreateValue(wireless);
            bool now = moved > 0f;
            if (active.Value != now)
            {
                active.Value = now;
                SaltysDroidDualBattery.Log(now
                    ? "Wireless battery started topping up the other droid battery (" + wireless.PowerStored.ToString("F0") + " J left)"
                    : "Wireless battery stopped topping up (" + wireless.PowerStored.ToString("F0") + " J left)");
            }
        }
    }
}
