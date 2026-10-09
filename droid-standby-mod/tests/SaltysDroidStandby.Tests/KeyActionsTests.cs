using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class KeyActionsTests
    {
        // Revision 3 table. Waking goes to Power Save (the controller's job); every gesture in
        // Standby wakes, so holding the key there can't reopen the menu.
        [Theory]
        [InlineData(StandbyLevel.Normal, Gesture.Tap, KeyAction.EnterPowerSave)]
        [InlineData(StandbyLevel.Normal, Gesture.LongPress, KeyAction.OpenMenu)]
        [InlineData(StandbyLevel.PowerSave, Gesture.Tap, KeyAction.EnterNormal)]
        [InlineData(StandbyLevel.PowerSave, Gesture.LongPress, KeyAction.OpenMenu)]
        [InlineData(StandbyLevel.Standby, Gesture.Tap, KeyAction.Wake)]
        [InlineData(StandbyLevel.Standby, Gesture.LongPress, KeyAction.Wake)]
        [InlineData(StandbyLevel.Normal, Gesture.None, KeyAction.None)]
        [InlineData(StandbyLevel.Standby, Gesture.None, KeyAction.None)]
        public void Table_matchesRevision3(StandbyLevel level, Gesture g, KeyAction expected)
        {
            Assert.Equal(expected, KeyActions.Decide(level, g));
        }

        [Fact]
        public void WakeTarget_isPowerSave()
        {
            Assert.Equal(StandbyLevel.PowerSave, KeyActions.WakeTarget);
        }

        // Cooldown gates every state change by the key, waking included; opening the menu
        // changes nothing, so it is never gated.
        [Theory]
        [InlineData(KeyAction.EnterPowerSave, true)]
        [InlineData(KeyAction.EnterNormal, true)]
        [InlineData(KeyAction.Wake, true)]
        [InlineData(KeyAction.OpenMenu, false)]
        [InlineData(KeyAction.None, false)]
        public void GatedByCooldown(KeyAction a, bool expected)
        {
            Assert.Equal(expected, KeyActions.IsGatedByCooldown(a));
        }
    }
}
