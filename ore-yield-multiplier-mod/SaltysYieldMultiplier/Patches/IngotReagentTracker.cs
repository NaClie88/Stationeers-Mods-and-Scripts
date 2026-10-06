using System.Runtime.CompilerServices;
using Reagents;

namespace SaltysYieldMultiplier.Patches
{
    // The furnaces melt ores and ingots into the same ReagentMixture pool (an ingot melts back
    // to the same reagent its recipe was made from), and only apply the ingot-count scale once,
    // on the way out -- so the pool alone can't tell "reagent that came from an ore" (should be
    // multiplied) from "reagent that came from an ingot" (should be 1:1). This remembers, per
    // pool, how many reagent units were added by ingots so the output scale can be blended.
    //
    // Keyed on the ReagentMixture object itself (not the furnace) so it works for FurnaceBase
    // (Furnace/AdvancedFurnace) and ArcFurnace alike -- both hand their own ReagentMixture to
    // Smelt(). ConditionalWeakTable lets entries die with the mixture, and is thread-safe.
    // Not persisted: reloading a save mid-batch forgets any ingot-sourced share, so that one
    // batch is treated as ore. See INVESTIGATE_LATER.md.
    internal static class IngotReagentTracker
    {
        private sealed class Entry
        {
            public double IngotUnits;
        }

        private static readonly ConditionalWeakTable<ReagentMixture, Entry> Table =
            new ConditionalWeakTable<ReagentMixture, Entry>();

        // An empty pool has no ingot-sourced share, even if ReagentMixture.Clear() (called by
        // the furnaces directly, not through anything patched here) wiped it since last time.
        public static void ResetIfEmpty(ReagentMixture mix, double totalBefore)
        {
            if (totalBefore < 0.001 && Table.TryGetValue(mix, out var entry))
            {
                entry.IngotUnits = 0.0;
            }
        }

        public static void RecordIngotMelt(ReagentMixture mix, double unitsAdded)
        {
            if (unitsAdded <= 0.0) return;
            Table.GetOrCreateValue(mix).IngotUnits += unitsAdded;
        }

        // Called after a furnace turned part of the pool into ingots or de-gassed/dirty ore. The ingot-sourced
        // share drops by the same fraction the pool did (assumes an evenly mixed pool -- the
        // pool itself can't say which molecules came from where).
        public static void OnPoolConsumed(ReagentMixture mix, double totalBefore)
        {
            if (totalBefore <= 0.0 || !Table.TryGetValue(mix, out var entry)) return;
            double after = mix.TotalReagents;
            if (after <= 0.0)
            {
                entry.IngotUnits = 0.0;
                return;
            }
            entry.IngotUnits *= System.Math.Min(1.0, after / totalBefore);
        }

        // Fraction (0..1) of the pool's reagent that came from ingots.
        public static float IngotFraction(ReagentMixture mix)
        {
            if (!Table.TryGetValue(mix, out var entry) || entry.IngotUnits <= 0.0) return 0f;
            double total = mix.TotalReagents;
            if (total <= 0.0) return 0f;
            return (float)System.Math.Min(1.0, entry.IngotUnits / total);
        }

        // Average output multiplier per reagent unit: ore-sourced units count as `multiplier`
        // ingots each, ingot-sourced units count as 1. Returns the plain multiplier for a pool
        // with no ingot share, so untouched pools behave exactly as before.
        public static float BlendedMultiplier(ReagentMixture mix, float multiplier)
        {
            float f = IngotFraction(mix);
            return f <= 0f ? multiplier : (1f - f) * multiplier + f;
        }
    }
}
