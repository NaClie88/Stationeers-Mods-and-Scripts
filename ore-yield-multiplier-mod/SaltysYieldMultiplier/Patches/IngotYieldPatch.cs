using Assets.Scripts.Util;
using HarmonyLib;
using Reagents;

namespace SaltysYieldMultiplier.Patches
{
    // Furnace.GetSmelterScale() and AdvancedFurnace.GetSmelterScale() both delegate to this
    // single instance method to decide how many ingots come out per matched recipe portion.
    // Confirmed via decompile that IQuantityRecipeComparable (and its ingot-recipe dictionary)
    // is used exclusively by Furnace/AdvancedFurnace/ArcFurnace -- Centrifuge uses a completely
    // separate class (RecyclerRecipeComparable, its own dictionary, its own hardcoded loss
    // ratio), so this patch cannot affect Centrifuge splitting.
    //
    // ArcFurnace does not call GetSmelterScale/GetOutputScale at all -- its ingot output
    // (ArcFurnace.DropIngots) is computed inline with no scale hook, so it needs its own
    // patch: see ArcFurnaceYieldPatch.
    [HarmonyPatch(typeof(IQuantityRecipeComparable), nameof(IQuantityRecipeComparable.GetOutputScale))]
    public static class IngotYieldPatch
    {
        public static void Postfix(Recipe recipe, ref float __result)
        {
            // Vanilla recipes never configure an explicit Output, so GetOutputScale normally
            // returns 0 here -- CreateIngots() treats "scale <= 0" as "no scaling" (its own
            // `if (smelterScale > 0f)` guard). Multiplying 0 directly would always yield 0, so
            // an unset scale must be treated as the vanilla-equivalent 1 before multiplying.
            // If some other recipe already has a real non-zero scale configured, that value is
            // respected (multiplied, not clobbered) rather than assumed to be 1.
            float baseScale = __result > 0f ? __result : 1f;
            __result = baseScale * SaltysYieldMultiplier.IngotMultiplier;
            // Deliberately not interpolating `recipe` directly -- Recipe.ToString() dumps a
            // multi-line description (Temperature/Pressure/ingredients each on their own line),
            // which splits a single log call across several lines in LogOutput.log and breaks
            // simple single-line grep patterns. baseScale/__result is what actually matters here.
            SaltysYieldMultiplier.LogVerbose($"IngotYieldPatch fired: baseScale={baseScale}, scale={__result}");
        }
    }
}
