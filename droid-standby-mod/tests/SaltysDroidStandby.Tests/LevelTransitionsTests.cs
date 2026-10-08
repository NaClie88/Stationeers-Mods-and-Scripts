using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class LevelTransitionsTests
    {
        [Theory]
        [InlineData(StandbyLevel.Normal, Gesture.Tap, StandbyLevel.PowerSave)]
        [InlineData(StandbyLevel.Normal, Gesture.LongPress, StandbyLevel.Deep)]
        [InlineData(StandbyLevel.PowerSave, Gesture.Tap, StandbyLevel.Normal)]
        [InlineData(StandbyLevel.PowerSave, Gesture.LongPress, StandbyLevel.Deep)]
        [InlineData(StandbyLevel.Deep, Gesture.Tap, StandbyLevel.Normal)]
        [InlineData(StandbyLevel.Deep, Gesture.LongPress, StandbyLevel.Normal)]
        [InlineData(StandbyLevel.PowerSave, Gesture.None, StandbyLevel.PowerSave)]
        [InlineData(StandbyLevel.Deep, Gesture.None, StandbyLevel.Deep)]
        public void Table_matchesSpec(StandbyLevel from, Gesture g, StandbyLevel expected)
        {
            Assert.Equal(expected, LevelTransitions.Next(from, g));
        }
    }
}
