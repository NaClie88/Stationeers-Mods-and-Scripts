using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects.Entities;

namespace SaltysDroidStandby
{
    // Monotonic seconds, safe on the life-tick worker thread (Unity's Time is main-thread only).
    public static class StandbyClock
    {
        private static readonly Stopwatch Watch = Stopwatch.StartNew();
        public static float Now => (float)Watch.Elapsed.TotalSeconds;
    }

    // Per-Human standby level, plus when it changed and the drain factor in effect at that
    // moment, for the Revision 3 drain ramp. On the server this is the truth that drives the
    // stun floor and drain; on a client it mirrors the local player's own level (UI, patches,
    // wake checks). Weak keys: entries die with the Human. Never saved -- load = Normal.
    public static class StandbyRegistry
    {
        private sealed class Entry
        {
            public StandbyLevel Level;
            public float FromFactor = 1f;
            public float ChangedAt = float.NegativeInfinity;
        }

        private static readonly ConditionalWeakTable<Human, Entry> Entries = new ConditionalWeakTable<Human, Entry>();

        public static StandbyLevel Get(Human h)
        {
            return h != null && Entries.TryGetValue(h, out var e) ? e.Level : StandbyLevel.Normal;
        }

        public static void Set(Human h, StandbyLevel level)
        {
            if (h == null) return;
            Entry e = Entries.GetOrCreateValue(h);
            StandbyLevel previous;
            lock (e)
            {
                if (e.Level == level) return;
                float now = StandbyClock.Now;
                e.FromFactor = Effective(e, now); // a change mid-ramp starts from where it was
                previous = e.Level;
                e.Level = level;
                e.ChangedAt = now;
            }
            // ReferenceId, not h.name: Set can run on the life-tick worker thread, where Unity
            // object APIs throw (final-review C1). Message built only when verbose is on.
            if (StandbyConfig.IsVerbose)
            {
                SaltysDroidStandby.Log("Standby #" + h.ReferenceId + ": " + previous + " -> " + level);
            }
        }

        // Drain multiplier right now, with the Revision 3 ramp applied (worker-thread safe).
        public static float DrainFactor(Human h, float now)
        {
            if (h == null || !Entries.TryGetValue(h, out var e)) return 1f;
            lock (e) return Effective(e, now);
        }

        // Seconds of "powering down" left (0 when not easing down), for the status line.
        public static float RampSecondsLeft(Human h, float now)
        {
            if (h == null || !Entries.TryGetValue(h, out var e)) return 0f;
            lock (e)
            {
                return DrainRamp.SecondsLeft(e.FromFactor, StandbyConfig.DrainFactor(e.Level), now - e.ChangedAt, StandbyConfig.RampSeconds);
            }
        }

        private static float Effective(Entry e, float now) =>
            DrainRamp.Factor(e.FromFactor, StandbyConfig.DrainFactor(e.Level), now - e.ChangedAt, StandbyConfig.RampSeconds);
    }
}
