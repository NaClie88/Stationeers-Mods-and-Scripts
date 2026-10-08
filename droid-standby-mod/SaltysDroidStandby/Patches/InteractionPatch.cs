using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace SaltysDroidStandby.Patches
{
    // Spec Revision 2 input limits, local player only.
    //  - Standby and Deep: no world interaction. These InventoryManager mode methods handle
    //    the cursor thing, use, mining and placement. Vanilla skips them entirely while the
    //    cursor is visible (Ctrl/Alt mouse mode), so blocking them never touches inventory.
    //  - Deep only: no inventory either -- slot hotkeys/scroll (CheckDisplaySlotInput) and the
    //    slot buttons' mouse handlers.
    internal static class StandbyInput
    {
        public static StandbyLevel LocalLevel()
        {
            Human local = InventoryManager.ParentHuman;
            return local == null ? StandbyLevel.Normal : StandbyRegistry.Get(local);
        }
    }

    [HarmonyPatch]
    public static class WorldInteractionPatch
    {
        [HarmonyTargetMethods]
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> Targets()
        {
            yield return AccessTools.Method(typeof(InventoryManager), "NormalMode");
            yield return AccessTools.Method(typeof(InventoryManager), "PlacementMode");
            yield return AccessTools.Method(typeof(InventoryManager), "PrecisionPlacementMode");
        }

        public static bool Prefix(InventoryManager __instance)
        {
            if (!LevelProfile.BlocksWorldInteraction(StandbyInput.LocalLevel())) return true;
            __instance.ClearCursor();
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryManager), "CheckDisplaySlotInput")]
    public static class InventoryFreezePatch
    {
        public static bool Prefix() => !LevelProfile.BlocksInventory(StandbyInput.LocalLevel());
    }

    [HarmonyPatch]
    public static class SlotButtonFreezePatch
    {
        [HarmonyTargetMethods]
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> Targets()
        {
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnBeginDrag), new[] { typeof(PointerEventData) });
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnPointerClick), new[] { typeof(PointerEventData) });
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnPointerDown), new[] { typeof(PointerEventData) });
        }

        public static bool Prefix() => !LevelProfile.BlocksInventory(StandbyInput.LocalLevel());
    }
}
