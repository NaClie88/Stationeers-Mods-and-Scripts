using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using UnityEngine;

namespace SaltysYieldMultiplier.Patches
{
    // Display only. The furnace's window-panel tooltip lists the raw reagent pool ("Iron 3")
    // and then a "Produce N ingots" line that already includes the multiplier (via
    // GetSmelterScale, see IngotYieldPatch), so the two never matched. This rewrites the
    // reagent list with the same per-unit scale the Produce line uses, so the contents read
    // as what will actually come out. The real pool is never modified.
    //
    // Only applies while an ingot recipe is matched (SmelterResult != null) -- with no recipe
    // the output is de-gassed/dirty ore, which the mod deliberately leaves 1:1, so the raw list
    // stays correct.
    [HarmonyPatch(typeof(FurnaceBase), nameof(FurnaceBase.GetPassiveTooltip))]
    public static class FurnaceTooltipPatch
    {
        public static void Postfix(FurnaceBase __instance, Collider hitCollider, ref PassiveTooltip __result)
        {
            if (hitCollider != __instance.WindowPanel) return;
            var mix = __instance.ReagentMixture;
            if (mix == null || __instance.SmelterResult == null || string.IsNullOrEmpty(__result.Extended)) return;

            float scale = __instance.GetSmelterScale();
            if (scale <= 0f || scale == 1f) return;

            string raw = mix.ToString();
            if (string.IsNullOrEmpty(raw)) return;
            __result.Extended = __result.Extended.Replace(raw, mix.ToString(scale));
        }
    }
}
