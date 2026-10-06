using Assets.Scripts.Atmospherics;
using HarmonyLib;
using UnityEngine;

namespace SaltysYieldMultiplier.Patches
{
    // AtmosphereHelper.CreateIcePrefab is the actual freeze event (environmental gas/liquid
    // solidifying into a PureIce item -- confirmed this can't happen inside pipes; a pipe
    // that would freeze bursts instead, and the freeze happens to the released atmosphere).
    // It records the exact frozen mole quantity onto the new PureIce's SpawnContents via
    // PureIce.AssignSpawnGasValues. That method is ALSO called from PureIce.OnPrefabLoad with
    // a hardcoded default quantity (template/preview setup, not a real freeze event) -- patching
    // AssignSpawnGasValues directly would incorrectly touch that too, so this patches the
    // narrower, private CreateIcePrefab instead, which is only ever called from a real freeze.
    //
    // Dividing the recorded quantity here (while the actual liquid consumed from the world
    // atmosphere is unchanged -- that removal already happened earlier in
    // AtmosphereHelper.FreezeWorldAtmosphere, before this method runs) means it now takes
    // IceGasYieldMultiplier times more liquid to freeze into the same nominal ice quantity.
    // Combined with PureIceMeltYieldPatch (which multiplies that quantity back on melt), the
    // freeze/melt cycle nets out to exactly the original amount -- no gain, no loss. Without
    // both patches together, this would either duplicate material or destroy it.
    [HarmonyPatch(typeof(AtmosphereHelper), "CreateIcePrefab")]
    public static class PureIceFreezeYieldPatch
    {
        public static void Prefix(ref Mole mole)
        {
            float multiplier = SaltysYieldMultiplier.IceMultiplier;
            if (multiplier > 0f)
            {
                float scaled = mole.Quantity.ToFloat() / multiplier;
                SaltysYieldMultiplier.LogVerbose(
                    $"PureIceFreezeYieldPatch fired: gas={mole.Type}, liquidConsumed={mole.Quantity.ToFloat()}mol, " +
                    $"nominalIceRecorded={scaled}mol (/{multiplier})");

                // Always-on, not verbose-gated: mirrors PureIce.AssignSpawnGasValues' own
                // piece-count math (CeilToInt(quantity/50)) so the suspected vanilla 50-piece
                // stack clamp (see UpdateNotes.md "Investigated edge case") gets caught
                // naturally during normal play/testing, rather than needing a dedicated
                // boundary-probing session. Should be rare enough that this essentially never
                // fires under default settings -- if it ever does, it's worth investigating
                // regardless of the verbose-logging setting.
                int wouldBePieces = Mathf.CeilToInt(scaled / 50f);
                if (wouldBePieces > 50)
                {
                    SaltysYieldMultiplier.Log(
                        $"WARNING: PureIce freeze of {scaled}mol {mole.Type} would need " +
                        $"{wouldBePieces} pieces, exceeding the 50-piece stack cap -- likely to " +
                        "trigger the suspected vanilla clamp-loss bug. Not caused by this mod's " +
                        "multiplier (which only ever shrinks this number) -- see UpdateNotes.md " +
                        "'Investigated edge case' section.");
                }

                mole.Quantity = new MoleQuantity(scaled);
            }
        }
    }
}
