using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysYieldMultiplier.Patches
{
    // PureIce (condensed-atmosphere ice, from a freeze event -- see PureIceFreezeYieldPatch)
    // overrides Smelt() with its own separate implementation rather than inheriting
    // Ore.Smelt(), so IceMeltYieldPatch (which targets Ore.Smelt specifically) never fires for
    // it -- C# virtual dispatch sends a PureIce instance's .Smelt() straight to this override.
    // This patch exists specifically to pair with PureIceFreezeYieldPatch: freezing now records
    // a nominal quantity divided by the multiplier, so melting must multiply it back for the
    // freeze/melt cycle to net out to the original amount instead of destroying material.
    // Same Prefix/Postfix scale-then-restore shape as IceMeltYieldPatch, just retargeted.
    [HarmonyPatch(typeof(PureIce), nameof(PureIce.Smelt))]
    public static class PureIceMeltYieldPatch
    {
        public static void Prefix(PureIce __instance, out float[] __state)
        {
            var contents = __instance.SpawnContents;
            __state = new float[contents.Count];
            float multiplier = SaltysYieldMultiplier.IceMultiplier;
            for (int i = 0; i < contents.Count; i++)
            {
                __state[i] = contents[i].Quantity;
                contents[i].Quantity *= multiplier;
                SaltysYieldMultiplier.LogVerbose(
                    $"PureIceMeltYieldPatch fired: gas={contents[i].Type}, " +
                    $"before={__state[i]}mol, after={contents[i].Quantity}mol (x{multiplier})");
            }
        }

        // A Finalizer, not a Postfix (FIXED 2026-10-06, bug hunt): a Postfix is skipped when
        // Smelt throws, which would leave SpawnContents multiplied, so the next melt would
        // multiply again and compound. A Finalizer runs either way.
        public static void Finalizer(PureIce __instance, float[] __state)
        {
            if (__state == null) return; // Prefix didn't get as far as saving anything
            var contents = __instance.SpawnContents;
            for (int i = 0; i < contents.Count && i < __state.Length; i++)
            {
                contents[i].Quantity = __state[i];
            }
        }
    }
}
