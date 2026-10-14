# Salty's Ceiling Solar

A Stationeers mod that adds **Solar Panel (Clinging)**, a third style of the Kit (Solar Panel Basic) next to Flat and Angled. It mounts on the inside of a wall or ceiling with its **cells facing the surface it clings to**. Put it against a glass wall or under a glass roof and it collects sunlight from outside while sitting in a sealed room, out of the storms.

## What it does

- **New build style, vanilla styles untouched.** Cycle the basic solar kit's styles while placing and the clinging panel is the third entry. The Flat and Angled panels keep their exact vanilla placements and rotation options.
- **Cells face the mount.** It is the flat panel turned upside down against its mount, so on a wall the cells look out through the wall and on a ceiling they look up through the roof, with the frame sticking out into the room.
- **Same cost, same kit back.** It is built and deconstructed with the basic solar kit, like the other two styles.
- **No change to the power maths.** The game already measures sunlight along the cells' own facing and casts its shadow rays from the cells, so the turned cells compute correctly as they are.

## Things to know

- **Placement** is the flat panel's own (floor, wall and ceiling). On a floor the clinging panel faces the ground, so it's only useful on walls and ceilings.
- **Storms:** a panel inside a pressurised room takes no storm damage (vanilla behaviour), but a storm still lowers its output like any other panel.
- **Saves:** clinging panels are saved by name. Load that save without the mod and they're skipped with a warning; nothing else breaks.
- **Multiplayer:** required on server and clients. The kit style is sent as a position in the kit's build list, and the panel spawns by prefab hash. *Not tested in multiplayer.*

## Requirements and install

- [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad) v1.0.0 or later.
- Put the mod folder (`About/About.xml` + `SaltysCeilingSolar.dll`) in `Documents/My Games/Stationeers/mods/`. Developers: `build-and-install.sh` builds, deploys and hash-verifies, and refuses to run while the game is open. A new mod folder must also be added to the active LaunchPad profile (see the repo's `CLAUDE.md`).

## Config (LaunchPad config UI)

| Section | Key | Default | Meaning |
|---|---|---|---|
| Debug | `VerboseLogging` | on (while testing) | When a clinging panel faces the sun but makes no power, log what blocks it to `BepInEx/LogOutput.log` |

## How it works

| Harmony patch | Purpose |
|---|---|
| prefix `Prefab.LoadAll` | Copies `StructureSolarPanelFlat` as `StructureSolarPanelClinging`, works out a local flip (half a turn plus a shift along the mount normal), applies it to the copy's children and root collider, moves the root's frame renderer onto a flipped child pivot, adds the copy to `WorldManager.SourcePrefabs` and appends it to `ItemKitSolarPanelBasic`'s build list. The game then registers it and builds its placement cursor like any vanilla prefab. Logs the placement, deconstruct tool, flip, sunlight-blocking layers and layout once. |
| postfix `Structure.GetBatchMatrix` | The same, for meson scanner overlays. |
| postfix `InventoryManager.HandleStructurePrefab` | Flips the clinging cursor's blueprint ghost so the placement preview matches. |
| postfix `Assets.Scripts.Localization.GetThingName` / `GetThingDescription` | Gives the new prefab its name and description in every language. |
| postfix `SolarPanel.CalculateSolarEfficiency` | `VerboseLogging` only: explains a clinging panel that faces the sun but makes nothing. |

## Sources and credits

- Game behaviour was established by decompiling Stationeers' `Assembly-CSharp.dll` (Update 0.2.6428.27798) and `LaunchPadBooster.dll` with [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`). Key types: `SolarPanel`, `SolarPanelArm`, `Structure`, `Prefab`, `WorldManager`, `MultiConstructor`, `InventoryManager`, `Localization`.
- Built on [BepInEx](https://github.com/BepInEx/BepInEx), [Harmony](https://github.com/pardeike/Harmony) and [StationeersLaunchPad / LaunchPadBooster](https://github.com/StationeersLaunchPad/LaunchPadBooster).
- Project skeleton shared with this repo's *Salty's Fabricator Overflow*.
- Built in collaboration with Claude (Anthropic).

**Author:** NaClie88 (Salty).
