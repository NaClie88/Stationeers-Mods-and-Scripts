using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec Revision 2: no jetpack in Standby or Deep Standby. HandleJetpack applies every
    // thrust force from the movement axes; this prefix replaces it with the no-input path:
    // no thrust, the stabilizer only (so a droid parked mid-air in zero-g is damped to a stop),
    // and the emission level reset. Final review I4: a stale CurrentEmission keeps burning
    // propellant (Jetpack.OnAtmosphericTick, server side via the networked value) and a stale
    // _jetpackUsed stops StabilizeJetpack's damping -- so both are set here every frame.
    [HarmonyPatch(typeof(MovementController), "HandleJetpack")]
    public static class JetpackPatch
    {
        private static readonly AccessTools.FieldRef<MovementController, bool> Stabilizer =
            AccessTools.FieldRefAccess<MovementController, bool>("Stabilizer");
        private static readonly AccessTools.FieldRef<MovementController, bool> JetpackUsed =
            AccessTools.FieldRefAccess<MovementController, bool>("_jetpackUsed");
        private static readonly System.Action<MovementController> Stabilize =
            AccessTools.MethodDelegate<System.Action<MovementController>>(AccessTools.Method(typeof(MovementController), "StabilizeJetpack"));

        public static bool Prefix(MovementController __instance, bool haveGravity)
        {
            Human local = InventoryManager.ParentHuman;
            if (local == null || __instance.parentEntity != local) return true;
            if (!LevelProfile.BlocksJetpack(StandbyRegistry.Get(local))) return true;

            JetpackUsed(__instance) = false;
            if (!local.TryGetJetpack(out Jetpack jetpack) || (object)jetpack == null) return false;
            if (!jetpack.HasPropellent)
            {
                jetpack.ClearEmissions(); // vanilla's empty-tank path: no free hover
                return false;
            }
            int emission = 0;
            if (Stabilizer(__instance))
            {
                Stabilize(__instance);
                if (haveGravity) emission = 1; // vanilla: stabilizing against gravity costs propellant
            }
            jetpack.CurrentEmission = emission;
            return false;
        }
    }
}
