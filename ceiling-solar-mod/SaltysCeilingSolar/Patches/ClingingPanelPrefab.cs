using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using Objects.Electrical;
using UnityEngine;

namespace SaltysCeilingSolar.Patches
{
    // Adds "Solar Panel (Clinging)" as a third style of the Kit (Solar Panel Basic), next to Flat
    // and Angled. It is a copy of the flat panel with the whole model turned upside down against
    // its mount, so the cells face INTO whatever the panel clings to: out through a glass wall, or
    // up through a glass roof. The vanilla Flat and Angled prefabs are left untouched.
    //
    // Why a Prefab.LoadAll prefix: at that point WorldManager.SourcePrefabs is filled but nothing
    // is registered yet and InventoryManager hasn't built its construction cursors, so a prefab
    // added to SourcePrefabs here is registered, gets a cursor and is wired into the kit exactly
    // like a vanilla one. LaunchPadBooster's own AddPrefabs uses the same hook.
    //
    // Power needs no patch: SolarPanelArm measures sunlight along Cells.forward and starts its
    // rays from the cells' own frame, so the turned cells compute correctly on their own.
    [HarmonyPatch(typeof(Prefab), nameof(Prefab.LoadAll))]
    public static class ClingingPanelPrefabPatch
    {
        public const string ClingingName = "StructureSolarPanelClinging";
        private const string FlatName = "StructureSolarPanelFlat";
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

                // Placement: Flat and Angled are both FaceMount / Z / All in vanilla (logged
                // 2026-10-13), so the copy keeps the flat panel's placement unchanged.

                if (!ComputeFlip(clone))
                {
                    return;
                }
                FlipChildren(clone);

                // Pivot the batched frame is drawn at (see ClingingPanelRenderPatch).
                var pivot = new GameObject(ClingingPanelRenderPatch.PivotName).transform;
                pivot.SetParent(clone.transform, false);
                pivot.localPosition = FlipMatrix.MultiplyPoint3x4(Vector3.zero);
                pivot.localRotation = FlipMatrix.rotation;

                // Our own kit insertion rather than LaunchPadBooster's AddToMultiConstructorKit:
                // that helper also sets BuildStates[0].Tool.ToolExit to the kit, which made the
                // kit the tool needed to deconstruct the panel. Appended, so the existing kit
                // indices (sent over the network as OptionIndex) don't move.
                MultiConstructor kit = sources.Find(p => p != null && p.PrefabName == KitName) as MultiConstructor;
                if (kit == null)
                {
                    SaltysCeilingSolar.Log("Clinging panel NOT added: no " + KitName + " in SourcePrefabs");
                    return;
                }
                sources.Add(clone);
                if (!kit.Constructables.Contains(clone))
                {
                    kit.Constructables.Add(clone);
                }

                SaltysCeilingSolar.Log("Clinging panel added to " + KitName + " as style " + kit.Constructables.Count +
                    ". Placement: " + Describe(clone) + ". Deconstruct tool: " + DescribeExitTools(clone) +
                    ". Flip: normal " + _normal + ", extent " + _min.ToString("F3") + ".." + _max.ToString("F3") +
                    ". Sunlight blocked by layers [" + DescribeMask(clone.CollisionMask) + "]");
                SaltysCeilingSolar.Log("Clinging panel layout after flip:" + DescribeHierarchy(clone));
            }
            catch (Exception e)
            {
                SaltysCeilingSolar.Log("Adding the clinging panel threw");
                SaltysCeilingSolar.Log(e.ToString());
            }
        }

        // The whole model turns half a turn about an axis lying in the mount plane, then shifts
        // along the mount normal so it still occupies the same slab of space: the cells end up
        // against the mount and the frame sticks out into the room.
        //
        // The frame mesh is batch-rendered at the ROOT transform (Structure.Awake destroys the
        // root MeshRenderer and draws BuildStates[i].InitialDrawData at the root's matrix), and
        // the root's rotation belongs to placement. So the root stays as is, its children and
        // collider get FlipMatrix here, and ClingingPanelRenderPatch multiplies FlipMatrix into
        // the batch matrix of clinging panels.
        public static Matrix4x4 FlipMatrix = Matrix4x4.identity;
        private static Vector3 _normal;
        private static float _min;
        private static float _max;

        private static bool ComputeFlip(SolarPanel panel)
        {
            var arms = Traverse.Create(panel).Field("_panelArms").GetValue<List<SolarPanelArm>>();
            if (arms == null || arms.Count == 0 || arms[0] == null || arms[0].Cells == null)
            {
                SaltysCeilingSolar.Log("Clinging panel NOT added: flat panel has no Cells");
                return false;
            }
            // Vanilla cells face straight away from the mount, so their facing in the root's
            // frame is the mount normal. Snap it to the nearest local axis.
            Vector3 n = panel.transform.InverseTransformDirection(arms[0].Cells.forward);
            _normal = SnapToAxis(n);

            // Extent of the whole model along the normal: the batch-rendered frame mesh of the
            // final build state, plus every child renderer, in root-local space.
            _min = float.MaxValue;
            _max = float.MinValue;
            if (panel.BuildStates != null)
            {
                foreach (var state in panel.BuildStates)
                {
                    Mesh mesh = state?.InitialDrawData.mesh;
                    if (mesh != null)
                    {
                        Encapsulate(mesh.bounds, Matrix4x4.identity);
                    }
                }
            }
            foreach (Renderer r in panel.GetComponentsInChildren<Renderer>(true))
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && r.transform != panel.transform)
                {
                    Encapsulate(mf.sharedMesh.bounds, panel.transform.worldToLocalMatrix * r.transform.localToWorldMatrix);
                }
            }
            if (_min > _max)
            {
                SaltysCeilingSolar.Log("Clinging panel NOT added: no mesh bounds to flip around");
                return false;
            }

            Vector3 axis = Mathf.Abs(_normal.x) < 0.5f ? Vector3.right : Vector3.up;
            FlipMatrix = Matrix4x4.Translate(_normal * (_min + _max)) * Matrix4x4.Rotate(Quaternion.AngleAxis(180f, axis));
            return true;
        }

        private static Vector3 SnapToAxis(Vector3 v)
        {
            Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
            if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            if (a.y >= a.z) return new Vector3(0f, Mathf.Sign(v.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

        private static void Encapsulate(Bounds b, Matrix4x4 toRoot)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                float d = Vector3.Dot(toRoot.MultiplyPoint3x4(corner), _normal);
                _min = Mathf.Min(_min, d);
                _max = Mathf.Max(_max, d);
            }
        }

        private static void FlipChildren(SolarPanel panel)
        {
            Quaternion rot = FlipMatrix.rotation;
            foreach (Transform child in panel.transform)
            {
                child.localPosition = FlipMatrix.MultiplyPoint3x4(child.localPosition);
                child.localRotation = rot * child.localRotation;
            }
            // Colliders on the root itself can't be moved by a transform; shift their centres.
            foreach (Collider c in panel.GetComponents<Collider>())
            {
                switch (c)
                {
                    case BoxCollider box:
                        box.center = FlipMatrix.MultiplyPoint3x4(box.center);
                        break;
                    case SphereCollider sphere:
                        sphere.center = FlipMatrix.MultiplyPoint3x4(sphere.center);
                        break;
                    case CapsuleCollider capsule:
                        capsule.center = FlipMatrix.MultiplyPoint3x4(capsule.center);
                        break;
                    default:
                        SaltysCeilingSolar.Log("Root collider " + c.GetType().Name + " left unflipped");
                        break;
                }
            }
        }

        private static string DescribeExitTools(Structure s)
        {
            var sb = new StringBuilder();
            if (s.BuildStates != null)
            {
                foreach (var state in s.BuildStates)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }
                    var exit = state?.Tool?.ToolExit;
                    sb.Append(exit != null ? exit.PrefabName : "none");
                }
            }
            return sb.ToString();
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
            if (panel.OpenEnds != null)
            {
                foreach (var end in panel.OpenEnds)
                {
                    sb.Append("\n  open end ").Append(end.ConnectionType).Append(" on ")
                        .Append(end.Transform != null ? end.Transform.name : "null");
                }
            }
            return sb.ToString();
        }

        private static void DescribeTransform(Transform t, int depth, HashSet<Transform> cells, StringBuilder sb)
        {
            if (depth > 6)
            {
                return;
            }
            sb.Append("\n  ").Append(' ', depth * 2).Append(t.name)
                .Append(" local pos ").Append(t.localPosition.ToString("F3"))
                .Append(" rot ").Append(t.localEulerAngles.ToString("F0"));
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
