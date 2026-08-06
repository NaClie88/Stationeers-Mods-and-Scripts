using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Structures;
using HarmonyLib;

namespace AirlockCardMod.Patches
{
    // Milestone 2: fires FailsafeController.OnDoorOpened for whichever
    // door just opened, regardless of trigger (native door button,
    // Console UI, or this mod's own future ForceEvacuate/
    // HoldBothDoorsOpen calls).
    //
    // CORRECTED (2026-08-05, in-game): the original version of this
    // patch targeted Thing.IsOpen's property setter, following
    // PATCH_PLAN.md's static-decompilation read of that setter as the
    // single shared attachment point. In-game testing showed it never
    // fires at all -- not even a broad diagnostic logging any door
    // open, with no exception either. Tracing OnServer.Interact (the
    // method every door-open path in this codebase actually calls,
    // confirmed both for AdvancedAirlockControl's own automated
    // cycling and, by the same shared dispatch, native/Console player
    // interaction) shows it resolves the matching Interactable and
    // calls Interactable.Interact(state, ...), which sets
    // Interactable.State -- and that setter calls
    // Thing.OnInteractableStateChanged(interactable, newState,
    // oldState), which is what actually drives the door's Animator
    // (SetIntegerSafe(interactable.PropertyId, newState)).
    // Thing.IsOpen's setter is a separate, effectively-unused code
    // path in practice -- nothing in the real interaction flow calls
    // it. OnInteractableStateChanged is the real single attachment
    // point, confirmed neither Door nor Structure overrides it (so
    // patching Thing directly is safe here, unlike OnThreadUpdate's
    // AirlockControlBase situation).
    [HarmonyPatch(typeof(Thing), nameof(Thing.OnInteractableStateChanged))]
    internal static class DoorOpenPatch
    {
        private static bool loggedFirstMatch;

        // TEMP diagnostic (2026-08-05), same purpose as the one that
        // caught the previous attachment point being wrong: confirm
        // this new one actually fires before trusting it. Remove once
        // confirmed.
        private static bool loggedAnyDoorOpen;

        private static void Postfix(Thing __instance, Interactable interactable, int newState, int oldState)
        {
            if (interactable.Action != InteractableType.Open) return;
            if (newState != 1 || oldState == 1) return; // edge-detect closed -> open only
            if (!(__instance is Door door)) return;

            if (!loggedAnyDoorOpen)
            {
                loggedAnyDoorOpen = true;
                UnityEngine.Debug.Log("[Salty's Advanced Airlock]: DIAGNOSTIC -- OnInteractableStateChanged Open fired for a Door ("
                    + door.DisplayName + "), KnownControllers.Count=" + AdvancedAirlockFailsafePatch.KnownControllers.Count);
            }

            foreach (var controller in AdvancedAirlockFailsafePatch.KnownControllers)
            {
                if (!(bool)controller) continue; // Unity-destroyed check

                DoorSide? side = null;
                if (door == controller.ExteriorAirlock) side = DoorSide.Exterior;
                else if (door == controller.InteriorAirlock) side = DoorSide.Interior;

                if (side.HasValue)
                {
                    AdvancedAirlockFailsafePatch.GetOrCreateController(controller).OnDoorOpened(side.Value);

                    if (!loggedFirstMatch)
                    {
                        loggedFirstMatch = true;
                        UnityEngine.Debug.Log("[Salty's Advanced Airlock]: OnDoorOpened fired, side=" + side.Value);
                    }
                }
            }
        }
    }
}
