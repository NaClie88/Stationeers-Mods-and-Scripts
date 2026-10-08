using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec §4.3. jumpForce is a per-MovementController instance field, so scaling it for the
    // duration of HandleJump and restoring it in a Finalizer affects only this player's jump.
    // Jetpack thrust is a separate system and is not touched.
    [HarmonyPatch(typeof(MovementController), "HandleJump")]
    public static class JumpPatch
    {
        public static void Prefix(MovementController __instance, out float __state)
        {
            __state = -1f;
            Human local = InventoryManager.ParentHuman;
            if (local == null || __instance.gameObject != local.gameObject) return;
            float factor = StandbyConfig.JumpFactor(StandbyRegistry.Get(local));
            if (factor >= 1f) return;
            __state = __instance.jumpForce;
            __instance.jumpForce *= factor;
        }

        public static void Finalizer(MovementController __instance, float __state)
        {
            if (__state >= 0f) __instance.jumpForce = __state;
        }
    }
}
