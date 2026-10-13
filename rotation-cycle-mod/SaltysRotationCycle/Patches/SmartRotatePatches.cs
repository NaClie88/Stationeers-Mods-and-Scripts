using System;
using System.Collections.Generic;
using System.Reflection;
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
    // The main axis is the part's local up for grid parts and its local forward (the face it is
    // mounted on) for face-placed parts.
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

        // The 24 orientations in cycle order, one list per main-axis convention.
        private static readonly Quaternion[] GridCycle = BuildCycle(mainIsForward: false);
        private static readonly Quaternion[] FaceCycle = BuildCycle(mainIsForward: true);

        // _GetNext runs on the main thread, so a plain HashSet is fine.
        private static readonly HashSet<string> _logged = new HashSet<string>();

        private static MethodInfo _willMerge;
        private static Type _gridMergeable;
        private static MethodInfo _playSound;
        private static readonly int RotateBlueprintHash = Animator.StringToHash("RotateBlueprint");

        public static void Prepare()
        {
            _gridMergeable = AccessTools.TypeByName("IGridMergeable");
            if (_gridMergeable != null)
            {
                _willMerge = AccessTools.Method(_gridMergeable, "WillMergeWhenPlaced");
            }
            Type audio = AccessTools.TypeByName("UIAudioManager");
            if (audio != null)
            {
                _playSound = AccessTools.Method(audio, "Play", new[] { typeof(int) });
            }
            SaltysRotationCycle.Log("Lookups: IGridMergeable " + (_willMerge != null ? "found" : "MISSING") +
                ", UIAudioManager.Play " + (_playSound != null ? "found" : "MISSING"));
        }

        public static bool Prefix(ISmartRotatable rotatable, bool isForward, Quaternion offset, Vector3 centerOfRotation)
        {
            if (rotatable == null)
            {
                return true;
            }
            try
            {
                // A pipe or cable that will merge into the one it's placed on keeps the game's
                // plain single step: its orientation must match what it merges with.
                if (_willMerge != null && _gridMergeable.IsInstanceOfType(rotatable) &&
                    (bool)_willMerge.Invoke(rotatable, null))
                {
                    return true;
                }

                Quaternion[] cycle = PickCycle(rotatable);
                if (cycle != null)
                {
                    StepMainAxisCycle(rotatable, cycle, isForward, offset, centerOfRotation);
                }
                else
                {
                    StepGameTable(rotatable, isForward, offset, centerOfRotation);
                }
                PlaySound();
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
                    return rotatable.GetAllowedRotations() == AllowedRotations.Floor ? null : GridCycle;
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

            LogOnce(rotatable, (cycle == FaceCycle ? "face" : "grid") + " main-axis cycle, " + stops.Count + " stops");
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

        // Main axis first: for each axis direction, four quarter-turn spins about it.
        private static Quaternion[] BuildCycle(bool mainIsForward)
        {
            var cycle = new Quaternion[24];
            int n = 0;
            foreach (Vector3 axis in AxisOrder)
            {
                Vector3 reference = Mathf.Abs(axis.y) > 0.5f ? Vector3.forward : Vector3.up;
                Quaternion start = mainIsForward
                    ? Quaternion.LookRotation(axis, reference)   // local forward on the axis
                    : Quaternion.LookRotation(reference, axis);  // local up on the axis
                for (int spin = 0; spin < 4; spin++)
                {
                    cycle[n++] = Quaternion.AngleAxis(90f * spin, axis) * start;
                }
            }
            return cycle;
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

        private static void PlaySound()
        {
            if (_playSound != null)
            {
                _playSound.Invoke(null, new object[] { RotateBlueprintHash });
            }
        }

        private static void LogOnce(ISmartRotatable rotatable, string what)
        {
            string name = rotatable is Thing thing ? thing.PrefabName : rotatable.GetType().Name;
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
