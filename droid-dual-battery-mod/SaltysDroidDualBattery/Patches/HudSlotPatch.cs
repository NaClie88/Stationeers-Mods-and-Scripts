using System.Linq;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using CharacterCustomisation;
using HarmonyLib;
using UnityEngine;

namespace SaltysDroidDualBattery.Patches
{
    // The HUD's equipment buttons are InventoryManager.DisplaySlots: SlotDisplay objects that each
    // bind to parent.Slots[SlotId]. We clone the droid's #5 battery button and link the clone to
    // the second battery slot ourselves.
    //
    // The clone is deliberately NOT added to DisplaySlots: vanilla's per-frame key loop reads
    // displaySlot.Slot.Occupant on every entry, so an entry left unlinked (a human character after
    // a droid one) would throw every frame. Clicks and drags go through the button's own
    // SlotDisplay, and Slot.RefreshSlotDisplay goes through Slot.Display, so neither needs the list.
    // InventoryManager has both Initialize() and Initialize(Entity); name the overload explicitly
    // or Harmony throws AmbiguousMatchException (caught in the first v0.2 test).
    [HarmonyPatch(typeof(InventoryManager), nameof(InventoryManager.Initialize), new[] { typeof(Entity) })]
    public static class HudSlotPatch
    {
        private static SlotDisplay _extraDisplay;
        private static bool _layoutLogged;

        public static void Postfix(Entity parent)
        {
            Refresh(parent as Human);
        }

        // Called after the HUD is (re)built for the local player, and when a droid gains its
        // second slot (a new character can learn its species after the HUD was built).
        public static void Refresh(Human human)
        {
            InventoryManager manager = InventoryManager.Instance;
            if (human == null || manager == null || InventoryManager.Parent != human)
            {
                return;
            }

            Slot extraSlot = ExtraBatterySlot.Get(human);
            if (extraSlot == null)
            {
                if (IsAlive(_extraDisplay))
                {
                    _extraDisplay.SlotDisplayButton.SetVisible(isVisble: false);
                }
                // Humans/Zrilians share the #5 button: hand its label back to vanilla.
                BatteryLabels.SetDroidMode(false, _sourceDisplay, _extraDisplay);
                return;
            }

            if (!IsAlive(_extraDisplay) && !TryCreateButton(manager, human))
            {
                return;
            }

            _extraDisplay.SlotId = extraSlot.SlotIndex;
            _extraDisplay.LinkToSlot(human);
            _extraDisplay.SlotDisplayButton.SetVisible(isVisble: true);
            extraSlot.RefreshSlotDisplay();
            BatteryLabels.SetDroidMode(true, _sourceDisplay, _extraDisplay);
        }

        // The vanilla #5 button the clone was made from (its label becomes "<Battery> α").
        private static SlotDisplay _sourceDisplay;

        // The button is destroyed with the HUD on a scene reload.
        private static bool IsAlive(SlotDisplay display)
        {
            return display != null && display.SlotDisplayButton != null;
        }

        private static bool TryCreateButton(InventoryManager manager, Human human)
        {
            int uniformIndex = human.UniformSlot.SlotIndex;
            SlotDisplay source = manager.DisplaySlots.FirstOrDefault(d =>
                d.SlotId == uniformIndex && d.SlotDisplayButton != null &&
                d.SlotDisplayButton.IsAvailableForSpecies(SpeciesClass.Robot));
            if (source == null)
            {
                SaltysDroidDualBattery.Log("HUD: no display button bound to the #5 battery slot (index " +
                                           uniformIndex + ") -- second battery button NOT added");
                return false;
            }

            SlotDisplayButton sourceButton = source.SlotDisplayButton;
            GameObject clone = Object.Instantiate(sourceButton.gameObject, sourceButton.transform.parent, false);
            clone.name = sourceButton.gameObject.name + "_Battery2";
            clone.transform.SetSiblingIndex(sourceButton.transform.GetSiblingIndex() + 1);

            // The clone carries the original's "5" key label; slot 2 has no hotkey in v1.
            foreach (HotkeyDisplay hotkey in clone.GetComponentsInChildren<HotkeyDisplay>(true))
            {
                if (hotkey.Assignment == source.SwapButton)
                {
                    hotkey.gameObject.SetActive(false);
                }
            }

            // SwapButton empty: belt and braces in case anything iterates it like a DisplaySlots entry.
            _extraDisplay = new SlotDisplay(clone.GetComponent<SlotDisplayButton>()) { SwapButton = string.Empty };

            // The HUD label is baked into the button prefab and localized by a LocalizedText
            // component, so the clone inherits #5's text; BatteryLabels relabels both.
            _sourceDisplay = source;
            BatteryLabels.Attach(source, _extraDisplay);

            PlaceBesideIfNoLayout(sourceButton, clone);
            SaltysDroidDualBattery.Log("HUD: added second battery button (cloned from '" + sourceButton.gameObject.name +
                                       "', hotkey '" + source.SwapButton + "')");
            return true;
        }

        // If the hotbar row arranges its children with a LayoutGroup, the sibling index above is
        // enough. If buttons are hand-positioned, shift the clone one button-width to the right.
        // The layout is logged once either way: it isn't knowable from the decompile alone.
        private static void PlaceBesideIfNoLayout(SlotDisplayButton source, GameObject clone)
        {
            Transform row = source.transform.parent;
            bool hasLayout = row != null && row.GetComponents<Component>()
                .Any(c => c != null && c.GetType().Name.Contains("LayoutGroup"));

            var sourceRect = source.transform as RectTransform;
            var cloneRect = clone.transform as RectTransform;
            if (!hasLayout && sourceRect != null && cloneRect != null)
            {
                cloneRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(sourceRect.rect.width + 4f, 0f);
            }

            if (_layoutLogged)
            {
                return;
            }
            _layoutLogged = true;
            string rowComponents = row == null ? "(no parent)"
                : string.Join(", ", row.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name));
            SaltysDroidDualBattery.Log("HUD layout: row '" + (row != null ? row.name : "?") + "' components [" +
                                       rowComponents + "], layoutGroup=" + hasLayout +
                                       ", source pos=" + (sourceRect != null ? sourceRect.anchoredPosition.ToString() : "?") +
                                       " size=" + (sourceRect != null ? sourceRect.rect.size.ToString() : "?") +
                                       ", clone pos=" + (cloneRect != null ? cloneRect.anchoredPosition.ToString() : "?"));
        }
    }
}
