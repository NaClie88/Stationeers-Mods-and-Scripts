using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using HarmonyLib;
using Objects.Items;
using Reagents;
using UnityEngine;

namespace SaltysFabricatorOverflow.Patches
{
    // Printers keep no ingot slots: FabricatorBase.CollectResource pours every imported stack
    // into one ReagentMixture pool (1 ingot = 1.0 reagent), which has no upper bound. Vanilla's
    // 500 limit only appears on the way back out (Thing.DropReagent takes at most 500 per
    // ingot). These two patches enforce a per-ingot-type cap on the pool itself: inserts are
    // timestamped, and once EjectDelaySeconds pass with no further insert, anything above the
    // cap is pushed out through the machine's own export path, one <=500 stack per cycle.

    internal static class InsertTimes
    {
        // Weak keys so a deconstructed machine never leaves an entry behind.
        private static readonly ConditionalWeakTable<FabricatorBase, StrongBox<float>> _lastInsert =
            new ConditionalWeakTable<FabricatorBase, StrongBox<float>>();

        public static void Mark(FabricatorBase machine)
        {
            _lastInsert.GetOrCreateValue(machine).Value = Time.time;
        }

        // No entry (e.g. a save loaded with a machine already over the cap) counts as "long ago".
        public static float SecondsSince(FabricatorBase machine)
        {
            return _lastInsert.TryGetValue(machine, out var box) ? Time.time - box.Value : float.MaxValue;
        }
    }

    // CollectResource is the single entry point for imported ingots (hand-placed and chute
    // imports both arrive via FabricatorBase.OnImportClosingComplete -> CollectResource).
    [HarmonyPatch(typeof(FabricatorBase), nameof(FabricatorBase.CollectResource))]
    public static class FabricatorInsertTrackingPatch
    {
        public static void Postfix(FabricatorBase __instance)
        {
            if (__instance is SimpleFabricatorBase)
            {
                InsertTimes.Mark(__instance);
            }
        }
    }

    // Runs right after vanilla's own export tick, so vanilla's open-and-empty and print-output
    // paths always get first claim on the export slot. Anything we place into ExportSlot is
    // pushed out by vanilla on the next tick (CanBeginExport -> InteractExport -> chute/world),
    // so overflow follows an attached export chute like any other output.
    [HarmonyPatch(typeof(SimpleFabricatorBase), "OnServerExportTick")]
    public static class FabricatorOverflowEjectPatch
    {
        private const int GameStackLimit = 500;

        // Reagent hash -> ingot prefab (null = this reagent has no ingot; never capped, so the
        // mod can't turn something into ItemReagentMix slag).
        private static readonly Dictionary<int, Ingot> _ingotForReagent = new Dictionary<int, Ingot>();

        public static void Postfix(SimpleFabricatorBase __instance)
        {
            try
            {
                if (!GameManager.RunSimulation
                    || !__instance.OnOff || !__instance.Powered || !__instance.IsStructureCompleted
                    || __instance.IsOpen || __instance.Activate != 0
                    || !__instance.IsNextExportReady || __instance.ExportSlot.IsNotEmpty())
                {
                    return;
                }
                if (InsertTimes.SecondsSince(__instance) < SaltysFabricatorOverflow.Delay)
                {
                    return;
                }

                int cap = SaltysFabricatorOverflow.Cap;
                ReagentMixture pool = __instance.ReagentMixture;
                foreach (Reagent reagent in Reagent.AllReagents)
                {
                    double amount = pool.Get(reagent);
                    if (amount <= cap)
                    {
                        continue;
                    }
                    int excess = Math.Min((int)Math.Floor(amount - cap), GameStackLimit);
                    if (excess < 1)
                    {
                        continue;
                    }
                    Ingot prefab = FindIngotPrefab(pool, reagent);
                    if (prefab == null)
                    {
                        continue;
                    }
                    Eject(__instance, reagent, prefab, excess);
                    return; // one stack per export cycle
                }
            }
            catch (Exception e)
            {
                SaltysFabricatorOverflow.Log("Overflow eject threw on " + __instance?.DisplayName);
                SaltysFabricatorOverflow.Log(e.ToString());
            }
        }

        private static Ingot FindIngotPrefab(ReagentMixture pool, Reagent reagent)
        {
            if (_ingotForReagent.TryGetValue(reagent.Hash, out var cached))
            {
                return cached;
            }
            // Same lookup Thing.DropReagent(Reagent, Slot) does, run on a throwaway copy so the
            // real pool is only touched once we know an ingot prefab exists.
            ReagentMixture probe = new ReagentMixture();
            probe.Add(new ReagentMixture(pool).Take(reagent, 1.0));
            Ingot.RecipeComparable.Recipes.TryGetValue(new Recipe(probe.Normalize(), null), out var prefab);
            _ingotForReagent[reagent.Hash] = prefab;
            return prefab;
        }

        // Mirrors Thing.DropReagent(Reagent, Slot) + its private CreateReagent(Ingot, int, Slot),
        // but with our own quantity instead of the hardcoded 500.
        private static void Eject(SimpleFabricatorBase machine, Reagent reagent, Ingot prefab, int quantity)
        {
            Reagent taken = machine.ReagentMixture.Take(reagent, quantity);
            if (taken == null)
            {
                return;
            }
            ReagentMixture single = new ReagentMixture();
            single.Add(taken);
            int count = (int)taken.Quantity;
            single = single.Normalize();

            Ingot ingot = Thing.Create<Ingot>(prefab, machine.ExportSlot.Location);
            ingot.Quantity = count;
            ingot.CreatedReagentMixture = single;
            OnServer.MoveToSlotOrWorld(ingot, machine.ExportSlot);

            SaltysFabricatorOverflow.LogVerbose("Ejected " + count + " " + prefab.DisplayName +
                " from " + machine.DisplayName + " (now holds " + machine.ReagentMixture.Get(reagent) + ")");
        }
    }
}
