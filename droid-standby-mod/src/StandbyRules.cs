namespace SaltysDroidStandby
{
    // One rule for "standby must not apply to this body right now" (final-review C3/I3/I5),
    // shared by the server floor patch, the client mirror reset and the safety-net gate.
    public static class StandbyRules
    {
        public static bool ShouldClear(bool isDroid, bool isAlive, bool isSleeping, bool inLifeSuspender,
                                       bool networkActive, bool ownerOnline)
        {
            return !isDroid
                || !isAlive                          // dead or unconscious: never hold the floor (I3)
                || isSleeping
                || inLifeSuspender                   // bed / cryo / Droid Sleeper: vanilla handles it
                || (networkActive && !ownerOnline);  // owner disconnected in multiplayer (I5)
        }
    }
}
