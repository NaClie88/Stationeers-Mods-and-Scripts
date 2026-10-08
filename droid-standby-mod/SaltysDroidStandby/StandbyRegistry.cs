using System.Runtime.CompilerServices;
using Assets.Scripts.Objects.Entities;

namespace SaltysDroidStandby
{
    // Per-Human standby level. On the server this is the truth that drives the stun floor
    // and drain refund; on a client it mirrors the local player's own level (jump, UI, wake
    // checks). Weak keys: entries die with the Human. Never saved -- load = Normal (spec §11).
    public static class StandbyRegistry
    {
        private static readonly ConditionalWeakTable<Human, StrongBox<StandbyLevel>> Levels =
            new ConditionalWeakTable<Human, StrongBox<StandbyLevel>>();

        public static StandbyLevel Get(Human h)
        {
            return h != null && Levels.TryGetValue(h, out var box) ? box.Value : StandbyLevel.Normal;
        }

        public static void Set(Human h, StandbyLevel level)
        {
            if (h == null) return;
            var box = Levels.GetOrCreateValue(h);
            if (box.Value == level) return;
            SaltysDroidStandby.LogVerbose("Standby " + h.name + ": " + box.Value + " -> " + level);
            box.Value = level;
        }
    }
}
