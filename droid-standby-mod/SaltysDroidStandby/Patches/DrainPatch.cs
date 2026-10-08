using Assets.Scripts;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec §4.2. Human.OnLifeTick drains `RobotBattery.PowerStored -= k x PowerDrainedPerTick`.
    // PowerDrainedPerTick is STATIC (shared by every human, also set by the headlamp), so it
    // must not be scaled per droid. Instead: remember the battery and its charge before the
    // tick, and refund the unspent share afterwards. Which battery drains is never changed,
    // so Dual Battery's alpha -> beta order, chargers and the sleeper are untouched.
    [HarmonyPatch(typeof(Human), nameof(Human.OnLifeTick))]
    public static class DrainPatch
    {
        public struct Before
        {
            public BatteryCell Battery;
            public float Stored;
            public float Factor;
        }

        // Runs on the life-tick worker thread like CognitionFloorPatch: no Unity object APIs,
        // ReferenceEquals instead of Unity's ==, and nothing may escape (final-review C1).
        public static void Prefix(Human __instance, out Before __state)
        {
            __state = default;
            try
            {
                if (!GameManager.RunSimulation || !__instance.IsArtificial) return;
                float factor = StandbyConfig.DrainFactor(StandbyRegistry.Get(__instance));
                if (factor >= 1f) return;
                BatteryCell battery = __instance.RobotBattery;
                if (ReferenceEquals(battery, null)) return;
                __state = new Before { Battery = battery, Stored = battery.PowerStored, Factor = factor };
            }
            catch (System.Exception e)
            {
                __state = default;
                SaltysDroidStandby.LogError("DrainPatch prefix: " + e.Message);
            }
        }

        public static void Postfix(Before __state)
        {
            try
            {
                if (ReferenceEquals(__state.Battery, null)) return;
                float spent = __state.Stored - __state.Battery.PowerStored;
                if (spent <= 0f) return;
                __state.Battery.PowerStored += spent * (1f - __state.Factor);
            }
            catch (System.Exception e)
            {
                SaltysDroidStandby.LogError("DrainPatch postfix: " + e.Message);
            }
        }
    }
}
