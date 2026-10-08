using System.Collections.Generic;

namespace SaltysDroidStandby
{
    // Spec §4.4: "battery" = energy summed over every battery-type slot (works with or
    // without the Dual Battery mod's second slot, no reference to it).
    public static class BatteryMath
    {
        public static float TotalRatio(IEnumerable<KeyValuePair<float, float>> cells)
        {
            float stored = 0f;
            float max = 0f;
            foreach (var cell in cells)
            {
                if (cell.Value <= 0f)
                {
                    continue;
                }
                stored += cell.Key < 0f ? 0f : cell.Key;
                max += cell.Value;
            }
            if (max <= 0f)
            {
                return 0f;
            }
            float ratio = stored / max;
            return ratio > 1f ? 1f : ratio;
        }
    }
}
