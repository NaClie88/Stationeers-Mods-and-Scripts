using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;
using Objects.Electrical;
using UnityEngine;

namespace SaltysDroidDualBattery.Patches
{
    // DroidSleeper.ChargeRobot tops up human.RobotBattery (the battery the droid is running on)
    // and then, the same way, the suit battery. With two slots that isn't enough: if #5 is dead
    // and slot 2 has charge, RobotBattery is slot 2, so vanilla charges slot 2 and nothing charges
    // #5 (found in testing). So both slot batteries are topped up explicitly, skipping whichever
    // one vanilla already charged this tick, the same way vanilla charges the suit battery:
    // up to _chargePerTick each, added to _powerUsedDuringTick so the sleeper draws it from the grid.
    [HarmonyPatch(typeof(DroidSleeper), "ChargeRobot")]
    public static class DroidSleeperPatch
    {
        private static readonly AccessTools.FieldRef<DroidSleeper, float> ChargePerTick =
            AccessTools.FieldRefAccess<DroidSleeper, float>("_chargePerTick");

        private static readonly AccessTools.FieldRef<DroidSleeper, float> PowerUsedDuringTick =
            AccessTools.FieldRefAccess<DroidSleeper, float>("_powerUsedDuringTick");

        // Capture what vanilla is about to charge: charging can flip RobotBattery's answer
        // (an empty #5 becomes non-empty), so reading it again afterwards isn't reliable.
        public static void Prefix(Human human, out BatteryCell __state)
        {
            __state = human.RobotBattery;
        }

        public static void Postfix(DroidSleeper __instance, Human human, BatteryCell __state)
        {
            Charge(__instance, human.UniformSlot?.Get<BatteryCell>(), __state);
            Charge(__instance, ExtraBatterySlot.Get(human)?.Get<BatteryCell>(), __state);
        }

        private static void Charge(DroidSleeper sleeper, BatteryCell battery, BatteryCell chargedByVanilla)
        {
            if (battery == null || battery == chargedByVanilla)
            {
                return;
            }
            float added = Mathf.Min(ChargePerTick(sleeper), battery.PowerMaximum - battery.PowerStored);
            if (added <= 0f)
            {
                return;
            }
            battery.AddPowerSafe(added);
            PowerUsedDuringTick(sleeper) += added;
        }
    }
}
