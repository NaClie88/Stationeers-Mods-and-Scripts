using System;
using System.Runtime.CompilerServices;
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
    //
    // FIXED 2026-10-06 (bug hunt): DropIngots subtracts the ORIGINAL unit count from the pool
    // (`ReagentMixture.Subtract(_ratioMix * num)`) after this prefix has multiplied and
    // re-clamped the output. Whenever `num * multiplier` exceeded the max stack, the furnace
    // still consumed all `num` units for a clamped output, silently losing the multiplier
    // (200 units at 5x gave 500 ingots, effectively 2.5x). The units the clamped output didn't
    // need are now recorded here and handed back to the pool after DropIngots' subtraction (see
    // ArcFurnaceIngotPoolPatch), so they smelt on the next cycle at the full multiplier.
    [HarmonyPatch(typeof(ArcFurnace), nameof(ArcFurnace.CreateOutput))]
    public static class ArcFurnaceYieldPatch
    {
        private static readonly ConditionalWeakTable<ArcFurnace, StrongBox<int>> PendingRefund =
            new ConditionalWeakTable<ArcFurnace, StrongBox<int>>();

        public static void Prefix(ArcFurnace __instance, IQuantity orePrefab, ref int quantity)
        {
            // Blended so reagent that came from melted ingots isn't multiplied -- see
            // IngotReagentTracker. This runs before DropIngots subtracts from the pool, so the
            // ingot share it reads is still the one that produced `quantity`.
            float multiplier = __instance.ReagentMixture != null
                ? IngotReagentTracker.BlendedMultiplier(__instance.ReagentMixture, SaltysYieldMultiplier.IngotMultiplier)
                : SaltysYieldMultiplier.IngotMultiplier;
            int units = quantity;
            quantity = (int)Mathf.Clamp(units * multiplier, 0f, orePrefab.GetMaxQuantity);

            // Units actually needed for the (possibly clamped) output; the rest goes back.
            int needed = multiplier > 0f
                ? Math.Min(units, (int)Math.Ceiling(quantity / (double)multiplier - 1e-4))
                : units;
            PendingRefund.GetOrCreateValue(__instance).Value = Math.Max(0, units - needed);

            SaltysYieldMultiplier.LogVerbose(
                $"ArcFurnaceYieldPatch fired: units={units}, quantity={quantity}, x{multiplier}, refund={units - needed}");
        }

        // Consumed by ArcFurnaceIngotPoolPatch's DropIngots postfix, after vanilla's Subtract.
        internal static int TakeRefund(ArcFurnace furnace)
        {
            if (!PendingRefund.TryGetValue(furnace, out var box)) return 0;
            int refund = box.Value;
            box.Value = 0;
            return refund;
        }
    }
}
