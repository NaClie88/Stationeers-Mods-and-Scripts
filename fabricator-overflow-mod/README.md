# Salty's Fabricator Overflow

A Stationeers mod that **caps how many of each ingot type a printer will hold**. Anything over the cap is ejected as normal ingot stacks a few seconds after the last ingot goes in.

## What it does

- Applies to the printer-type fabricators: Autolathe, Pipe Bender, Electronics Printer, Tool Printer and the others built on the game's `SimpleFabricatorBase`.
- **Per-ingot-type cap**, default **500**. A printer holding 800 iron and 200 copper ejects 300 iron and leaves the copper alone.
- **Delayed, batched eject.** The countdown restarts on every insert, so you can keep topping up. Once you stop for `EjectDelaySeconds` (default 5 s), the overflow comes out together.
- **Real stacks through the machine's own export.** Overflow is placed in the printer's export slot, so it follows an attached export chute or drops in front of the machine like any printed item. Each stack is at most 500, the game's ingot stack limit, one stack per export cycle.
- **Never makes slag.** Only reagents that have an ingot prefab are capped. Anything without an ingot is left in the machine instead of turned into a reagent mix.

## Why

Printers don't store ingots in slots. Every imported stack is poured into one reagent pool with no upper bound (1 ingot = 1.0 reagent), so a chute feeding a printer can bury thousands of ingots inside it. Vanilla's 500 limit only appears on the way back *out*. This mod enforces a cap on the pool itself.

## Requirements and install

- [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad) v1.0.0 or later.
- Put the mod folder (`About/About.xml` + `SaltysFabricatorOverflow.dll`) in `Documents/My Games/Stationeers/mods/`. Developers: `build-and-install.sh` builds, deploys and hash-verifies, and refuses to run while the game is open.
- **Multiplayer:** the eject runs on the server only (`GameManager.RunSimulation`), and the ejected ingots are ordinary items the game syncs itself. Clients don't strictly need the mod. *Not tested in multiplayer.*

## Config (LaunchPad config UI)

| Section | Key | Default | Range | Meaning |
|---|---|---|---|---|
| Overflow | `MaxPerIngotType` | 500 | 50 to 5000 | Most of any one ingot type a printer holds |
| Overflow | `EjectDelaySeconds` | 5 | 0 to 60 | Quiet time after the last insert before ejecting |
| Debug | `VerboseLogging` | off | | Log every eject (machine, ingot, amount) to `BepInEx/LogOutput.log` |

## How it works

`Patches/FabricatorOverflowPatch.cs`:

| Harmony patch | Purpose |
|---|---|
| postfix `FabricatorBase.CollectResource` | Timestamp every ingot insert. This is the single entry point for hand-placed and chute imports (`OnImportClosingComplete` → `CollectResource`). Timestamps live in a `ConditionalWeakTable`, so a deconstructed printer leaves nothing behind. |
| postfix `SimpleFabricatorBase.OnServerExportTick` | Runs after vanilla's own export tick, so vanilla's print output always gets the export slot first. When the printer is idle, closed, powered and its export slot is empty, and the delay has passed, it takes up to 500 of one over-cap reagent and builds the ingot the same way vanilla's `Thing.DropReagent` does, but with a chosen quantity instead of a hardcoded 500. |

The ingot prefab for each reagent is found with the game's own recipe lookup (`Ingot.RecipeComparable.Recipes`), run on a copy of the pool, and cached.

## Known limitations

- Don't set `MaxPerIngotType` below what a recipe needs of a single ingot type: the printer would eject down to the cap and could never hold enough to print that recipe. The default, 500, is above any single-ingot requirement in vanilla printers.

## Sources and credits

- Game behaviour was established by decompiling Stationeers' `Assembly-CSharp.dll` with [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`). Key types: `FabricatorBase`, `SimpleFabricatorBase`, `ReagentMixture`, `Reagent`, `Ingot`, `Thing.DropReagent`.
- Built on [BepInEx](https://github.com/BepInEx/BepInEx), [Harmony](https://github.com/pardeike/Harmony) and [StationeersLaunchPad / LaunchPadBooster](https://github.com/StationeersLaunchPad/LaunchPadBooster) (config via `OnLoaded(ConfigFile)`).
- Project skeleton shared with this repo's *Salty's Yield Multiplier*.
- Built in collaboration with Claude (Anthropic).

**Author:** NaClie88 (Salty).
