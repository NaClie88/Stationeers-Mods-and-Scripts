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

        public static void Prefix(Human __instance, out Before __state)
        {
            __state = default;
            if (!GameManager.RunSimulation || !__instance.IsArtificial) return;
            float factor = StandbyConfig.DrainFactor(StandbyRegistry.Get(__instance));
            if (factor >= 1f) return;
            BatteryCell battery = __instance.RobotBattery;
            if (battery == null) return;
            __state = new Before { Battery = battery, Stored = battery.PowerStored, Factor = factor };
        }

        public static void Postfix(Before __state)
        {
            if (__state.Battery == null) return;
            float spent = __state.Stored - __state.Battery.PowerStored;
            if (spent <= 0f) return;
            __state.Battery.PowerStored += spent * (1f - __state.Factor);
        }
    }
}
