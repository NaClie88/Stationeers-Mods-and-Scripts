using System;
using System.Reflection;
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
    //
    // In-game bug (2026-10-08): `Stabilizer` is a private PROPERTY, not a field. Resolving it as a
    // field threw from the static initializer on the first jetpack frame, so every jetpack frame
    // threw -- in every state, Normal included. Members are now resolved in Prepare(): if any is
    // missing, Harmony skips the patch (one log line) and vanilla's jetpack runs untouched.
    [HarmonyPatch(typeof(MovementController), "HandleJetpack")]
    public static class JetpackPatch
    {
        private static Func<MovementController, bool> _stabilizerOn;
        private static AccessTools.FieldRef<MovementController, bool> _jetpackUsed;
        private static Action<MovementController> _stabilize;
        private static bool _errorLogged;

        public static bool Prepare()
        {
            try
            {
                MethodInfo getter = AccessTools.PropertyGetter(typeof(MovementController), "Stabilizer");
                FieldInfo used = AccessTools.Field(typeof(MovementController), "_jetpackUsed");
                MethodInfo stabilize = AccessTools.Method(typeof(MovementController), "StabilizeJetpack");
                if (getter == null || used == null || stabilize == null)
                {
                    SaltysDroidStandby.Log("Jetpack block skipped: MovementController members changed (Stabilizer/_jetpackUsed/StabilizeJetpack)");
                    return false;
                }
                _stabilizerOn = AccessTools.MethodDelegate<Func<MovementController, bool>>(getter);
                _jetpackUsed = AccessTools.FieldRefAccess<MovementController, bool>(used);
                _stabilize = AccessTools.MethodDelegate<Action<MovementController>>(stabilize);
                return true;
            }
            catch (Exception e)
            {
                SaltysDroidStandby.LogError("Jetpack block skipped: " + e.Message);
                return false;
            }
        }

        public static bool Prefix(MovementController __instance, bool haveGravity)
        {
            try
            {
                Human local = InventoryManager.ParentHuman;
                if (local == null || __instance.parentEntity != local) return true;
                if (!LevelProfile.BlocksJetpack(StandbyRegistry.Get(local))) return true;

                _jetpackUsed(__instance) = false;
                if (!local.TryGetJetpack(out Jetpack jetpack) || (object)jetpack == null) return false;
                if (!jetpack.HasPropellent)
                {
                    jetpack.ClearEmissions(); // vanilla's empty-tank path: no free hover
                    return false;
                }
                int emission = 0;
                if (_stabilizerOn(__instance))
                {
                    _stabilize(__instance);
                    if (haveGravity) emission = 1; // vanilla: stabilizing against gravity costs propellant
                }
                jetpack.CurrentEmission = emission;
                return false;
            }
            catch (Exception e)
            {
                // Never break the jetpack: fall back to vanilla for this frame (logged once).
                if (!_errorLogged) { _errorLogged = true; SaltysDroidStandby.LogError("JetpackPatch: " + e); }
                return true;
            }
        }
    }
}
