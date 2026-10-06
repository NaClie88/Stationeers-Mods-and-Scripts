using System.Runtime.CompilerServices;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidDualBattery.Patches
{
    // Human.RobotBattery is the single source every consumer reads: OnLifeTick drains it, Brain
    // stuns the droid when it's null/empty, the HUD shows it, DroidSleeper and the disposable
    // charger top it up. Vanilla returns the #5-slot battery, or if that's null/empty, a charged
    // battery held in either hand. We slot the extra battery in between: slot 1 -> slot 2 -> hands.
    [HarmonyPatch(typeof(Human), nameof(Human.RobotBattery), MethodType.Getter)]
    public static class RobotBatteryPatch
    {
        private enum Source { None, Slot1, Slot2, Other }

        // Last battery source per droid, only so verbose logging can report the switch once.
        private static readonly ConditionalWeakTable<Human, StrongBox<Source>> _lastSource =
            new ConditionalWeakTable<Human, StrongBox<Source>>();

        public static void Postfix(Human __instance, ref BatteryCell __result)
        {
            BatteryCell primary = __instance.UniformSlot?.Get<BatteryCell>();
            if (primary == null || primary.IsEmpty)
            {
                BatteryCell extra = ExtraBatterySlot.Get(__instance)?.Get<BatteryCell>();
                if (extra != null && !extra.IsEmpty)
                {
                    __result = extra;
                }
            }

            // StatusUpdates reads this getter for every species; only droids are interesting.
            if (__instance.IsArtificial && SaltysDroidDualBattery.VerboseLogging != null &&
                SaltysDroidDualBattery.VerboseLogging.Value)
            {
                ReportSwitch(__instance, primary, __result);
            }
        }

        private static void ReportSwitch(Human human, BatteryCell primary, BatteryCell result)
        {
            Source source = result == null ? Source.None
                : result == primary ? Source.Slot1
                : result == ExtraBatterySlot.Get(human)?.Get<BatteryCell>() ? Source.Slot2
                : Source.Other;
            StrongBox<Source> last = _lastSource.GetOrCreateValue(human);
            if (last.Value != source)
            {
                SaltysDroidDualBattery.Log("Droid " + human.name + " now running on: " + source +
                                           (result != null ? " (" + result.PowerStored.ToString("F0") + " J)" : ""));
                last.Value = source;
            }
        }
    }
}
