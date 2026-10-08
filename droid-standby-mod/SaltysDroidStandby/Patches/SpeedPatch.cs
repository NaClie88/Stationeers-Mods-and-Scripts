using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Vanilla stun does NOT cap top speed: MovementController.MovementHandler scales only the
    // per-step headroom (maxSpeed - currentSpeed) by (1 - 0.9 x stun), so a stunned droid still
    // reaches full speed, just accelerating more slowly (found in the first in-game test: vision
    // dimmed correctly but walking stayed at full speed). This caps the top speed itself by
    // scaling the per-player characterMaxSpeed field for the duration of the call -- the same
    // scale-and-restore shape as JumpPatch. Local player only (movement is client-driven).
    // Revision 2: Power Save 12.5 %, Standby and Deep Standby 0 % (no walking).
    [HarmonyPatch(typeof(MovementController), "MovementHandler")]
    public static class SpeedPatch
    {
        public static void Prefix(MovementController __instance, out float __state)
        {
            __state = -1f;
            Human local = InventoryManager.ParentHuman;
            if (local == null || __instance.parentEntity != local) return;
            float factor = LevelProfile.Movement(StandbyRegistry.Get(local));
            if (factor >= 1f) return;
            __state = __instance.characterMaxSpeed;
            __instance.characterMaxSpeed *= factor;
        }

        public static void Finalizer(MovementController __instance, float __state)
        {
            if (__state >= 0f) __instance.characterMaxSpeed = __state;
        }
    }
}
