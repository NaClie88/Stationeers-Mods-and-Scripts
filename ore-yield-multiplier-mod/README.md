# Salty's Yield Multiplier

A Stationeers mod with configurable multipliers for **smelting yield** (ingots from a Furnace, Advanced Furnace or Arc Furnace) and **ice-melt gas yield**. Both are adjustable live from StationeersLaunchPad's config UI, with no restart needed.

The player-facing description is in [`WORKSHOP_DESCRIPTION.md`](WORKSHOP_DESCRIPTION.md). This README is the developer view.

## What it does

- **Ingot Yield Multiplier** (default 5×): more ingots from the same ore in the Furnace, Advanced Furnace and Arc Furnace.
  - **Re-melting ingots is 1:1.** Reagent that came from melted ingots isn't multiplied again, so you can't loop ingots for free material. Only ore-sourced reagent gets the multiplier. A mixed pool gets a blended rate.
  - The furnace tooltip's contents list shows the multiplied amounts, so it matches the "Produce N ingots" line.
- **Ice Gas Yield Multiplier** (default 5×): more gas from melting ice (Volatile Ice, Oxite, …), in the world or in a furnace. It also multiplies the trace gas that regular ores release when smelted.
  - **Condensed ice (PureIce) round-trips to zero net change:** freezing records the ice divided by the multiplier, and melting multiplies it back. The freeze and melt patches are applied together or not at all, so a half-applied pair can't destroy or duplicate material.
- **Doesn't touch** mining yield, world difficulty or the Centrifuge. Many Workshop yield mods add a custom Difficulty tier, which breaks Droid and Zrilian respawn kits and is baked into the save; see `WORKSHOP_DESCRIPTION.md`.

## Requirements and install

- [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad) v1.0.0 or later. **Required**: the multipliers are bound in LaunchPad's `OnLoaded(ConfigFile)` callback.
- Put the mod folder (`About/About.xml` + `SaltysYieldMultiplier.dll`) in `Documents/My Games/Stationeers/mods/`. **Not** in `BepInEx/plugins/`: LaunchPad never sees mods there, so `OnLoaded` never runs (see `UpdateNotes.md`). Developers: `build-and-install.sh` builds, deploys and hash-verifies.

## Config (LaunchPad config UI)

| Section | Key | Default | Range | Meaning |
|---|---|---|---|---|
| Yield | `IngotYieldMultiplier` | 5 | 0.1 to 20 | Ingots per vanilla ingot's worth of ore |
| Yield | `IceGasYieldMultiplier` | 5 | 0.1 to 20 | Gas per vanilla ice melt (and the PureIce freeze/melt factor) |
| Debug | `VerboseLogging` | off | | Per-smelt and per-melt before/after quantities in `BepInEx/LogOutput.log` |

Values are **snapped** before the game sees them: whole numbers at or above 1, tenths below 1. The LaunchPad slider is continuous, and the snap guarantees no odd fractional multipliers.

## How it works

Every patch is applied on its own (`PatchSafely`), so a game update breaking one can't take down the others. The exception is the PureIce pair, which is applied and rolled back together.

| File | Harmony patch | Purpose |
|---|---|---|
| `IngotYieldPatch.cs` | postfix `IQuantityRecipeComparable.GetOutputScale` | Ingot scale for Furnace and Advanced Furnace. This comparable is used only by the furnace family, so the Centrifuge is unaffected. |
| `ArcFurnaceYieldPatch.cs` | prefix `ArcFurnace.CreateOutput` | The Arc Furnace has no scale hook, so its quantity is scaled directly and re-clamped to the max stack. |
| `IceMeltYieldPatch.cs` | prefix + postfix `Ore.Smelt` | Temporarily scales `SpawnContents` gas around the melt, then restores it. |
| `IngotRemeltPatches.cs` + `IngotReagentTracker.cs` | `Consumable.Smelt`, `FurnaceBase.CreateIngots`, `ArcFurnace.DropIngots`, `GetSmelterScale` | Track the ingot-sourced share of each furnace's reagent pool and blend the output scale, so re-melted ingots come out 1:1. |
| `FurnaceTooltipPatch.cs` | postfix `FurnaceBase.GetPassiveTooltip` | Display only: shows the pool scaled to what will actually come out. |
| `PureIceFreezeYieldPatch.cs` / `PureIceMeltYieldPatch.cs` | prefix `AtmosphereHelper.CreateIcePrefab` / `PureIce.Smelt` | The matched freeze ÷ / melt × pair. |

Further reading:
- [`UpdateNotes.md`](UpdateNotes.md): design decisions and troubleshooting per patch
- [`TEST_PLAN.md`](TEST_PLAN.md): in-game test procedures
- [`REFERENCE_TABLES.md`](REFERENCE_TABLES.md): vanilla yields
- [`INVESTIGATE_LATER.md`](INVESTIGATE_LATER.md), [`BUG_REPORT_DRAFT_pure_ice_stack_clamp.md`](BUG_REPORT_DRAFT_pure_ice_stack_clamp.md): open questions

## Sources and credits

- Game behaviour was established by decompiling Stationeers' `Assembly-CSharp.dll` with [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`), and by decompiling `StationeersLaunchPad.dll` for its config-UI and mod-discovery behaviour.
- Built on [BepInEx](https://github.com/BepInEx/BepInEx), [Harmony](https://github.com/pardeike/Harmony) and [StationeersLaunchPad / LaunchPadBooster](https://github.com/StationeersLaunchPad/LaunchPadBooster).
- Vanilla gas constants were cross-checked against the [Stationeers Community Wiki](https://stationeers-wiki.com/) (Ice (Oxite), Ice (Volatiles), Furnace pages).
- Built in collaboration with Claude (Anthropic); see the AI disclosure in `WORKSHOP_DESCRIPTION.md`.
- History: first published as the private repo `NaClie88/saltys-yield-multiplier`, which only has the initial version. Development continues here on the `yield-multiplier-mod` branch.

**Author:** NaClie88 (Salty).
