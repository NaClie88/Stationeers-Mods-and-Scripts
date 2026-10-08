using System.Collections.Generic;

namespace SaltysDroidStandby
{
    // Spec §9. `suppressedUntilInput` is set after any automatic wake (e.g. "battery low")
    // and cleared on real player input, so an AFK droid isn't bounced in and out of standby.
    public static class SafetyNetLogic
    {
        public static bool ShouldTrigger(StandbyLevel level, float batteryRatio, float idleSeconds,
                                         float batteryThreshold, float idleThresholdSeconds,
                                         bool suppressedUntilInput)
        {
            return level != StandbyLevel.Deep
                && !suppressedUntilInput
                && batteryRatio <= batteryThreshold
                && idleSeconds >= idleThresholdSeconds;
        }

        // Spec §9 "effectively solo": true single-player, or a multiplayer host whose only
        // connection is itself (an empty client list counts). A client is never solo here --
        // a lone player on a dedicated server is phase 3's job.
        public static bool IsEffectivelySolo(bool networkActive, bool isServer, IEnumerable<bool> connectedClientIsHost)
        {
            if (!networkActive) return true;
            if (!isServer) return false;
            foreach (bool isHost in connectedClientIsHost)
            {
                if (!isHost) return false;
            }
            return true;
        }
    }
}
