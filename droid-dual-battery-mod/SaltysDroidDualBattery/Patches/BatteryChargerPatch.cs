using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using CharacterCustomisation;
using HarmonyLib;
using Objects.Items;

namespace SaltysDroidDualBattery.Patches
{
    // The handheld Disposable Battery Charger picks its target with GetTargetBattery: for a droid,
    // human.RobotBattery unless that's (nearly) full, then the suit battery. With two slots,
    // RobotBattery is slot beta whenever alpha is empty, so -- like the Droid Sleeper before
    // DroidSleeperPatch -- a dead alpha could never be charged while beta had charge (found in
    // the 2026-10-06 bug hunt). Now: alpha if it isn't full, then beta, then vanilla's answer
    // (the suit battery, or a battery held in hand).
    [HarmonyPatch(typeof(DisposableBatteryCharger), nameof(DisposableBatteryCharger.GetTargetBattery))]
    public static class BatteryChargerPatch
    {
        // Vanilla's own "full" threshold in GetTargetBattery.
        private const float FullRatio = 0.99f;

        public static void Postfix(Human targetHuman, ref BatteryCell __result)
        {
            if (targetHuman == null || targetHuman.SpeciesClass != SpeciesClass.Robot)
            {
                return;
            }
            BatteryCell alpha = targetHuman.UniformSlot?.Get<BatteryCell>();
            if (alpha != null && alpha.PowerRatio <= FullRatio)
            {
                __result = alpha;
                return;
            }
            BatteryCell beta = ExtraBatterySlot.Get(targetHuman)?.Get<BatteryCell>();
            if (beta != null && beta.PowerRatio <= FullRatio)
            {
                __result = beta;
            }
        }
    }
}
