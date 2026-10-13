using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using LaunchPadBooster.Utils;
using Objects.Electrical;
using UnityEngine;

namespace SaltysCeilingSolar.Patches
{
    // Adds "Solar Panel (Clinging)" as a third style of the Kit (Solar Panel Basic), next to Flat
    // and Angled. It is a copy of the flat panel whose cells are turned 180 degrees, so they face
    // INTO whatever the panel is mounted on: out through a glass wall, or up through a glass roof.
    // The vanilla Flat and Angled prefabs are left untouched.
    //
    // Why a Prefab.LoadAll prefix: at that point WorldManager.SourcePrefabs is filled but nothing
    // is registered yet and InventoryManager hasn't built its construction cursors, so a prefab
    // added to SourcePrefabs here is registered, gets a cursor and is wired into the kit exactly
    // like a vanilla one. LaunchPadBooster's own AddPrefabs uses the same hook.
    //
    // Power needs no patch: SolarPanelArm measures sunlight along Cells.forward and starts its
    // rays from the cells' own frame, so turned cells compute correctly on their own.
    [HarmonyPatch(typeof(Prefab), nameof(Prefab.LoadAll))]
    public static class ClingingPanelPrefabPatch
    {
        public const string ClingingName = "StructureSolarPanelClinging";
        private const string FlatName = "StructureSolarPanelFlat";
        private const string AngledName = "StructureSolarPanel45";
        private const string KitName = "ItemKitSolarPanelBasic";

        private static bool _done;

        public static void Prefix()
        {
            // LoadAll returns early once prefabs exist; LaunchPad can also re-enter on reload.
            if (_done || Prefab.PrefabsGameObject != null)
            {
                return;
            }
            _done = true;
            try
            {
                List<Thing> sources = WorldManager.Instance.SourcePrefabs;
                SolarPanel flat = sources.Find(p => p != null && p.PrefabName == FlatName) as SolarPanel;
                SolarPanel angled = sources.Find(p => p != null && p.PrefabName == AngledName) as SolarPanel;
                if (flat == null)
                {
                    SaltysCeilingSolar.Log("Clinging panel NOT added: no " + FlatName + " in SourcePrefabs");
                    return;
                }

                // Instantiating under an inactive parent keeps Awake/OnEnable from running on the copy.
                var holder = new GameObject("~SaltysCeilingSolarPrefabs");
                holder.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(holder);

                SolarPanel clone = UnityEngine.Object.Instantiate(flat, holder.transform);
                clone.name = ClingingName;
                clone.PrefabName = ClingingName;
                clone.PrefabHash = Animator.StringToHash(ClingingName);

                // Placement: borrow the Angled panel's settings. Vanilla already lets it go on
                // floors, walls and ceilings (upside down), and that combination is one the game's
                // smart-rotate and cable open-end code handles. The Flat panel can't do ceilings.
                if (angled != null)
                {
                    clone.PlacementType = angled.PlacementType;
                    clone.RotationAxis = angled.RotationAxis;
                    clone.AllowedRotations = angled.AllowedRotations;
                }
                else
                {
                    clone.AllowedRotations = AllowedRotations.All;
                    SaltysCeilingSolar.Log("No " + AngledName + " found; clinging panel uses AllowedRotations=All");
                }

                int flipped = FlipCells(clone);

                sources.Add(clone);
                clone.AddToMultiConstructorKit(KitName);

                SaltysCeilingSolar.Log("Clinging panel added to " + KitName + " (" + flipped + " cell group(s) flipped)" +
                    ". Flat: " + Describe(flat) + ". Angled: " + Describe(angled) + ". Clinging: " + Describe(clone) +
                    ". Sunlight blocked by layers [" + DescribeMask(clone.CollisionMask) + "]");
                SaltysCeilingSolar.Log("Clinging panel hierarchy:" + DescribeHierarchy(clone));
            }
            catch (Exception e)
            {
                SaltysCeilingSolar.Log("Adding the clinging panel threw");
                SaltysCeilingSolar.Log(e.ToString());
            }
        }

        // Turn each arm's cells half a turn about their own X axis: Cells.forward (the panel
        // normal) now points back at the mount. Changing the prefab copy only; the flat panel's
        // shared meshes and materials are not touched.
        private static int FlipCells(SolarPanel panel)
        {
            var arms = Traverse.Create(panel).Field("_panelArms").GetValue<List<SolarPanelArm>>();
            int count = 0;
            if (arms == null)
            {
                return count;
            }
            foreach (SolarPanelArm arm in arms)
            {
                if (arm != null && arm.Cells != null)
                {
                    arm.Cells.localRotation = arm.Cells.localRotation * Quaternion.Euler(180f, 0f, 0f);
                    count++;
                }
            }
            return count;
        }

        private static string Describe(Structure s)
        {
            if (s == null)
            {
                return "missing";
            }
            return "PlacementType=" + s.PlacementType + ", RotationAxis=" + s.RotationAxis +
                ", AllowedRotations=" + s.AllowedRotations;
        }

        private static string DescribeMask(LayerMask mask)
        {
            var names = new StringBuilder();
            for (int layer = 0; layer < 32; layer++)
            {
                if ((mask.value & (1 << layer)) == 0)
                {
                    continue;
                }
                if (names.Length > 0)
                {
                    names.Append(", ");
                }
                string name = LayerMask.LayerToName(layer);
                names.Append(string.IsNullOrEmpty(name) ? layer.ToString() : name);
            }
            return names.ToString();
        }

        // One-time layout dump, so the in-game test can tell whether the cell mesh hangs under
        // Cells (and so turns visibly) and which layers the panel's own colliders use.
        private static string DescribeHierarchy(SolarPanel panel)
        {
            var cells = new HashSet<Transform>();
            var arms = Traverse.Create(panel).Field("_panelArms").GetValue<List<SolarPanelArm>>();
            if (arms != null)
            {
                foreach (SolarPanelArm arm in arms)
                {
                    if (arm == null)
                    {
                        continue;
                    }
                    if (arm.Cells != null)
                    {
                        cells.Add(arm.Cells);
                    }
                    if (arm.YawPivot != null || arm.PitchPivot != null)
                    {
                        SaltysCeilingSolar.Log("Note: clinging panel arm has a yaw/pitch pivot (tracking)");
                    }
                }
            }
            var sb = new StringBuilder();
            DescribeTransform(panel.transform, 0, cells, sb);
            return sb.ToString();
        }

        private static void DescribeTransform(Transform t, int depth, HashSet<Transform> cells, StringBuilder sb)
        {
            if (depth > 6)
            {
                return;
            }
            sb.Append("\n  ").Append(' ', depth * 2).Append(t.name);
            if (cells.Contains(t))
            {
                sb.Append(" [Cells]");
            }
            if (t.GetComponent<MeshRenderer>() != null)
            {
                sb.Append(" [mesh]");
            }
            if (t.GetComponent<Collider>() != null)
            {
                sb.Append(" [collider layer ").Append(LayerMask.LayerToName(t.gameObject.layer)).Append(']');
            }
            foreach (Transform child in t)
            {
                DescribeTransform(child, depth + 1, cells, sb);
            }
        }
    }
}
