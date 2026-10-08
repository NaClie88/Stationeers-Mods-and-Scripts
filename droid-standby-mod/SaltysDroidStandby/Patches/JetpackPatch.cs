using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec Revision 2: no jetpack in Standby or Deep Standby. HandleJetpack applies every
    // thrust force from the movement axes; skipping it removes thrust. The stabilizer still
    // runs, so a droid that parks mid-air in zero-g is held steady instead of tumbling.
    [HarmonyPatch(typeof(MovementController), "HandleJetpack")]
    public static class JetpackPatch
    {
        private static readonly AccessTools.FieldRef<MovementController, bool> Stabilizer =
            AccessTools.FieldRefAccess<MovementController, bool>("Stabilizer");
        private static readonly System.Action<MovementController> Stabilize =
            AccessTools.MethodDelegate<System.Action<MovementController>>(AccessTools.Method(typeof(MovementController), "StabilizeJetpack"));

        public static bool Prefix(MovementController __instance)
        {
            Human local = InventoryManager.ParentHuman;
            if (local == null || __instance.parentEntity != local) return true;
            if (!LevelProfile.BlocksJetpack(StandbyRegistry.Get(local))) return true;
            if (Stabilizer(__instance)) Stabilize(__instance);
            return false;
        }
    }
}
