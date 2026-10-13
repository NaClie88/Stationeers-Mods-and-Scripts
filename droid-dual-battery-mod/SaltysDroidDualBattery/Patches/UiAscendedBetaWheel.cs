using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;
using HarmonyLib;

namespace SaltysDroidDualBattery.Patches
{
    // Battery β as "contents" of UI Ascended's #5 wheel: the same slot entry UI Ascended builds
    // for each slot of a bag (take to hand, drop onto, stow from hand). With α empty the wheel
    // lists both slots, and it opens as long as β holds a battery.
    public static class UiAscendedBetaWheel
    {
        private const string WheelTypeName = "StationeersUIMod.Features.EquipmentKeyRadialFeature";
        private const string MenuBuilderTypeName = "StationeersUIMod.Features.ItemMenuBuilder";
        private const string BatteryButton = "UniformSlot";

        private static readonly HookGuard _buildGuard = new HookGuard("β wheel entry");
        private static readonly HookGuard _openGuard = new HookGuard("β wheel open");

        private static FieldInfo _buttonName;
        private static MethodInfo _buildSlotEntry;
        private static FieldInfo _sublabel;

        public static void Apply(Harmony harmony)
        {
            Type wheel = AccessTools.TypeByName(WheelTypeName);
            Type builder = AccessTools.TypeByName(MenuBuilderTypeName);
            _buttonName = wheel != null ? AccessTools.Field(wheel, "_buttonName") : null;
            _buildSlotEntry = builder != null ? AccessTools.Method(builder, "BuildSlotEntry", new[] { typeof(Slot) }) : null;
            if (_buttonName == null || _buildSlotEntry == null)
            {
                SaltysDroidDualBattery.Log("UI Ascended compat: #5 wheel members not found -- β wheel entry skipped");
                return;
            }
            _sublabel = AccessTools.Field(_buildSlotEntry.ReturnType, "Sublabel");

            UiAscendedCompat.Patch(harmony, AccessTools.Method(wheel, "BuildRoot"),
                typeof(UiAscendedBetaWheel), nameof(BuildRootPostfix), "β wheel entry");
            UiAscendedCompat.Patch(harmony, AccessTools.Method(wheel, "CanOpen"),
                typeof(UiAscendedBetaWheel), nameof(CanOpenPostfix), "β wheel open");
            UiAscendedCompat.Patch(harmony, AccessTools.PropertyGetter(wheel, "Title"),
                typeof(UiAscendedBetaWheel), nameof(TitlePostfix), "β wheel title");
        }

        private static readonly HookGuard _titleGuard = new HookGuard("β wheel title");

        // With α empty the wheel's title falls back to the raw key name ("UniformSlot").
        private static void TitlePostfix(object __instance, ref string __result)
        {
            if (__result != BatteryButton || _titleGuard.Disabled)
            {
                return;
            }
            try
            {
                if (BetaSlotFor(__instance, out _) != null)
                {
                    __result = BatteryLabels.BatteryName();
                }
            }
            catch (Exception e)
            {
                _titleGuard.Fail(e);
            }
        }

        // The #5 feature for a droid with a β slot; everything else is left alone.
        private static Slot BetaSlotFor(object feature, out Human human)
        {
            human = null;
            if ((string)_buttonName.GetValue(feature) != BatteryButton)
            {
                return null;
            }
            human = InventoryManager.ParentHuman;
            return human != null && human.SpeciesClass == SpeciesClass.Robot ? ExtraBatterySlot.Get(human) : null;
        }

        private static void BuildRootPostfix(object __instance, object __result)
        {
            if (_buildGuard.Disabled)
            {
                return;
            }
            try
            {
                var entries = __result as IList;
                if (entries == null)
                {
                    return;
                }
                Slot beta = BetaSlotFor(__instance, out Human human);
                if (beta == null)
                {
                    return;
                }
                string name = BatteryLabels.BatteryName();
                if (human.UniformSlot != null && human.UniformSlot.Get() == null)
                {
                    // Vanilla built nothing for an empty #5: offer it as a slot too.
                    Add(entries, human.UniformSlot, name + " α");
                }
                Add(entries, beta, name + " β");
            }
            catch (Exception e)
            {
                _buildGuard.Fail(e);
            }
        }

        private static void Add(IList entries, Slot slot, string sublabel)
        {
            object entry = _buildSlotEntry.Invoke(null, new object[] { slot });
            if (entry == null)
            {
                return;
            }
            // Both slots are named "Battery"; the letter tells them apart.
            _sublabel?.SetValue(entry, sublabel);
            entries.Add(entry);
        }

        private static void CanOpenPostfix(object __instance, ref bool __result)
        {
            if (__result || _openGuard.Disabled)
            {
                return;
            }
            try
            {
                Slot beta = BetaSlotFor(__instance, out _);
                if (beta != null && beta.Get() != null)
                {
                    __result = true;
                }
            }
            catch (Exception e)
            {
                _openGuard.Fail(e);
            }
        }
    }
}
