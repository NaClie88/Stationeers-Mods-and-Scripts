using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using UnityEngine;

namespace SaltysCeilingSolar.Patches
{
    // Every solar panel variant (basic, flat, angled, heavy, dual) is the same SolarPanel class;
    // which ways it can be placed comes only from three serialized Structure fields on the prefab:
    // PlacementType, RotationAxis and AllowedRotations. InventoryManager reads them off the
    // construction cursor to decide which rotate keys work and which faces are allowed.
    //
    // The power maths needs no change: SolarPanelArm measures sunlight along its own Cells.forward
    // and casts its rays from the cells' own frame, so a panel mounted upside down or sideways
    // already computes the right efficiency for wherever its cells point.
    //
    // So the only change is to open up the rotation fields. The cursor and every placed panel are
    // instantiated from the prefab, so widening them in Awake covers both.
    [HarmonyPatch(typeof(SolarPanel), "Awake")]
    public static class SolarPanelRotationPatch
    {
        // Awake runs on the main thread, so a plain HashSet is fine.
        private static readonly HashSet<string> _logged = new HashSet<string>();

        public static void Postfix(Structure __instance)
        {
            try
            {
                if (!(__instance is SolarPanel))
                {
                    return; // only if Harmony fell back to an inherited Awake
                }

                RotationAxis oldAxis = __instance.RotationAxis;
                AllowedRotations oldAllowed = __instance.AllowedRotations;

                __instance.RotationAxis = RotationAxis.All;
                __instance.AllowedRotations = AllowedRotations.All;

                if (_logged.Add(__instance.PrefabName))
                {
                    SaltysCeilingSolar.Log("Widened rotation on " + __instance.PrefabName +
                        ": PlacementType=" + __instance.PlacementType +
                        ", RotationAxis " + oldAxis + " -> All" +
                        ", AllowedRotations " + oldAllowed + " -> All" +
                        ", sunlight blocked by layers [" + DescribeCollisionMask(__instance) + "]");
                }
            }
            catch (Exception e)
            {
                SaltysCeilingSolar.Log("Solar panel rotation patch threw");
                SaltysCeilingSolar.Log(e.ToString());
            }
        }

        // Diagnostic only: tells us whether glass (window) layers block a panel's sunlight rays,
        // which decides whether a panel behind a glass roof or wall can work at all.
        private static string DescribeCollisionMask(Structure panel)
        {
            LayerMask mask = Traverse.Create(panel).Field("CollisionMask").GetValue<LayerMask>();
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
    }
}
