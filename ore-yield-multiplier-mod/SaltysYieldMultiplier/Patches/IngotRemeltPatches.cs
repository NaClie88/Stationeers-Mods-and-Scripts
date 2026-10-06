using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using Objects.Items;
using Reagents;

namespace SaltysYieldMultiplier.Patches
{
    // Together these make "1 ingot in = 1 ingot out" while ore keeps its multiplier. See
    // IngotReagentTracker for why the furnace's shared reagent pool needs help to tell them
    // apart. Each class is registered on its own (a failure in one can't take down the
    // others) and all of them degrade to the old always-multiply behavior if they fail.

    // Ingot.Smelt is inherited unchanged from Consumable.Smelt (FuelIngot's override calls
    // through to it), so that's the one place every ingot melt passes through, in every
    // furnace type. Measures how much reagent the ingot added to the pool it was given.
    [HarmonyPatch(typeof(Consumable), nameof(Consumable.Smelt))]
    public static class IngotMeltTrackingPatch
    {
        public static void Prefix(Consumable __instance, ReagentMixture reagentMixture, out double __state)
        {
            __state = -1.0;
            if (!(__instance is Ingot) || reagentMixture == null) return;
            __state = reagentMixture.TotalReagents;
            IngotReagentTracker.ResetIfEmpty(reagentMixture, __state);
        }

        public static void Postfix(Consumable __instance, ReagentMixture reagentMixture, double __state)
        {
            if (__state < 0.0) return;
            double added = reagentMixture.TotalReagents - __state;
            IngotReagentTracker.RecordIngotMelt(reagentMixture, added);
            SaltysYieldMultiplier.LogVerbose(
                $"IngotMeltTrackingPatch fired: {__instance.name} added {added} reagent units to the furnace pool");
        }
    }

    // Scales down the ingot-sourced share of the pool as ingots or de-gassed/dirty ore are taken out of it.
    [HarmonyPatch(typeof(FurnaceBase), nameof(FurnaceBase.CreateIngots))]
    public static class FurnaceIngotPoolPatch
    {
        public static void Prefix(FurnaceBase __instance, out double __state)
        {
            __state = __instance.ReagentMixture != null ? __instance.ReagentMixture.TotalReagents : -1.0;
        }

        public static void Postfix(FurnaceBase __instance, double __state)
        {
            if (__state <= 0.0) return;
            IngotReagentTracker.OnPoolConsumed(__instance.ReagentMixture, __state);
        }
    }

    // Same, for the Arc Furnace (DropIngots is private, so it's targeted by name).
    [HarmonyPatch(typeof(ArcFurnace), "DropIngots")]
    public static class ArcFurnaceIngotPoolPatch
    {
        public static void Prefix(ArcFurnace __instance, out double __state)
        {
            __state = __instance.ReagentMixture != null ? __instance.ReagentMixture.TotalReagents : -1.0;
        }

        public static void Postfix(ArcFurnace __instance, double __state)
        {
            if (__state <= 0.0) return;
            IngotReagentTracker.OnPoolConsumed(__instance.ReagentMixture, __state);
        }
    }

    // Furnace and AdvancedFurnace each override GetSmelterScale (FurnaceBase's version is a
    // plain 1), and both already have the full multiplier applied by IngotYieldPatch. Here
    // the result is rescaled to the ore/ingot blend: result / multiplier gives the vanilla
    // per-unit scale, and the blended multiplier puts back only what ore-sourced units earn.
    // The game multiplies this scale by the whole unit count itself, hence a per-unit average.
    [HarmonyPatch]
    public static class IngotScaleBlendPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Furnace), nameof(FurnaceBase.GetSmelterScale));
            yield return AccessTools.Method(typeof(AdvancedFurnace), nameof(FurnaceBase.GetSmelterScale));
        }

        public static void Postfix(FurnaceBase __instance, ref float __result)
        {
            var mix = __instance.ReagentMixture;
            if (mix == null || __result <= 0f) return;
            float multiplier = SaltysYieldMultiplier.IngotMultiplier;
            float blended = IngotReagentTracker.BlendedMultiplier(mix, multiplier);
            if (blended == multiplier) return;
            float before = __result;
            __result = __result / multiplier * blended;
            SaltysYieldMultiplier.LogVerbose(
                $"IngotScaleBlendPatch fired: scale {before} -> {__result} " +
                $"(ingot share={IngotReagentTracker.IngotFraction(mix)})");
        }
    }
}
