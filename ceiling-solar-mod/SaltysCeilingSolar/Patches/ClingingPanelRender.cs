using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using UnityEngine;

namespace SaltysCeilingSolar.Patches
{
    // The panel frame is a "Batched" structure: Structure.Awake destroys the root MeshRenderer and
    // the frame mesh is drawn by RenderBatch with Graphics.DrawMeshInstanced at the matrix
    // BatchInstance.GetTRS() builds from the ROOT transform. The root's rotation belongs to
    // placement, so for clinging panels FlipMatrix is multiplied into that matrix here instead.
    // Shadows use the same matrices.
    [HarmonyPatch(typeof(BatchInstance), nameof(BatchInstance.GetTRS))]
    public static class ClingingPanelRenderPatch
    {
        private static bool _logged;

        public static void Postfix(ref BatchInstance __instance, ref Matrix4x4 __result)
        {
            try
            {
                Thing parent = __instance.Batchable?.GetParent;
                // Only the frame drawn at the root: child renderers (Cells, the destroyed-state
                // mesh) sit on transforms that already carry the flip.
                if (parent is SolarPanel && parent.PrefabName == ClingingPanelPrefabPatch.ClingingName
                    && ReferenceEquals(__instance.Transform, parent.ThingTransform))
                {
                    __result = __result * ClingingPanelPrefabPatch.FlipMatrix;
                    if (!_logged)
                    {
                        _logged = true;
                        SaltysCeilingSolar.Log("Clinging panel frame flipped in the batch renderer (first draw)");
                    }
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
