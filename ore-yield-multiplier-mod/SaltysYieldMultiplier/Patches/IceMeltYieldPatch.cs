using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysYieldMultiplier.Patches
{
    // Ice melting (Volatile Ice, Oxite, etc. -- both in-world and inside a Furnace) all goes
    // through this single inherited Ore.Smelt method, which reads its per-unit gas composition
    // from SpawnContents. That field is also used by GasCanister/FuelIngot/MineableDeposit, but
    // none of those call Ore.Smelt, so scaling SpawnContents only around this call (and
    // restoring it immediately after) can't leak into their unrelated usages.
    //
    // Regular solid ores (Iron, Gold, etc.) are NOT a no-op here -- confirmed in testing that
    // ItemIronOre/ItemGoldOre have a non-empty SpawnContents too (trace pollutant/byproduct gas
    // released on smelt), so this patch multiplies that trace release alongside ice-melt gas.
    // That's in scope for what IceGasYieldMultiplier controls, not a bug, but it means the
    // config's name undersells what it actually does -- see UpdateNotes.md.
    [HarmonyPatch(typeof(Ore), nameof(Ore.Smelt))]
    public static class IceMeltYieldPatch
    {
        public static void Prefix(Ore __instance, out float[] __state)
        {
            var contents = __instance.SpawnContents;
            __state = new float[contents.Count];
            float multiplier = SaltysYieldMultiplier.IceMultiplier;
            for (int i = 0; i < contents.Count; i++)
            {
                __state[i] = contents[i].Quantity;
                contents[i].Quantity *= multiplier;
                SaltysYieldMultiplier.LogVerbose(
                    $"IceMeltYieldPatch fired: {__instance.name}, gas={contents[i].Type}, " +
                    $"before={__state[i]}mol, after={contents[i].Quantity}mol (x{multiplier})");
            }
        }

        public static void Postfix(Ore __instance, float[] __state)
        {
            var contents = __instance.SpawnContents;
            for (int i = 0; i < contents.Count; i++)
            {
                contents[i].Quantity = __state[i];
            }
        }
    }
}
