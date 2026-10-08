using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Deep Standby: the droid's BUILT-IN night vision goes off on Start and its key does nothing
    // until the droid wakes (user, 2026-10-07). Night Vision Goggles (a head-mounted tool) are
    // left alone (user clarification). Both drive the same camera effect, so ForceOff only acts
    // when no powered, switched-on goggles are worn -- otherwise the effect is the goggles'.
    // Local player only; camera effect, main thread.
    [HarmonyPatch(typeof(Human), nameof(Human.ToggleNightVision))]
    public static class NightVisionPatch
    {
        public static bool Prefix(Human __instance)
        {
            if (!ReferenceEquals(__instance, InventoryManager.ParentHuman)) return true;
            return !LevelProfile.BlocksNightVision(StandbyRegistry.Get(__instance));
        }

        public static void ForceOff()
        {
            Human me = InventoryManager.ParentHuman;
            if (me == null) return;
            NightVisionGoggles goggles = me.GlassesAsNightVision;
            bool gogglesOn = goggles != null && goggles.IsOperable && goggles.OnOff && goggles.Powered;
            if (Human.CurrentlyUsingNightVision && !gogglesOn)
            {
                CameraController.SetNightVision(false, 1f, 0.5f, robotMode: true);
            }
        }
    }
}
