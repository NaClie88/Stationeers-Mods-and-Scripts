using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using UnityEngine;

namespace SaltysCeilingSolar.Patches
{
    // The panel frame is a "Batched" structure: Structure.Awake destroys the root MeshRenderer and
    // RenderBatch draws each build state's mesh with Graphics.DrawMeshInstanced at the pose of
    // ThingRenderer.GetRendererTransform(), which for these DrawData renderers is the panel's ROOT
    // transform. The root's rotation belongs to placement, so for clinging panels this hands
    // RenderBatch a child pivot that carries FlipMatrix instead. Register, the per-instance
    // matrix and Deregister all ask the same method, so they stay consistent; shadows use the
    // same matrices.
    //
    // (v0.1 postfixed BatchInstance.GetTRS instead. It never fired in game: a tiny struct method
    // like that gets inlined by the JIT. GetRendererTransform is called through the IBatchable
    // interface, so it can't be.)
    [HarmonyPatch]
    public static class ClingingPanelRenderPatch
    {
        public const string PivotName = "~SaltysClingFlip";

        private static bool _logged;

        public static MethodBase TargetMethod()
        {
            Type thingRenderer = AccessTools.TypeByName("ThingRenderer");
            SaltysCeilingSolar.Log("ThingRenderer resolved to " + (thingRenderer != null ? thingRenderer.FullName : "nothing"));
            return AccessTools.Method(thingRenderer, "GetRendererTransform", Type.EmptyTypes);
        }

        public static void Postfix(object __instance, ref Transform __result)
        {
            try
            {
                if (__result == null || !__result.name.StartsWith(ClingingPanelPrefabPatch.ClingingName, StringComparison.Ordinal))
                {
                    return; // cheap filter: clinging roots are named after the prefab (maybe with "(Clone)")
                }
                Thing parent = Traverse.Create(__instance).Property("Parent").GetValue<Thing>()
                    ?? Traverse.Create(__instance).Field("Parent").GetValue<Thing>();
                if (parent == null || !ReferenceEquals(__result, parent.ThingTransform))
                {
                    return; // a child renderer: its own transform already carries the flip
                }
                Transform pivot = __result.Find(PivotName);
                if (pivot == null)
                {
                    return;
                }
                __result = pivot;
                if (!_logged)
                {
                    _logged = true;
                    SaltysCeilingSolar.Log("Clinging panel frame drawn at the flip pivot (first draw)");
                }
            }
            catch (Exception e)
            {
                SaltysCeilingSolar.Log("Clinging render patch threw: " + e.Message);
            }
        }
    }

    // Meson scanner overlays draw structures at Structure.GetBatchMatrix().
    [HarmonyPatch(typeof(Structure), nameof(Structure.GetBatchMatrix))]
    public static class ClingingPanelScannerPatch
    {
        public static void Postfix(Structure __instance, ref Matrix4x4 __result)
        {
            if (__instance.PrefabName == ClingingPanelPrefabPatch.ClingingName)
            {
                __result = __result * ClingingPanelPrefabPatch.FlipMatrix;
            }
        }
    }

    // The construction cursor doesn't draw the batched frame; it shows an instance of the prefab's
    // Blueprint (a wireframe ghost) parented at the cursor's root with an identity local pose.
    // Give the clinging cursor's ghost the flip so the preview matches what gets built.
    [HarmonyPatch(typeof(InventoryManager), "HandleStructurePrefab")]
    public static class ClingingPanelCursorPatch
    {
        public static void Postfix(Thing prefab)
        {
            try
            {
                if (prefab == null || prefab.PrefabName != ClingingPanelPrefabPatch.ClingingName)
                {
                    return;
                }
                var cursors = Traverse.Create(typeof(InventoryManager)).Field("_constructionCursors")
                    .GetValue<Dictionary<string, Structure>>();
                if (cursors == null || !cursors.TryGetValue(prefab.name, out Structure cursor) || cursor == null)
                {
                    SaltysCeilingSolar.Log("Clinging cursor not found; placement preview stays unflipped");
                    return;
                }
                if (cursor.Wireframe == null)
                {
                    SaltysCeilingSolar.Log("Clinging cursor has no blueprint ghost; preview uses its child meshes");
                    return;
                }
                Transform ghost = cursor.Wireframe.transform;
                Matrix4x4 f = ClingingPanelPrefabPatch.FlipMatrix;
                ghost.localPosition = f.MultiplyPoint3x4(ghost.localPosition);
                ghost.localRotation = f.rotation * ghost.localRotation;
                SaltysCeilingSolar.Log("Clinging cursor preview flipped");
            }
            catch (Exception e)
            {
                SaltysCeilingSolar.Log("Clinging cursor patch threw");
                SaltysCeilingSolar.Log(e.ToString());
            }
        }
    }
}
