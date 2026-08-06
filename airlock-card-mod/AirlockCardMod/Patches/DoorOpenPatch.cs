using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Structures;
using HarmonyLib;

namespace AirlockCardMod.Patches
{
    // Milestone 2: fires FailsafeController.OnDoorOpened for whichever
    // door just opened, regardless of trigger (native door button,
    // Console UI, or this mod's own future ForceEvacuate/
    // HoldBothDoorsOpen calls). See PATCH_PLAN.md's "Where OnDoorOpened
    // attaches" -- Thing.IsOpen's property setter is the single
    // confirmed point every one of those paths funnels through.
    [HarmonyPatch(typeof(Thing), nameof(Thing.IsOpen), MethodType.Setter)]
    internal static class DoorOpenPatch
    {
        // Not yet exercised in-game as of 2026-08-05 (unlike
        // AdvancedAirlockFailsafePatch, confirmed working via its own
        // "Failsafe layer attached" log line) -- this patches
        // Thing.IsOpen's setter globally, every openable Thing in the
        // entire game, not just airlock doors, so it's worth
        // confirming separately that it applies cleanly and doesn't
        // disturb ordinary door behavior before building anything real
        // on top of it. Logged once so that's visible without needing
        // a debugger attached.
        private static bool loggedFirstMatch;

        // TEMP diagnostic (2026-08-05): the first in-game door-open
        // test produced no "OnDoorOpened fired" line and no exception
        // either -- ambiguous between "the Postfix never runs on a
        // door open" (deeper problem, Thing.IsOpen's setter isn't
        // actually the real path) and "it runs but this door isn't
        // recognized as belonging to any tracked airlock" (smaller
        // problem). Logs once for ANY Door whose IsOpen transitions to
        // true, regardless of controller match, to tell those two
        // apart. Remove once resolved.
        private static bool loggedAnyDoorOpen;

        // Captures the value before this specific assignment, so the
        // Postfix can edge-detect false -> true instead of firing on
        // every redundant same-value write (the setter runs on every
        // assignment -- see PATCH_PLAN.md's "one real wrinkle").
        private static void Prefix(Thing __instance, out bool __state)
        {
            __state = __instance.IsOpen;
        }

        private static void Postfix(Thing __instance, bool value, bool __state)
        {
            if (!value || __state) return;
            if (!(__instance is Door door)) return;

            if (!loggedAnyDoorOpen)
            {
                loggedAnyDoorOpen = true;
                UnityEngine.Debug.Log("[Salty's Advanced Airlock]: DIAGNOSTIC -- Thing.IsOpen setter fired for a Door ("
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
