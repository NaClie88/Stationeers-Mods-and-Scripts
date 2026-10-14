using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using UnityEngine;

namespace SaltysCeilingSolar.Patches
{
    // VerboseLogging only. When a clinging panel faces the sun but makes nothing, log what its
    // centre ray hit, at most every 10 s per panel. Answers the open question of whether glass
    // (or the panel's own frame, now that the rays leave through its back) blocks sunlight.
    [HarmonyPatch(typeof(SolarPanel), nameof(SolarPanel.CalculateSolarEfficiency))]
    public static class ClingingPanelDiagnosticsPatch
    {
        private const float LogEverySeconds = 10f;

        private static readonly ConditionalWeakTable<SolarPanel, StrongBox<float>> _nextLog =
            new ConditionalWeakTable<SolarPanel, StrongBox<float>>();

        // Runs on the main thread (UniTask FixedUpdate loop).
        public static void Postfix(SolarPanel __instance)
        {
            if (!SaltysCeilingSolar.Verbose || __instance.PrefabName != ClingingPanelPrefabPatch.ClingingName
                || __instance.GenerationEfficiency > 0f)
            {
                return;
            }
            try
            {
                StrongBox<float> next = _nextLog.GetOrCreateValue(__instance);
                if (Time.time < next.Value)
                {
                    return;
                }
                next.Value = Time.time + LogEverySeconds;

                Vector3 sun = OrbitalSimulation.WorldSunVector;
                foreach (var arm in Traverse.Create(__instance).Field("_panelArms").GetValue<System.Collections.Generic.List<Objects.Electrical.SolarPanelArm>>())
                {
                    if (arm == null || arm.Cells == null)
                    {
                        continue;
                    }
                    float dot = Vector3.Dot(arm.Cells.forward, sun);
                    if (dot <= 0f)
                    {
                        continue; // night, or facing away from the sun: nothing to explain
                    }
                    var ray = new Ray(arm.Cells.position + arm.Cells.rotation * new Vector3(0f, 0f, 0.5f), sun);
                    if (Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, __instance.CollisionMask))
                    {
                        SaltysCeilingSolar.Log("Clinging panel at " + __instance.transform.position + " makes 0: sunlight blocked by '" +
                            hit.collider.name + "' (layer " + LayerMask.LayerToName(hit.collider.gameObject.layer) +
                            ", trigger=" + hit.collider.isTrigger + ") " + hit.distance.ToString("F2") + " m away");
                    }
                    else
                    {
                        SaltysCeilingSolar.Log("Clinging panel at " + __instance.transform.position + " makes 0 with a clear centre ray (terrain, eclipse, or angle over 60 degrees from the sun)");
                    }
                }
            }
            catch (Exception e)
            {
                SaltysCeilingSolar.Log("Clinging diagnostic threw: " + e.Message);
            }
        }
    }
}
