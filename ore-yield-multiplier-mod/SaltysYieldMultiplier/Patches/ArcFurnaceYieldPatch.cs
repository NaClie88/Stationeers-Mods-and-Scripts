using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using UnityEngine;

namespace SaltysYieldMultiplier.Patches
{
    // ArcFurnace.DropIngots() (private, no scale hook -- unlike Furnace/AdvancedFurnace,
    // see IngotYieldPatch) already clamps the ingot count to the output prefab's max stack
    // quantity *before* calling this method, so scaling here must re-clamp against the same
    // orePrefab.GetMaxQuantity bound rather than letting the multiplied value exceed it.
    // CreateOutput is public and used nowhere else in ArcFurnace (confirmed via decompile),
    // so this is a narrow, single-purpose hook -- not a Transpiler reaching into a private
    // method's internals, which is why this was worth doing where the earlier "not covered"
    // decision in UpdateNotes.md held back.
    [HarmonyPatch(typeof(ArcFurnace), nameof(ArcFurnace.CreateOutput))]
    public static class ArcFurnaceYieldPatch
    {
        public static void Prefix(ArcFurnace __instance, IQuantity orePrefab, ref int quantity)
        {
            // Blended so reagent that came from melted ingots isn't multiplied -- see
            // IngotReagentTracker. This runs before DropIngots subtracts from the pool, so the
            // ingot share it reads is still the one that produced `quantity`.
            float multiplier = __instance.ReagentMixture != null
                ? IngotReagentTracker.BlendedMultiplier(__instance.ReagentMixture, SaltysYieldMultiplier.IngotMultiplier)
                : SaltysYieldMultiplier.IngotMultiplier;
            quantity = (int)Mathf.Clamp(quantity * multiplier, 0f, orePrefab.GetMaxQuantity);
            SaltysYieldMultiplier.LogVerbose($"ArcFurnaceYieldPatch fired: quantity={quantity}, x{multiplier}");
        }
    }
}
