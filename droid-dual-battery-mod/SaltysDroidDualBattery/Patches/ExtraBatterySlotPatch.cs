using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;
using HarmonyLib;
using UnityEngine;

namespace SaltysDroidDualBattery.Patches
{
    // The droid's only battery slot is vanilla's Uniform slot, retyped to Battery by
    // Human.SetSpeciesSpecificSlots. We hook that same moment and, for droids only, append a
    // second Battery slot to the end of the Human's Slots list. Humans and Zrilians are untouched.
    //
    // Why this is safe even though the slot appears after Awake: species is only known once
    // cosmetics are read, and both load paths read them synchronously right after creating the
    // Human, before any of its saved items can look for their slot:
    //   - save load:   XmlSaveLoad.Load -> Thing.Create -> Human.DeserializeSave -> UpdateCosmeticIdentity
    //                  (an item loaded earlier waits in MoveToParentWhenReady and re-checks next frame)
    //   - client join: NetworkClient.ProcessThings -> Create -> Human.DeserializeOnJoin -> UpdateCosmeticIdentity
    //                  (one thing at a time; a child's parent must already exist)
    // Items address their slot by index (ParentSlotId), so the slot must always land at the end,
    // after vanilla's and any other mod's Awake-time slots.
    public static class ExtraBatterySlot
    {
        public const string SlotKey = "Battery";

        private static readonly ConditionalWeakTable<Human, Slot> _slots = new ConditionalWeakTable<Human, Slot>();

        public static Slot Get(Human human)
        {
            return human != null && _slots.TryGetValue(human, out var slot) ? slot : null;
        }

        // Idempotent: SetSpeciesSpecificSlots runs again whenever cosmetics are re-applied.
        public static Slot EnsureAdded(Human human)
        {
            Slot existing = Get(human);
            if (existing != null)
            {
                return existing;
            }

            Slot uniform = human.UniformSlot;
            if (uniform == null)
            {
                SaltysDroidDualBattery.Log("Droid " + human.name + " has no battery (#5) slot yet -- second slot NOT added");
                return null;
            }
            var slot = new Slot
            {
                StringKey = SlotKey,
                StringHash = Animator.StringToHash(SlotKey),
                Type = Slot.Class.Battery,
                Parent = human,
                Location = uniform.Location,
                EntityControlMode = uniform.EntityControlMode,
                UseInternalAtmosphere = uniform.UseInternalAtmosphere,
                RealWorldScale = uniform.RealWorldScale,
                ScaleMultiplier = uniform.ScaleMultiplier,
                OccupantCastsShadows = uniform.OccupantCastsShadows,
                // Shares the battery slot's body anchor; hide it so two batteries don't overlap.
                HidesOccupant = true,
                IsHiddenInSeat = uniform.IsHiddenInSeat,
                IsInteractable = uniform.IsInteractable,
                IsSwappable = uniform.IsSwappable,
                AllowDragging = uniform.AllowDragging,
                Size = uniform.Size,
            };
            human.Slots.Add(slot);

            // What Thing.Awake (ConfigureSlots + Initialize) does for slots that exist at Awake.
            slot.Action = (InteractableType)Enum.Parse(typeof(InteractableType), "Slot" + (slot.SlotIndex + 1));
            slot.Initialize();

            _slots.Add(human, slot);
            SaltysDroidDualBattery.LogVerbose("Added second battery slot to droid " + human.name +
                                              " at index " + slot.SlotIndex);
            return slot;
        }
    }

    [HarmonyPatch(typeof(Human), "SetSpeciesSpecificSlots")]
    public static class ExtraBatterySlotPatch
    {
        public static void Postfix(Human __instance)
        {
            if (__instance.SpeciesClass != SpeciesClass.Robot)
            {
                return;
            }
            ExtraBatterySlot.EnsureAdded(__instance);

            // A brand-new droid can learn its species after the HUD was already built for it.
            HudSlotPatch.Refresh(__instance);
        }
    }
}
