using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class KeyActionsTests
    {
        [Theory]
        [InlineData(StandbyLevel.Normal, Gesture.SingleTap, KeyAction.EnterPowerSave)]
        [InlineData(StandbyLevel.Normal, Gesture.DoubleTap, KeyAction.EnterStandby)]
        [InlineData(StandbyLevel.Normal, Gesture.LongPress, KeyAction.OpenMenu)]
        [InlineData(StandbyLevel.PowerSave, Gesture.SingleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.PowerSave, Gesture.DoubleTap, KeyAction.EnterStandby)]
        [InlineData(StandbyLevel.PowerSave, Gesture.LongPress, KeyAction.OpenMenu)]
        [InlineData(StandbyLevel.Standby, Gesture.SingleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.Standby, Gesture.DoubleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.Standby, Gesture.LongPress, KeyAction.OpenMenu)]
        [InlineData(StandbyLevel.DeepStandby, Gesture.SingleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.DeepStandby, Gesture.DoubleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.DeepStandby, Gesture.LongPress, KeyAction.Wake)]
        [InlineData(StandbyLevel.Normal, Gesture.None, KeyAction.None)]
        [InlineData(StandbyLevel.DeepStandby, Gesture.None, KeyAction.None)]
        public void Table_matchesRevision2(StandbyLevel level, Gesture g, KeyAction expected)
        {
            Assert.Equal(expected, KeyActions.Decide(level, g));
        }

        [Theory]
        [InlineData(StandbyLevel.Normal, false)]
        [InlineData(StandbyLevel.PowerSave, false)]
        [InlineData(StandbyLevel.Standby, true)]
        [InlineData(StandbyLevel.DeepStandby, true)]
        public void ImmediateTap_onlyWhereATapCanOnlyMeanWake(StandbyLevel level, bool expected)
        {
            Assert.Equal(expected, KeyActions.WantsImmediateTap(level));
        }
    }
}
