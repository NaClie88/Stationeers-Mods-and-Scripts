using System;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec §4.1. Entity.DamageState.Stun *is* the brain's stun (EntityDamageState.Stun returns
    // OrganBrain.DamageState.Stun), so the brain is the one place to hold the floor. A powered
    // droid's brain recovers 3 stun per Brain.OnLifeTick; this postfix re-raises it to the
    // floor right after. It only ever RAISES stun, so real stun (damage, empty battery) still
    // shows on top. 85 stays under vanilla's unconscious thresholds (90 in a life suspender,
    // 100 anywhere -- Human.OnLifeTick).
    //
    // THREADING (final-review C1): Brain.OnLifeTick runs inside AtmosphericsManager.LifeTicksTick
    // on a thread-pool thread. Nothing here may touch a UnityEngine.Object API (name, transform,
    // ...); an exception escaping would make GameTick skip electricity/logic/atmosphere ticks.
    // Hence ReferenceEquals instead of Unity's == and a catch-all around the body.
    [HarmonyPatch(typeof(Brain), nameof(Brain.OnLifeTick))]
    public static class CognitionFloorPatch
    {
        public static void Postfix(Brain __instance)
        {
            try
            {
                if (!GameManager.RunSimulation) return;
                Human human = __instance.ParentHuman;
                if (ReferenceEquals(human, null)) return;

                StandbyLevel level = StandbyRegistry.Get(human);
                if (level == StandbyLevel.Normal) return;

                if (IsBlocked(human))
                {
                    StandbyRegistry.Set(human, StandbyLevel.Normal);
                    return;
                }

                float floor = StandbyConfig.StunFloor(level);
                if (__instance.DamageState.Stun < floor)
                {
                    __instance.DamageState.Damage(ChangeDamageType.Set, floor, DamageUpdateType.Stun);
                }
            }
            catch (Exception e)
            {
                SaltysDroidStandby.LogError("CognitionFloorPatch: " + e.Message);
            }
        }

        // Shared with the client mirror reset and the safety-net gate (StandbyRules).
        public static bool IsBlocked(Human human)
        {
            if (ReferenceEquals(human, null)) return true;
            Brain brain = human.OrganBrain;
            return StandbyRules.ShouldClear(
                human.IsArtificial,
                human.State == EntityState.Alive,
                human.IsSleeping,
                human.RootParent is ILifeSuspender suspender && suspender.IsSuspendingLife,
                NetworkManager.IsActive,
                !ReferenceEquals(brain, null) && brain.IsOnline);
        }
    }
}
