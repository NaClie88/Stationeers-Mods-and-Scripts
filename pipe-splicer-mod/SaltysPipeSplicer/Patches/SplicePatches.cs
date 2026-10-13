using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using HarmonyLib;
using UnityEngine;

namespace SaltysPipeSplicer.Patches
{
    // Wrench-merging a pipe or cable piece onto the middle of a long straight (3, 5 or 10 long).
    //
    // The game's MultiMergeConstructor.Construct (server/host only) looks up the piece in the
    // target cell, ORs its open ends with the new piece's, destroys it and builds ONE single-cell
    // junction. For a long straight that deletes every other cell of the run. This patch keeps the
    // game's merge for the junction cell and then rebuilds the rest of the run either side of it
    // from the same family's straights (10/5/3/1, cheapest mix), with the same rotation, owner
    // and paint.
    //
    // Cost: long straights are discounted against that many singles, so breaking one charges the
    // difference from the kit in hand: what the replacement pieces plus a single straight at the
    // junction cost, minus what the long piece cost. The game's merge then charges the junction
    // itself as it would against a single straight.
    //
    // Gas: a pipe network keeps its gas in one shared atmosphere. When a piece is destroyed the
    // game keeps every mole and shares it over what's left by volume, so pressure only spikes if
    // the volume drops. The replacement pieces are built in the same frame, before the next
    // atmospherics tick, and cover exactly the cells that were there, so the volume (and the
    // pressure) ends up where it started. A before/after pressure line is logged to check this.
    [HarmonyPatch(typeof(MultiMergeConstructor), "Construct",
        new[] { typeof(Grid3), typeof(Quaternion), typeof(int), typeof(Item), typeof(bool), typeof(ulong) })]
    public static class MultiMergeSplicePatch
    {
        public class Plan
        {
            public Structure Old;
            public string Family;
            public List<Piece> Pieces = new List<Piece>();
            public int Extra;
            public string PressureBefore;
            public string MolesBefore;
        }

        public struct Piece
        {
            public Structure Prefab;
            public Grid3 Origin;
        }

        public static bool Prefix(MultiMergeConstructor __instance, Grid3 localPosition, int optionIndex, bool authoringMode, out Plan __state)
        {
            __state = null;
            try
            {
                if (!GameManager.RunSimulation)
                {
                    return true;
                }
                Structure old = (Structure)__instance.GridController.GetPipe(localPosition) ?? __instance.GridController.GetCable(localPosition);
                if (old == null || !Splice.IsLongStraight(old))
                {
                    return true; // not a long piece: the game's merge is fine as it is
                }

                Plan plan = Splice.MakePlan(old, Splice.SmallCellOf(old.GridController, localPosition), out string problem);
                if (plan == null)
                {
                    // The game's merge would delete the whole run, so block it instead.
                    SaltysPipeSplicer.Log("Refusing to merge into " + old.PrefabName + ": " + problem);
                    return false;
                }

                if (!authoringMode)
                {
                    // OnUseItem clamps to what's in the stack, so check first. The junction's own
                    // charge comes out of the game's merge afterwards.
                    int needed = Math.Max(0, plan.Extra) + Splice.EntryOf(__instance.Constructables[optionIndex]);
                    if (__instance.Quantity < needed)
                    {
                        SaltysPipeSplicer.Log("Refusing to splice " + old.PrefabName + ": needs " + needed + " from the kit, has " + __instance.Quantity);
                        return false;
                    }
                    if (plan.Extra > 0)
                    {
                        __instance.OnUseItem(plan.Extra, null);
                    }
                }

                Splice.DescribeAsStraight(old);

                if (old is Pipe pipe && pipe.PipeNetwork != null && pipe.PipeNetwork.Atmosphere != null)
                {
                    plan.PressureBefore = pipe.PipeNetwork.Atmosphere.PressureGassesAndLiquids.ToString();
                    plan.MolesBefore = pipe.PipeNetwork.Atmosphere.GasMixture.GetTotalMolesGassesAndLiquids.ToString();
                }
                __state = plan;
                return true;
            }
            catch (Exception e)
            {
                SaltysPipeSplicer.Log("Splice prefix threw; blocking this merge so no run is lost");
                SaltysPipeSplicer.Log(e.ToString());
                return false;
            }
        }

        public static void Postfix(Grid3 localPosition, Plan __state)
        {
            if (__state == null)
            {
                return;
            }
            try
            {
                foreach (Piece piece in __state.Pieces)
                {
                    var instance = new CreateStructureInstance(piece.Prefab, __state.Old) { LocalGrid = piece.Origin };
                    Constructor.SpawnConstruct(instance);
                }
                SaltysPipeSplicer.Log("Spliced " + __state.Old.PrefabName + ": rebuilt " + __state.Pieces.Count +
                    " piece(s) around the junction [" + Splice.Describe(__state.Pieces) + "], extra cost " + __state.Extra);

                if (__state.MolesBefore != null && SaltysPipeSplicer.Instance != null)
                {
                    SaltysPipeSplicer.Instance.StartCoroutine(LogPressureAfter(localPosition, __state));
                }
            }
            catch (Exception e)
            {
                SaltysPipeSplicer.Log("Splice postfix threw while rebuilding " + __state.Old.PrefabName);
                SaltysPipeSplicer.Log(e.ToString());
            }
        }

        private static IEnumerator LogPressureAfter(Grid3 junction, Plan plan)
        {
            yield return new WaitForSeconds(2f);
            Pipe pipe = GridController.World.GetPipe(junction);
            if (pipe == null || pipe.PipeNetwork == null || pipe.PipeNetwork.Atmosphere == null)
            {
                SaltysPipeSplicer.Log("Pressure check: no pipe network at the junction");
                yield break;
            }
            SaltysPipeSplicer.Log("Pressure check for " + plan.Family + ": before " + plan.PressureBefore +
                " kPa, " + plan.MolesBefore + " mol; after " + pipe.PipeNetwork.Atmosphere.PressureGassesAndLiquids +
                " kPa, " + pipe.PipeNetwork.Atmosphere.GasMixture.GetTotalMolesGassesAndLiquids + " mol");
        }
    }

    // Lets the wrench-merge cursor through over a long straight on the client. The game refuses
    // a merge if the occupant's prefab sets DontAllowMergingWithWrench; long straights may set it
    // precisely because the vanilla merge would delete the run. The splice above makes it safe.
    [HarmonyPatch(typeof(Piping), "CanReplace")]
    public static class PipingCanReplacePatch
    {
        public static void Postfix(Piping __instance, MultiConstructor constructor, Item inactiveHandItem, ref CanConstructInfo __result)
        {
            try
            {
                if (__result.CanConstruct || __instance.DontAllowMergingWithWrench || !Splice.HoldingWrench(constructor, inactiveHandItem))
                {
                    return;
                }
                foreach (SmallCell cell in Splice.CellsOf(__instance))
                {
                    if (cell != null && cell.Pipe is Piping other && Splice.IsLongStraight(other) &&
                        other.PipeType == __instance.PipeType && other.PipeContentType == __instance.PipeContentType)
                    {
                        __result = Splice.CheckCost(other, __instance, constructor);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                SaltysPipeSplicer.LogOnce("Piping.CanReplace postfix threw: " + e);
            }
        }
    }

    // Cables: same cost check once the game has accepted the merge.
    [HarmonyPatch(typeof(Cable), "CanReplace")]
    public static class CableCanReplacePatch
    {
        public static void Postfix(Cable __instance, MultiConstructor constructor, Item inactiveHandItem, ref CanConstructInfo __result)
        {
            try
            {
                if (!__result.CanConstruct || !Splice.HoldingWrench(constructor, inactiveHandItem))
                {
                    return;
                }
                foreach (SmallCell cell in Splice.CellsOf(__instance))
                {
                    if (cell != null && cell.Cable != null && cell.Cable != __instance && Splice.IsLongStraight(cell.Cable))
                    {
                        __result = Splice.CheckCost(cell.Cable, __instance, constructor);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                SaltysPipeSplicer.LogOnce("Cable.CanReplace postfix threw: " + e);
            }
        }
    }

    // Cables: a long cable whose prefab sets BlockMergeWithOtherCables counts as a collision.
    [HarmonyPatch(typeof(Cable), "_IsCollision")]
    public static class CableCollisionPatch
    {
        public static void Postfix(Cable __instance, SmallGrid smallGrid, ref bool __result)
        {
            try
            {
                if (__result && !__instance.BlockMergeWithOtherCables && smallGrid is Cable other &&
                    other.CableType == __instance.CableType && Splice.IsLongStraight(other))
                {
                    __result = false;
                }
            }
            catch (Exception e)
            {
                SaltysPipeSplicer.LogOnce("Cable._IsCollision postfix threw: " + e);
            }
        }
    }

    public static class Splice
    {
        // "StructurePipeStraight10" -> "StructurePipeStraight", length 10.
        public static bool SplitName(string prefabName, out string family, out int length)
        {
            family = prefabName;
            length = 1;
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }
            foreach (int l in new[] { 10, 5, 3 })
            {
                string suffix = l.ToString();
                if (prefabName.EndsWith(suffix, StringComparison.Ordinal))
                {
                    family = prefabName.Substring(0, prefabName.Length - suffix.Length);
                    length = l;
                    return true;
                }
            }
            return false;
        }

        public static bool IsLongStraight(Structure piece)
        {
            return piece != null && SplitName(piece.PrefabName, out string family, out _) &&
                family.IndexOf("Straight", StringComparison.Ordinal) >= 0 &&
                piece.GridBounds != null && piece.GridBounds._gridsSmall != null && piece.GridBounds._gridsSmall.Length > 1;
        }

        public static Grid3[] Cells(Structure piece)
        {
            return (Grid3[])piece.GridBounds.GetLocalSmallGrid(piece.ThingTransformPosition, piece.ThingTransformRotation);
        }

        public static IEnumerable<SmallCell> CellsOf(Structure piece)
        {
            foreach (Grid3 g in Cells(piece))
            {
                yield return piece.GridController.GetSmallCell(g);
            }
        }

        public static bool HoldingWrench(MultiConstructor constructor, Item offhand)
        {
            if (!(constructor is MultiMergeConstructor merge) || offhand == null || merge.ToolExit == null)
            {
                return false;
            }
            return merge.ToolExit.PrefabHash == offhand.PrefabHash ||
                (offhand.ReplacementOf != null && merge.ToolExit.PrefabHash == offhand.ReplacementOf.PrefabHash);
        }

        // The game shapes the junction from the OR of the new piece's and the old piece's open-end
        // layout, and prices it against the kit's first constructable with the old piece's
        // ConnectionType. Both are only right if the long piece describes itself as a Straight
        // along its own length, so make sure it does. It's about to be destroyed anyway.
        public static void DescribeAsStraight(Structure old)
        {
            if (!(old is ISmartRotatable rotatable))
            {
                return;
            }
            SmartRotate.ConnectionType type = rotatable.GetConnectionType();
            string perm = string.Join(",", rotatable.GetOpenEndsPermutation());

            // Which local axis the run lies along, from the prefab's own cells.
            Grid3[] local = old.GridBounds._gridsSmall;
            Vector3 span = local[local.Length - 1].ToVector3() - local[0].ToVector3();
            float x = Mathf.Abs(span.x), y = Mathf.Abs(span.y), z = Mathf.Abs(span.z);
            // Face slots: 0 Up, 1 South, 2 East, 3 North, 4 West, 5 Down.
            int[] straight = y >= x && y >= z ? new[] { 1, 0, 0, 0, 0, 1 }
                : z >= x ? new[] { 0, 1, 0, 1, 0, 0 }
                : new[] { 0, 0, 1, 0, 1, 0 };

            bool same = type == SmartRotate.ConnectionType.Straight && perm == string.Join(",", straight);
            if (!same)
            {
                rotatable.SetConnectionType(SmartRotate.ConnectionType.Straight);
                rotatable.SetOpenEndsPermutation(straight);
            }
            SaltysPipeSplicer.LogOnce("Merge layout for " + old.PrefabName + ": ConnectionType " + type + " [" + perm + "]" +
                (same ? " (already a straight)" : " -> Straight [" + string.Join(",", straight) + "] for the merge"));
        }

        public static int EntryOf(Structure prefab)
        {
            return prefab.BuildStates[0].Tool.EntryQuantity;
        }

        // A structure origin as the small cell it sits in, in the same units as Cells().
        public static Grid3 SmallCellOf(GridController grid, Grid3 origin)
        {
            return grid.WorldToLocalGrid(grid.LocalToWorld(origin), SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
        }

        // Client side: is the kit in hand enough for this splice?
        public static CanConstructInfo CheckCost(Structure longPiece, Structure cursor, MultiConstructor kit)
        {
            Grid3[] cursorCells = Cells(cursor);
            var plan = MakePlan(longPiece, cursorCells[0], out string problem);
            if (plan == null)
            {
                return CanConstructInfo.InvalidPlacement("Can't split " + longPiece.DisplayName + " here: " + problem);
            }
            int needed = Math.Max(0, plan.Extra) + EntryOf(cursor);
            if (kit.Quantity < needed)
            {
                return CanConstructInfo.InvalidPlacement("Splitting " + longPiece.DisplayName + " needs " + needed +
                    " from this kit (" + (needed - kit.Quantity) + " more)");
            }
            return CanConstructInfo.ValidPlacement;
        }

        public static MultiMergeSplicePatch.Plan MakePlan(Structure old, Grid3 junctionCell, out string problem)
        {
            problem = null;
            SplitName(old.PrefabName, out string family, out _);
            Grid3[] cells = Cells(old);
            int j = Array.IndexOf(cells, junctionCell);
            if (j < 0)
            {
                problem = "junction cell " + junctionCell + " is not one of its cells";
                return null;
            }

            var plan = new MultiMergeSplicePatch.Plan { Old = old, Family = family };
            if (!AddRun(plan, family, cells, 0, j, out problem) ||
                !AddRun(plan, family, cells, j + 1, cells.Length - j - 1, out problem))
            {
                return null;
            }
            if (!Prefab.TryFind<Structure>(family, out Structure single))
            {
                problem = "no single " + family + " prefab";
                return null;
            }
            int cost = EntryOf(single); // the junction cell, charged as a single straight
            foreach (var piece in plan.Pieces)
            {
                cost += EntryOf(piece.Prefab);
            }
            plan.Extra = cost - EntryOf(old);
            return plan;
        }

        // Covers cells[start .. start+count) with the family's straights, choosing the cheapest
        // mix of lengths (fewest pieces on a tie).
        private static bool AddRun(MultiMergeSplicePatch.Plan plan, string family, Grid3[] cells, int start, int count, out string problem)
        {
            problem = null;
            if (count == 0)
            {
                return true;
            }
            var prefabs = new Dictionary<int, Structure>();
            foreach (int length in new[] { 1, 3, 5, 10 })
            {
                string name = length == 1 ? family : family + length;
                if (Prefab.TryFind<Structure>(name, out Structure prefab) && prefab.GridBounds != null)
                {
                    prefabs[length] = prefab;
                }
            }

            // best[i] = cheapest (cost, pieces) covering the first i cells of this run.
            var best = new Step[count + 1];
            best[0] = new Step();
            for (int i = 0; i < count; i++)
            {
                if (best[i] == null)
                {
                    continue;
                }
                foreach (var kv in prefabs)
                {
                    int length = kv.Key;
                    if (i + length > count || !TryFit(plan.Old, kv.Value, cells, start + i, length, out Grid3 origin))
                    {
                        continue;
                    }
                    var candidate = new Step { Cost = best[i].Cost + EntryOf(kv.Value), Pieces = best[i].Pieces + 1, Last = length, Origin = origin };
                    Step current = best[i + length];
                    if (current == null || candidate.Cost < current.Cost ||
                        (candidate.Cost == current.Cost && candidate.Pieces < current.Pieces))
                    {
                        best[i + length] = candidate;
                    }
                }
            }
            if (best[count] == null)
            {
                problem = "no " + family + " pieces fit the cells from " + cells[start];
                return false;
            }
            var pieces = new List<MultiMergeSplicePatch.Piece>();
            for (int i = count; i > 0; i -= best[i].Last)
            {
                pieces.Add(new MultiMergeSplicePatch.Piece { Prefab = prefabs[best[i].Last], Origin = best[i].Origin });
            }
            pieces.Reverse();
            plan.Pieces.AddRange(pieces);
            return true;
        }

        private class Step
        {
            public int Cost;
            public int Pieces;
            public int Last;
            public Grid3 Origin;
        }

        // Finds the origin that puts the prefab, with the old piece's rotation, exactly on
        // cells[at .. at+length), in either direction along the run.
        private static bool TryFit(Structure old, Structure prefab, Grid3[] cells, int at, int length, out Grid3 origin)
        {
            origin = default(Grid3);
            GridController grid = old.GridController;
            Quaternion rotation = old.ThingTransformRotation;
            Grid3[] local = prefab.GridBounds._gridsSmall;
            if (local == null || local.Length != length)
            {
                return false;
            }
            var target = new HashSet<Grid3>();
            for (int i = 0; i < length; i++)
            {
                target.Add(cells[at + i]);
            }
            // Line the prefab's first or last cell up with the first target cell.
            foreach (Vector3 anchorLocal in new[] { local[0].ToVector3(), local[length - 1].ToVector3() })
            {
                Vector3 pos = grid.LocalToWorld(cells[at]) - rotation * anchorLocal;
                Grid3 candidate = pos.ToGridPosition();
                Grid3[] placedCells = (Grid3[])prefab.GridBounds.GetLocalSmallGrid(grid.LocalToWorld(candidate), rotation);
                if (placedCells.Length == length && target.SetEquals(placedCells))
                {
                    origin = candidate;
                    return true;
                }
            }
            return false;
        }

        public static string Describe(List<MultiMergeSplicePatch.Piece> pieces)
        {
            var names = new List<string>();
            foreach (var p in pieces)
            {
                names.Add(p.Prefab.PrefabName);
            }
            return string.Join(", ", names.ToArray());
        }
    }
}
