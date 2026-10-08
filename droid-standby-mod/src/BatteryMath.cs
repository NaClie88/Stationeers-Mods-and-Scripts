using System.Collections.Generic;

namespace SaltysDroidStandby
{
    // Spec §4.4: "battery" = energy summed over every battery-type slot (works with or
    // without the Dual Battery mod's second slot, no reference to it).
    public static class BatteryMath
    {
        // Final-review C2: BatteryCell.PowerStored is not networked -- a multiplayer client
        // only receives CurrentPowerPercentage (0..100). Clients build the cell from that.
        public static KeyValuePair<float, float> Cell(float powerStored, float powerMaximum,
                                                      byte currentPowerPercentage, bool useSyncedPercentage)
        {
            float stored = useSyncedPercentage ? currentPowerPercentage / 100f * powerMaximum : powerStored;
            return new KeyValuePair<float, float>(stored, powerMaximum);
        }

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
    
        // Light cost as a share of the droid's own drain: vanilla's 105 vs 100 per tick.
        public const float LightShare = 0.05f;

        // Amount to ADD back to the droid battery after vanilla's life-tick drain of `spent`
        // (the body drain, with the vanilla light bug neutralised). The level factor scales the
        // body; a lit helmet light costs LightShare of the body drain at full rate in every
        // level. Negative = charge extra. No drain this tick (bed, charger) = no change.
        public static float Adjustment(float spent, float factor, bool lightOn)
        {
            if (spent <= 0f) return 0f;
            float refund = spent * (1f - factor);
            return lightOn ? refund - spent * LightShare : refund;
        }
    }
}
