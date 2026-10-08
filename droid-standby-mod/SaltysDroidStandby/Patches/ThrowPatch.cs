using System;
using System.Reflection;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // User request 2026-10-08: throw power (hold Q) is capped like movement -- Power Save 12.5 %,
    // Standby 0 (Q then just drops the item, so a dead battery can still be put down), Deep
    // already blocks the key. Vanilla charges ThrowItemBehaviour._throwForce up to _maxThrowForce
    // (6) in DropKeyHeld and throws with it in Throw(); clamping after each charge step keeps the
    // on-screen meter honest, and clamping before Throw() covers any other path.
    // Members are resolved in Prepare() (lesson from JetpackPatch): if they're gone, no patch.
    [HarmonyPatch]
    public static class ThrowPatch
    {
        private static AccessTools.FieldRef<ThrowItemBehaviour, float> _force;
        private static AccessTools.FieldRef<ThrowItemBehaviour, float> _max;
        private static AccessTools.FieldRef<ThrowItemBehaviour, Human> _human;
        private static bool _errorLogged;

        public static bool Prepare()
        {
            try
            {
                FieldInfo force = AccessTools.Field(typeof(ThrowItemBehaviour), "_throwForce");
                FieldInfo max = AccessTools.Field(typeof(ThrowItemBehaviour), "_maxThrowForce");
                FieldInfo human = AccessTools.Field(typeof(ThrowItemBehaviour), "_human");
                if (force == null || max == null || human == null
                    || AccessTools.Method(typeof(ThrowItemBehaviour), "DropKeyHeld") == null
                    || AccessTools.Method(typeof(ThrowItemBehaviour), "Throw") == null)
                {
                    SaltysDroidStandby.Log("Throw cap skipped: ThrowItemBehaviour members changed");
                    return false;
                }
                _force = AccessTools.FieldRefAccess<ThrowItemBehaviour, float>(force);
                _max = AccessTools.FieldRefAccess<ThrowItemBehaviour, float>(max);
                _human = AccessTools.FieldRefAccess<ThrowItemBehaviour, Human>(human);
                return true;
            }
            catch (Exception e)
            {
                SaltysDroidStandby.LogError("Throw cap skipped: " + e.Message);
                return false;
            }
        }

        [HarmonyTargetMethods]
        public static System.Collections.Generic.IEnumerable<MethodBase> Targets()
        {
            yield return AccessTools.Method(typeof(ThrowItemBehaviour), "DropKeyHeld");
            yield return AccessTools.Method(typeof(ThrowItemBehaviour), "Throw");
        }

        // Both targets get the clamp before and after: before Throw() is the one that matters for
        // the actual throw; after DropKeyHeld keeps the stored charge at the cap between frames.
        public static void Prefix(ThrowItemBehaviour __instance) => Clamp(__instance);
        public static void Postfix(ThrowItemBehaviour __instance) => Clamp(__instance);

        private static void Clamp(ThrowItemBehaviour b)
        {
            try
            {
                Human local = InventoryManager.ParentHuman;
                Human owner = _human(b);
                if (local == null || !ReferenceEquals(owner, local)) return;
                float factor = LevelProfile.Throw(StandbyRegistry.Get(local));
                if (factor >= 1f) return;
                float cap = _max(b) * factor;
                if (_force(b) > cap) _force(b) = cap;
            }
            catch (Exception e)
            {
                if (!_errorLogged) { _errorLogged = true; SaltysDroidStandby.LogError("ThrowPatch: " + e); }
            }
        }
    }
}
