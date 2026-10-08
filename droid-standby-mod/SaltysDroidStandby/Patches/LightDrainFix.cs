using System.Reflection;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Fixes the vanilla helmet-light drain bug (see UpdateNotes): ToggleHelmetLight calls
    // SetPowerDrain(105) on a static shared by every human, even when switching the light OFF,
    // and only ToggleNightVision resets it. Blocking SetPowerDrain keeps the static at its base;
    // DrainPatch then charges each droid its own light share, only while its light is on.
    // Optional by design: if a game update removes or reworks these members, Available is
    // false, the fix stays off, and the mod carries on (user requirement).
    public static class LightDrainFix
    {
        private static readonly MethodInfo SetPowerDrain =
            AccessTools.Method(typeof(Human), "SetPowerDrain", new[] { typeof(float) });
        private static readonly FieldInfo DrainField =
            AccessTools.Field(typeof(Human), "PowerDrainedPerTick");

        public static bool Available => SetPowerDrain != null && SetPowerDrain.IsStatic
            && DrainField != null && DrainField.IsStatic && DrainField.FieldType == typeof(float);

        public static bool Active { get; private set; }

        public static void Apply(Harmony harmony)
        {
            if (!StandbyConfig.LightDrainFixEnabled) { SaltysDroidStandby.Log("Helmet-light drain fix: off (config)"); return; }
            if (!Available) { SaltysDroidStandby.Log("Helmet-light drain fix: skipped, the game's light drain code has changed (likely fixed upstream)"); return; }
            try
            {
                harmony.Patch(SetPowerDrain, prefix: new HarmonyMethod(typeof(LightDrainFix), nameof(BlockPrefix)));
                DrainField.SetValue(null, 100f); // clear a 105 left over from before the patch
                Active = true;
                SaltysDroidStandby.Log("Helmet-light drain fix: active");
            }
            catch (System.Exception e)
            {
                SaltysDroidStandby.LogError("Helmet-light drain fix failed, left off: " + e.Message);
            }
        }

        public static bool BlockPrefix() => false;

        // Worker thread (life tick): plain field/property reads only, ReferenceEquals, no Unity APIs.
        public static bool LightOn(Human human)
        {
            Slot slot = human.HelmetSlot;
            if (ReferenceEquals(slot, null)) return false;
            IWearableLight light = slot.Occupant as IWearableLight;
            if (ReferenceEquals(light, null) || !light.OnOff) return false;
            Thing thing = light.GetAsThing;
            return !ReferenceEquals(thing, null) && thing.Powered;
        }
    }
}
