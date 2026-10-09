using Assets.Scripts;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec §4.2. Human.OnLifeTick drains `RobotBattery.PowerStored -= k x PowerDrainedPerTick`.
    // PowerDrainedPerTick is STATIC (shared by every human), so it must not be scaled per droid.
    // Instead: remember the battery and its charge before the tick, then settle afterwards with
    // BatteryMath.Adjustment -- the level factor scales the droid's own drain (Deep Standby's
    // factor 0 freezes it), and, while LightDrainFix is active, a lit helmet light adds its 5 %
    // share at full rate in every level (lights drain normally). Which battery drains is never changed,
    // so Dual Battery's alpha -> beta order, chargers and the sleeper are untouched.
    [HarmonyPatch(typeof(Human), nameof(Human.OnLifeTick))]
    public static class DrainPatch
    {
        public struct Before
        {
            public BatteryCell Battery;
            public float Stored;
            public float Factor;
            public bool LightOn;
        }

        // Runs on the life-tick worker thread like CognitionFloorPatch: no Unity object APIs,
        // ReferenceEquals instead of Unity's ==, and nothing may escape (final-review C1).
        public static void Prefix(Human __instance, out Before __state)
        {
            __state = default;
            try
            {
                if (!GameManager.RunSimulation || !__instance.IsArtificial) return;
                float factor = StandbyRegistry.DrainFactor(__instance, StandbyClock.Now); // ramp applied (Revision 3)
                bool lightFix = LightDrainFix.Active;
                if (factor >= 1f && !lightFix) return;
                BatteryCell battery = __instance.RobotBattery;
                if (ReferenceEquals(battery, null)) return;
                __state = new Before { Battery = battery, Stored = battery.PowerStored, Factor = factor, LightOn = lightFix && LightDrainFix.LightOn(__instance) };
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
                __state.Battery.PowerStored += BatteryMath.Adjustment(spent, __state.Factor, __state.LightOn);
                if (__state.Battery.PowerStored < 0f) __state.Battery.PowerStored = 0f;
            }
            catch (System.Exception e)
            {
                SaltysDroidStandby.LogError("DrainPatch postfix: " + e.Message);
            }
        }
    }
}
