using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using HarmonyLib;
using SaltysDroidStandby.Game;
using UnityEngine.EventSystems;

namespace SaltysDroidStandby.Patches
{
    // Spec Revision 2 input limits, local player only.
    //  - Standby and Deep: no world interaction. Two vanilla paths reach the world:
    //    * normal play: the InventoryManager mode methods (cursor thing, use, mining, placement);
    //    * Ctrl/Alt mouse mode: InputMouse.Idle/Click (switches, door buttons, picking items up).
    //    Slot moves in mouse mode go through SlotDisplayButton, not these, so inventory survives.
    //  - Deep only: no inventory either -- slot hotkeys/scroll (CheckDisplaySlotInput), the slot
    //    buttons' click and drag handlers, and the inventory key bindings in KeyManager.
    //  - While the Deep Standby menu is open, mouse-mode world clicks are blocked too, so a click
    //    on Start/Cancel can't reach a switch behind the ImGui window (final review I3).
    internal static class StandbyInput
    {
        public static StandbyLevel LocalLevel()
        {
            Human local = InventoryManager.ParentHuman;
            return local == null ? StandbyLevel.Normal : StandbyRegistry.Get(local);
        }

        public static bool InventoryBlocked() => LevelProfile.BlocksInventory(LocalLevel());
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

    // Final review I2: Ctrl/Alt mouse mode's own world path.
    [HarmonyPatch]
    public static class MouseWorldPatch
    {
        [HarmonyTargetMethods]
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> Targets()
        {
            yield return AccessTools.Method(typeof(InputMouse), "Idle");
            yield return AccessTools.Method(typeof(InputMouse), "Click");
        }

        public static bool Prefix(InputMouse __instance)
        {
            if (!LevelProfile.BlocksMouseWorld(StandbyInput.LocalLevel(), LocalController.MenuOpen)) return true;
            __instance.WorldMode = WorldMouseMode.Idle; // drop a pending click so it can't fire on wake
            CursorManager.SetSelectionVisibility(isVisible: false);
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryManager), "CheckDisplaySlotInput")]
    public static class InventoryFreezePatch
    {
        public static bool Prefix() => !StandbyInput.InventoryBlocked();
    }

    // Final review I1: the real slot-button paths are OnPointerUp (move to hand, smart stow) and
    // the drag pair (OnEndDrag also drops into world slots). OnPointerDown is empty in vanilla.
    [HarmonyPatch]
    public static class SlotButtonFreezePatch
    {
        [HarmonyTargetMethods]
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> Targets()
        {
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnPointerUp), new[] { typeof(PointerEventData) });
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnBeginDrag), new[] { typeof(PointerEventData) });
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnEndDrag), new[] { typeof(PointerEventData) });
        }

        public static bool Prefix() => !StandbyInput.InventoryBlocked();
    }

    // Final review I1: inventory key bindings that KeyManager dispatches on its own
    // (swap hands, hand-tool power, smart stow, inventory select, drop/throw). Deep only;
    // in Standby they are inventory management and stay available.
    [HarmonyPatch]
    public static class InventoryKeyFreezePatch
    {
        [HarmonyTargetMethods]
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> Targets()
        {
            foreach (string name in new[] { "SwapHandsOnKeyUp", "ToggleActiveHandTool", "SmartStow", "InventorySelect", "DropKeyUp", "DropKeyDown", "DropKeyHeld" })
            {
                System.Reflection.MethodInfo m = AccessTools.Method(typeof(KeyManager), name);
                if (m != null) yield return m;
            }
        }

        public static bool Prefix() => !StandbyInput.InventoryBlocked();
    }
}
