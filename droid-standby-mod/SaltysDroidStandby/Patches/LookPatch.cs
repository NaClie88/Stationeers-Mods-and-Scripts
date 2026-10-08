using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Sluggish mouse look in proportion to the standby level (user request after the first
    // test). CameraController.SetMouseLook adds axis x the static CameraSensitivity; scaling it
    // for the duration of the call and restoring it in a Finalizer leaves the player's own
    // sensitivity setting untouched. Main thread only (camera update), so the static is safe.
    // Revision 2: Power Save 12.5 %, Standby 6.25 %, Deep Standby 0 (frozen).
    [HarmonyPatch(typeof(CameraController), "SetMouseLook")]
    public static class LookPatch
    {
        public static void Prefix(out float __state)
        {
            __state = -1f;
            Human local = InventoryManager.ParentHuman;
            if (local == null || !local.IsArtificial) return;
            float factor = LevelProfile.Look(StandbyRegistry.Get(local));
            if (factor >= 1f) return;
            __state = CameraController.CameraSensitivity;
            CameraController.CameraSensitivity *= factor;
        }

        public static void Finalizer(float __state)
        {
            if (__state >= 0f) CameraController.CameraSensitivity = __state;
        }
    }
}
