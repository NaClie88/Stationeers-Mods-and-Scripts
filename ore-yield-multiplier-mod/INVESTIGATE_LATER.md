# Investigate Later

Open questions and possible extensions that are not being worked on now.

## Solid Fuel Generator exhaust gas

**Status:** parked (2026-09-26). Coal in the generator is expected to already be
covered; verifying that in-game is optional. Nothing below is implemented.

### Goal

Gas released into the atmosphere when the Solid Fuel Generator burns fuel should
be multiplied by the yield multiplier, like ice melting is.

### What the decompile shows

`SolidFuelGenerator.SmeltResource()` calls `ImportingThing.Smelt(localAtmosphere,
_temporaryReagentMixture)` on whatever item is in its import slot. What that does
depends on the item's class:

| Fuel class | Smelt override | Gas source | Covered today? |
|---|---|---|---|
| `Ore` (coal, `ItemCoalOre`) | `Ore.Smelt` | `SpawnContents` | Yes -- `IceMeltYieldPatch` patches `Ore.Smelt`, scaled by `IceGasYieldMultiplier` |
| `FuelIngot` (solid fuel ingots) | `FuelIngot.Smelt` | `GasMixture`, built once in `Start()` from `SpawnContents` | **No** -- the patch never touches it |
| `GasCanister` | `GasCanister.Smelt` | `InternalAtmosphere.GasMixture` | No, but the generator is unlikely to accept canisters |

### Open items

1. **Confirm coal in the generator.** The coal numbers in `REFERENCE_TABLES.md`
   (CO2 10 mol, Pollutant 3 mol) came from the mod log, not necessarily from a
   generator burn. Test: enable `VerboseLogging`, burn coal in the generator, and
   check that `IceMeltYieldPatch fired: ... gas=CarbonDioxide` shows the
   multiplied moles.
2. **`FuelIngot` gap.** Possible fix: a Harmony patch on `FuelIngot.Smelt` that
   scales `GasMixture` for the call and restores it afterward, following the
   before/after pattern in `IceMeltYieldPatch`. The `GasMixture` API needs
   checking first (scaling in place vs. swapping in a scaled clone). Also check
   which `FuelIngot` prefabs the generator's `Resources` list accepts.
3. **Config choice for (2).** Reuse `IceGasYieldMultiplier` (one knob, no new
   setting) or add a separate `FuelGasYieldMultiplier`. Leaning toward reuse.
4. **Scope of "burn other fuels".** Confirm which non-coal fuels are worth
   covering before writing any patch.

## Ingot melt exemption (implemented 2026-09-26, untested in-game)

Ore is multiplied by `IngotYieldMultiplier`; a melted-in ingot is 1:1. See
`Patches/IngotReagentTracker.cs` and `Patches/IngotRemeltPatches.cs`. Known gaps:

1. **Ingot -> de-gassed ore -> ingot.** If an ingot melts in a furnace too cool for
   the ingot recipe, its reagent leaves as ore (1 per unit): a normal-looking
   de-gassed ore if the mix matches a Centrifuge recipe, otherwise dirty ore (the
   game's `SlagPrefab` fallback). Re-melting that ore hot enough counts as ore and
   gets multiplied, so the ingot ends up multiplied after all. Fix would need that
   ore to carry an "ingot-sourced" tag.
2. **Not saved.** The ingot-sourced share lives in memory only. Reloading a save
   mid-batch treats that batch's ingot reagent as ore, once.
3. **Multiplayer clients.** Tooltips ("Produce N ingots") are computed client-side,
   where the tracker is empty, so a client may still see the ×N preview for ingots.
   Actual output is decided by the host.
4. **Even-mix assumption.** After a partial batch, the ingot share is reduced in
   proportion to the pool. Fine for one metal, approximate for mixed alloys.

In-game checks: ore ×N; one nickel ingot in -> exactly 1 out; mixed ore + ingot
batch; Arc Furnace with ingots; warm-furnace output (de-gassed/dirty ore) stays 1:1 with no extra gas.
