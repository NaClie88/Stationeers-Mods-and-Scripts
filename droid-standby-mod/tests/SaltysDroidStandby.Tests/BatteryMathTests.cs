using System.Collections.Generic;
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class BatteryMathTests
    {
        private static KeyValuePair<float, float> Cell(float stored, float max) =>
            new KeyValuePair<float, float>(stored, max);

        [Fact]
        public void SingleCell_isItsRatio()
        {
            Assert.Equal(0.25f, BatteryMath.TotalRatio(new[] { Cell(250f, 1000f) }), 3);
        }

        [Fact]
        public void TwoCells_areSummedByEnergy_notAveragedByRatio()
        {
            // 0 of 1000 + 3000 of 3000 = 3000 / 4000
            Assert.Equal(0.75f, BatteryMath.TotalRatio(new[] { Cell(0f, 1000f), Cell(3000f, 3000f) }), 3);
        }

        [Fact]
        public void NoBatteries_returnsZero()
        {
            Assert.Equal(0f, BatteryMath.TotalRatio(new KeyValuePair<float, float>[0]));
        }

        [Fact]
        public void ZeroCapacityCell_isIgnored()
        {
            Assert.Equal(0.5f, BatteryMath.TotalRatio(new[] { Cell(5f, 0f), Cell(50f, 100f) }), 3);
        }

        [Fact]
        public void Result_isClampedToOne()
        {
            Assert.Equal(1f, BatteryMath.TotalRatio(new[] { Cell(120f, 100f) }));
        }
    }
}
