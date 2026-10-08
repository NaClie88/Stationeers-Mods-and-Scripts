using System;

namespace SaltysDroidStandby
{
    // Plain snapshot of everything the wake conditions read (spec §6). Filled from the game
    // by Game/WorldReadings.cs; constructed by hand in tests.
    public struct WorldSnapshot
    {
        public float LightPercent;     // 0..100, sun height x storm dimming
        public float WindPercent;      // 0..100
        public bool StormActive;       // a weather event currently affects this altitude
        public bool SolarStormActive;  // ...and it is a solar storm
        public bool Outdoors;          // not inside a Room
        public float BatteryRatio;     // 0..1, summed over battery slots
        public float BruteBurn;        // brute + burn damage (stun excluded on purpose)
        public float PressureKpa;      // surrounding atmosphere
        public float TemperatureK;     // surrounding atmosphere
    }

    public sealed class WakeThresholds
    {
        public float LightPercent = 20f;
        public float WindPercent = 40f;
        public float BatteryChargedRatio = 0.9f;
        public float BatteryLowRatio = 0.05f;
        public float DamageDelta = 1f;
        public float PressureDeltaKpa = 20f;
        public float SafeTempMinK = 223.15f;
        public float SafeTempMaxK = 323.15f;
        public int ConsecutiveChecks = 3;
    }

    // One evaluator per Deep Standby stay. Check() is called about once per second; it
    // returns a reason string when a selected condition fires, else null.
    public sealed class WakeEvaluator
    {
        private readonly WakeThresholds _t;
        private readonly bool _entryStorm;
        private readonly bool _entrySolar;

        private bool _lightArmed, _windArmed, _chargedArmed, _lowArmed, _tempArmed;
        private int _lightCount, _windCount, _chargedCount, _lowCount, _stormCount, _tempCount;
        private float _lastBruteBurn, _lastPressure;

        // wakeOnLow false: the Battery condition only wakes on "charged" (Standby -- a silent wake
        // at low battery would just return an AFK droid to full drain; final review I5).
        public WakeEvaluator(WakeCondition selected, WakeThresholds thresholds, WorldSnapshot atEntry, bool wakeOnLow = true)
        {
            Selected = selected;
            _t = thresholds;
            _entryStorm = atEntry.StormActive;
            _entrySolar = atEntry.SolarStormActive;
            _lightArmed = atEntry.LightPercent < _t.LightPercent;
            _windArmed = atEntry.WindPercent < _t.WindPercent;
            _chargedArmed = atEntry.BatteryRatio < _t.BatteryChargedRatio;
            _lowArmed = wakeOnLow && atEntry.BatteryRatio > _t.BatteryLowRatio;
            _tempArmed = InBand(atEntry.TemperatureK);
            _lastBruteBurn = atEntry.BruteBurn;
            _lastPressure = atEntry.PressureKpa;
        }

        public WakeCondition Selected { get; }

        // True when the most recent wake was "battery low" -- the only automatic wake after
        // which the safety net should stand down until real input (final-review I4).
        public bool LastWakeWasBatteryLow { get; private set; }

        public string Check(WorldSnapshot now)
        {
            string reason = null;

            if (Has(WakeCondition.Light) &&
                Rising(now.LightPercent, _t.LightPercent, ref _lightArmed, ref _lightCount))
            {
                reason = $"sunrise (light {now.LightPercent:F0}%)";
            }
            if (reason == null && Has(WakeCondition.Wind) &&
                Rising(now.WindPercent, _t.WindPercent, ref _windArmed, ref _windCount))
            {
                reason = $"wind (wind {now.WindPercent:F0}%)";
            }
            if (reason == null && Has(WakeCondition.Battery))
            {
                if (Rising(now.BatteryRatio, _t.BatteryChargedRatio, ref _chargedArmed, ref _chargedCount))
                {
                    reason = $"battery charged ({now.BatteryRatio * 100f:F0}%)";
                }
                else if (Falling(now.BatteryRatio, _t.BatteryLowRatio, ref _lowArmed, ref _lowCount))
                {
                    reason = $"battery low ({now.BatteryRatio * 100f:F0}%)";
                    LastWakeWasBatteryLow = true;
                }
            }
            if (reason == null && Has(WakeCondition.Storm))
            {
                reason = CheckStorm(now);
            }
            if (reason == null && Has(WakeCondition.Danger))
            {
                reason = CheckDanger(now);
            }

            _lastBruteBurn = now.BruteBurn;
            _lastPressure = now.PressureKpa;
            return reason;
        }

        private bool Has(WakeCondition c) => (Selected & c) != 0;

        private bool InBand(float k) => k >= _t.SafeTempMinK && k <= _t.SafeTempMaxK;

        // >= threshold for N checks, but only after having been seen below it.
        private bool Rising(float value, float threshold, ref bool armed, ref int count)
        {
            if (value < threshold)
            {
                armed = true;
                count = 0;
                return false;
            }
            if (!armed)
            {
                return false;
            }
            return ++count >= _t.ConsecutiveChecks;
        }

        // <= threshold for N checks, but only after having been seen above it.
        private bool Falling(float value, float threshold, ref bool armed, ref int count)
        {
            if (value > threshold)
            {
                armed = true;
                count = 0;
                return false;
            }
            if (!armed)
            {
                return false;
            }
            return ++count >= _t.ConsecutiveChecks;
        }

        private string CheckStorm(WorldSnapshot now)
        {
            if (!now.Outdoors)
            {
                _stormCount = 0;
                return null;
            }
            bool changed = now.StormActive != _entryStorm || now.SolarStormActive != _entrySolar;
            if (!changed)
            {
                _stormCount = 0;
                return null;
            }
            if (++_stormCount < _t.ConsecutiveChecks)
            {
                return null;
            }
            string kind = (now.SolarStormActive || _entrySolar) ? "solar storm" : "storm";
            return now.StormActive ? kind + " started" : kind + " ended";
        }

        private string CheckDanger(WorldSnapshot now)
        {
            if (now.BruteBurn - _lastBruteBurn >= _t.DamageDelta)
            {
                return "damage taken";
            }
            if (Math.Abs(now.PressureKpa - _lastPressure) >= _t.PressureDeltaKpa)
            {
                return $"pressure change ({now.PressureKpa:F0} kPa)";
            }
            if (InBand(now.TemperatureK))
            {
                _tempArmed = true;
                _tempCount = 0;
                return null;
            }
            if (_tempArmed && ++_tempCount >= _t.ConsecutiveChecks)
            {
                return $"temperature ({now.TemperatureK - 273.15f:F0} C)";
            }
            return null;
        }
    }
}
