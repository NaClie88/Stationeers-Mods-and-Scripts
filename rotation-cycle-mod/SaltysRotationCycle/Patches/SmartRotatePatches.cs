using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using HarmonyLib;
using UnityEngine;

namespace SaltysRotationCycle.Patches
{
    // The C key (KeyMap.QuantityModifier) while placing runs SmartRotate._GetNext once per tap,
    // backwards with Shift held, and once per wheel notch while C is held. Grid, Face and
    // FaceMount placement all go through it.
    //
    // The game's version doesn't step to the next orientation: it tries every orientation in the
    // part's ConnectionType table and jumps to the first valid one with the most connections, so
    // the order a player sees looks random. This prefix replaces it with a fixed order:
    //   1. spin the part a quarter turn at a time about its main axis (4 presses), then
    //   2. tip the main axis to the next direction: up, north, east, south, west, down.
    // The main axis of a grid part comes from its connection points (see MainAxis); a
    // face-placed part spins about the face it is mounted on (local forward).
    //
    // Only the final Quaternion is sent to the server or saved, never a rotation index, so this is
    // client-side only.
    [HarmonyPatch(typeof(SmartRotate), "_GetNext")]
    public static class SmartRotateCyclePatch
    {
        // Main-axis directions in cycle order.
        private static readonly Vector3[] AxisOrder =
        {
            Vector3.up, Vector3.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.down
        };

        // Face parts always spin about their mount face (local forward).
        private static readonly Quaternion[] FaceCycle = BuildCycle(Vector3.forward);

        // Grid parts: one cycle per prefab, built from that prefab's main axis.
        private static readonly Dictionary<string, Quaternion[]> _gridCycles = new Dictionary<string, Quaternion[]>();
        private static Dictionary<string, Vector3> _overrides;

        private const NetworkType FlowTypes = NetworkType.Pipe | NetworkType.PipeLiquid | NetworkType.Chute;

        // _GetNext runs on the main thread, so a plain HashSet is fine.
        private static readonly HashSet<string> _logged = new HashSet<string>();

        // The game's own "RotateBlueprint" sound hash (private static on SmartRotate).
        private static readonly int RotateBlueprintHash =
            (int)AccessTools.Field(typeof(SmartRotate), "RotateBlueprintHash").GetValue(null);

        public static bool Prefix(ISmartRotatable rotatable, bool isForward, Quaternion offset, Vector3 centerOfRotation)
        {
            if (rotatable == null)
            {
                return true;
            }
            try
            {
                // Pipes and cables that will merge into the piece they're placed on get the same
                // cycle: the game only skips its auto-pick for them, the orientations are the same.
                Quaternion[] cycle = PickCycle(rotatable);
                if (cycle != null)
                {
                    StepMainAxisCycle(rotatable, cycle, isForward, offset, centerOfRotation);
                }
                else
                {
                    StepGameTable(rotatable, isForward, offset, centerOfRotation);
                }
                UIAudioManager.Play(RotateBlueprintHash);
                return false;
            }
            catch (Exception e)
            {
                SaltysRotationCycle.Log("Rotation cycle threw; using the game's smart rotate for this press");
                SaltysRotationCycle.Log(e.ToString());
                return true;
            }
        }

        // Parts that can turn freely on every axis get the main-axis cycle. Restricted parts
        // (one or two axes, wall-only, floor-only, wall-mounted) would be rotated into
        // orientations they don't allow, so they step through the game's own table instead,
        // without the jump-to-most-connections behaviour.
        private static Quaternion[] PickCycle(ISmartRotatable rotatable)
        {
            if (rotatable.GetRotationAxis() != RotationAxis.All)
            {
                return null;
            }
            switch (rotatable.GetPlacementType())
            {
                case PlacementSnap.Grid:
                    return rotatable.GetAllowedRotations() == AllowedRotations.Floor ? null : GridCycleFor(rotatable);
                case PlacementSnap.Face:
                    return rotatable.GetAllowedRotations() == AllowedRotations.All ? FaceCycle : null;
                default:
                    return null;
            }
        }

        private static void StepMainAxisCycle(ISmartRotatable rotatable, Quaternion[] cycle, bool isForward,
            Quaternion offset, Vector3 centerOfRotation)
        {
            // Orientations whose open ends land in the same places look identical to the game
            // (e.g. spinning a straight pipe about its own length), so by default only the first of
            // each is kept. The order of the ones kept is unchanged.
            var stops = new List<Quaternion>(cycle.Length);
            var keys = new List<string>(cycle.Length);
            bool everySpin = SaltysRotationCycle.ShowEverySpin.Value;
            for (int i = 0; i < cycle.Length; i++)
            {
                string key = everySpin ? i.ToString() : PermutationKey(rotatable, offset, cycle[i]);
                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    stops.Add(cycle[i]);
                }
            }

            Quaternion current = rotatable.Transform.rotation;
            int nearest = NearestIndex(cycle, current);
            string currentKey = everySpin ? nearest.ToString() : PermutationKey(rotatable, offset, cycle[nearest]);
            int at = keys.IndexOf(currentKey);
            int next = ((at + (isForward ? 1 : -1)) % stops.Count + stops.Count) % stops.Count;

            // Rotate from the actual (possibly drifted) rotation straight to the exact target,
            // which also snaps away any float drift from earlier presses.
            Quaternion delta = stops[next] * Quaternion.Inverse(current);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.01f && !float.IsInfinity(axis.x) && !float.IsNaN(axis.x))
            {
                rotatable.Rotate(axis, angle, offset, centerOfRotation);
            }

            LogOnce(rotatable, (cycle == FaceCycle ? "face (mount side)" : "grid") + " main-axis cycle, " + stops.Count + " stops");
        }

        // The game's own table for this part, one entry per press, with no auto-pick.
        private static void StepGameTable(ISmartRotatable rotatable, bool isForward, Quaternion offset, Vector3 centerOfRotation)
        {
            SmartRotate.ConnectionType type = rotatable.GetConnectionType();
            SmartRotate.RotationInformation[] table = SmartRotate.RotationsList[type];
            int index = SmartRotate.OrientationLookup[type][rotatable.GetOpenEndLocationPermutation(offset)];

            Quaternion step;
            if (isForward)
            {
                step = table[index].Rotation;
            }
            else
            {
                index = (index - 1 + table.Length) % table.Length;
                step = Quaternion.Inverse(table[index].Rotation);
            }
            step.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.01f && !float.IsInfinity(axis.x) && !float.IsNaN(axis.x))
            {
                rotatable.Rotate(axis, angle, offset, centerOfRotation);
            }

            LogOnce(rotatable, "game table " + type + ", " + table.Length + " stops");
        }

        private static Quaternion[] GridCycleFor(ISmartRotatable rotatable)
        {
            string name = PrefabName(rotatable);
            if (!_gridCycles.TryGetValue(name, out Quaternion[] cycle))
            {
                Vector3 main = MainAxis(rotatable, name, out string why);
                cycle = BuildCycle(main);
                _gridCycles[name] = cycle;
                SaltysRotationCycle.Log("Main axis for " + name + ": local " + AxisName(main) + " (" + why + ")");
            }
            return cycle;
        }

        // Which local axis a grid part spins about, as a positive unit axis (X, Y or Z).
        //   1. A config override for this prefab.
        //   2. The line through two opposite pipe/chute ends: inline pumps, valves, regulators,
        //      straight pipes, and a tee's through-run. With two such lines (a cross), the axis
        //      at right angles to both.
        //   3. The line all pipe/chute ends sit on: a tank or vent with one port.
        //   4. Steps 2-3 again with every end (power/data too), for cable-inline parts.
        //   5. Otherwise the part's own up: elbows, parts with no ends.
        private static Vector3 MainAxis(ISmartRotatable rotatable, string name, out string why)
        {
            if (Overrides().TryGetValue(name, out Vector3 forced))
            {
                why = "config override";
                return forced;
            }

            var flow = new List<Vector3>();
            var all = new List<Vector3>();
            var described = new List<string>();
            List<Connection> ends = rotatable.GetOpenEnds();
            if (ends != null)
            {
                foreach (Connection end in ends)
                {
                    if (end == null || end.Transform == null || !end.Transform.IsChildOf(rotatable.Transform))
                    {
                        continue;
                    }
                    Vector3 local = rotatable.Transform.InverseTransformDirection(end.Transform.forward);
                    if (local.sqrMagnitude < 0.25f)
                    {
                        continue;
                    }
                    Vector3 dir = Snap(local);
                    all.Add(dir);
                    if ((end.ConnectionType & FlowTypes) != 0)
                    {
                        flow.Add(dir);
                    }
                    described.Add(end.ConnectionType + "/" + end.ConnectionRole + " " + SignedName(dir));
                }
            }
            string endList = "ends: " + (described.Count > 0 ? string.Join(", ", described.ToArray()) : "none");

            if (LineFrom(flow, out Vector3 line) || LineFrom(all, out line))
            {
                why = endList;
                return line;
            }
            why = "no single connection line, using the part's up; " + endList;
            return Vector3.up;
        }

        private static bool LineFrom(List<Vector3> dirs, out Vector3 line)
        {
            line = Vector3.zero;
            if (dirs.Count == 0)
            {
                return false;
            }
            // Exactly one axis with ends on both sides.
            int pairs = 0;
            Vector3 unpaired = Vector3.zero;
            foreach (Vector3 axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
            {
                if (dirs.Contains(axis) && dirs.Contains(-axis))
                {
                    pairs++;
                    line = axis;
                }
                else
                {
                    unpaired = axis;
                }
            }
            if (pairs == 1)
            {
                return true;
            }
            if (pairs == 2)
            {
                line = unpaired; // a flat cross spins in its own plane
                return true;
            }
            if (pairs == 3)
            {
                return false;
            }
            // Otherwise every end on the same side.
            foreach (Vector3 d in dirs)
            {
                if (d != dirs[0])
                {
                    return false;
                }
            }
            line = Abs(dirs[0]);
            return true;
        }

        // "StructureTankSmall=Y, StructureVolumePump=Z" -> prefab name to local axis.
        private static Dictionary<string, Vector3> Overrides()
        {
            if (_overrides != null)
            {
                return _overrides;
            }
            _overrides = new Dictionary<string, Vector3>();
            foreach (string entry in (SaltysRotationCycle.AxisOverrides.Value ?? "").Split(','))
            {
                string[] parts = entry.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }
                string axis = parts[1].Trim().ToUpperInvariant();
                Vector3 v = axis == "X" ? Vector3.right : axis == "Y" ? Vector3.up : axis == "Z" ? Vector3.forward : Vector3.zero;
                if (v != Vector3.zero)
                {
                    _overrides[parts[0].Trim()] = v;
                }
                else
                {
                    SaltysRotationCycle.Log("Ignoring AxisOverrides entry '" + entry.Trim() + "': axis must be X, Y or Z");
                }
            }
            return _overrides;
        }

        // Main axis first: for each world direction, the orientation that puts the part's main
        // axis on it, then four quarter-turn spins about it.
        private static Quaternion[] BuildCycle(Vector3 mainLocal)
        {
            Quaternion local = Quaternion.LookRotation(mainLocal, Perpendicular(mainLocal));
            var cycle = new Quaternion[24];
            int n = 0;
            foreach (Vector3 axis in AxisOrder)
            {
                Quaternion start = Quaternion.LookRotation(axis, Perpendicular(axis)) * Quaternion.Inverse(local);
                for (int spin = 0; spin < 4; spin++)
                {
                    cycle[n++] = Quaternion.AngleAxis(90f * spin, axis) * start;
                }
            }
            return cycle;
        }

        private static Vector3 Perpendicular(Vector3 axis)
        {
            return Mathf.Abs(axis.y) > 0.5f ? Vector3.forward : Vector3.up;
        }

        private static Vector3 Snap(Vector3 v)
        {
            float x = Mathf.Abs(v.x), y = Mathf.Abs(v.y), z = Mathf.Abs(v.z);
            if (x >= y && x >= z)
            {
                return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            }
            return y >= z ? new Vector3(0f, Mathf.Sign(v.y), 0f) : new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

        private static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        private static string AxisName(Vector3 axis)
        {
            return axis.x != 0f ? "X" : axis.y != 0f ? "Y" : "Z";
        }

        private static string SignedName(Vector3 dir)
        {
            return (dir.x + dir.y + dir.z > 0f ? "+" : "-") + AxisName(dir);
        }

        private static int NearestIndex(Quaternion[] cycle, Quaternion rotation)
        {
            int best = 0;
            float bestAngle = float.MaxValue;
            for (int i = 0; i < cycle.Length; i++)
            {
                float a = Quaternion.Angle(cycle[i], rotation);
                if (a < bestAngle)
                {
                    bestAngle = a;
                    best = i;
                }
            }
            return best;
        }

        private static string PermutationKey(ISmartRotatable rotatable, Quaternion offset, Quaternion rotation)
        {
            return string.Join(",", rotatable.GetOpenEndLocationPermutation(offset, rotation));
        }

        private static string PrefabName(ISmartRotatable rotatable)
        {
            return rotatable is Thing thing && !string.IsNullOrEmpty(thing.PrefabName) ? thing.PrefabName : rotatable.GetType().Name;
        }

        private static void LogOnce(ISmartRotatable rotatable, string what)
        {
            string name = PrefabName(rotatable);
            if (_logged.Add(name))
            {
                SaltysRotationCycle.Log("C cycle for " + name + ": " + what +
                    " (PlacementType=" + rotatable.GetPlacementType() +
                    ", RotationAxis=" + rotatable.GetRotationAxis() +
                    ", AllowedRotations=" + rotatable.GetAllowedRotations() +
                    ", ConnectionType=" + rotatable.GetConnectionType() + ")");
            }
        }
    }
}
