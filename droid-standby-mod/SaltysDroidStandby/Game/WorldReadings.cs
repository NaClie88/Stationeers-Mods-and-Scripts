using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Objects;
using UnityEngine;
using Weather;

namespace SaltysDroidStandby.Game
{
    // Game state -> plain WorldSnapshot (spec §3/§6). Sources verified in the decompile:
    // light = sun height (OrbitalSimulation.WorldSunVector, the vector vanilla's day curve
    // uses) x storm dimming (WeatherManager.GetSolarRatioAt); wind = the 0..1 noise wind
    // turbines use; storm = WeatherManager running + affecting this altitude; solar storm =
    // that event has a DirectionalLight (vanilla's own solar-storm camera gate); outdoors =
    // not in a Room (as MovementController's jump code checks).
    public static class WorldReadings
    {
        public static WorldSnapshot Read(Human h)
        {
            float height = h.Position.y;
            bool storm = WeatherManager.CurrentEventAffects(height);
            var atmosphere = h.BreathingAtmosphere;
            return new WorldSnapshot
            {
                LightPercent = Mathf.Clamp01(Vector3.Dot(Vector3.up, OrbitalSimulation.WorldSunVector.normalized))
                               * WeatherManager.GetSolarRatioAt(height) * 100f,
                WindPercent = Mathf.Clamp01(WindTurbineGenerator.WindStrength) * 100f,
                StormActive = storm,
                SolarStormActive = storm && WeatherManager.CurrentWeatherEvent?.DirectionalLight != null,
                Outdoors = h.Room == null,
                BatteryRatio = TotalBattery(h),
                BruteBurn = h.DamageState.Brute + h.DamageState.Burn,
                PressureKpa = atmosphere != null ? atmosphere.PressureGassesAndLiquids.ToFloat() : 0f,
                TemperatureK = atmosphere != null ? atmosphere.Temperature.ToFloat() : 293.15f,
            };
        }

        // Spec §4.4: every battery-type slot, so the Dual Battery mod's beta slot is included
        // without referencing that mod.
        public static float TotalBattery(Human h)
        {
            var cells = new List<KeyValuePair<float, float>>();
            foreach (Slot slot in h.Slots)
            {
                if (slot.Type == Slot.Class.Battery && slot.Get() is BatteryCell cell)
                {
                    // PowerStored isn't networked; clients use the synced percentage (C2).
                    cells.Add(BatteryMath.Cell(cell.PowerStored, cell.PowerMaximum,
                        cell.CurrentPowerPercentage, NetworkManager.IsClient));
                }
            }
            return BatteryMath.TotalRatio(cells);
        }
    }
}
