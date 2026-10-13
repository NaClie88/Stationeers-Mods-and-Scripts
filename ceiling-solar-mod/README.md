# Salty's Ceiling Solar

A Stationeers mod that lets **solar panels be placed in any rotation**: upside down on a ceiling, sideways on a wall, or upright as usual. Put them against glass roofs and glass walls, inside a pressurised room where storms can't damage them.

## What it does

- Opens up the placement rotations of every solar panel variant (they all share the game's `SolarPanel` class). The usual rotate keys while placing now turn the panel on every axis.
- **No new model and no change to the power maths.** The game already measures sunlight along the direction the panel's cells face and casts its shadow rays from the cells themselves, so a flipped or sideways panel produces exactly what its real facing deserves.

## Things to know

- **Aiming is relative to the mount.** The panel's Horizontal and Vertical settings turn it relative to how it's mounted, and tilt is limited to ±75° from the mount. A panel hung from a ceiling has its cells facing down into the room by default; one on a wall faces out from the wall. IC10 or Solar Control setups written for floor panels won't aim a wall or ceiling panel correctly. A Daylight Sensor mounted the same way as the panel reports angles in the same frame.
- **Storms:** a panel inside a pressurised room takes no storm damage (vanilla behaviour), but a storm still reduces its output like any other panel.
- **Multiplayer:** required on server and clients (the construction cursor is client-side). *Not tested in multiplayer.*

## Requirements and install

- [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad) v1.0.0 or later.
- Put the mod folder (`About/About.xml` + `SaltysCeilingSolar.dll`) in `Documents/My Games/Stationeers/mods/`. Developers: `build-and-install.sh` builds, deploys and hash-verifies, and refuses to run while the game is open. A new mod folder must also be added to the active LaunchPad profile (see the repo's `CLAUDE.md`).

## How it works

`Patches/SolarPanelPatches.cs`:

| Harmony patch | Purpose |
|---|---|
| postfix `SolarPanel.Awake` | Sets `RotationAxis = All` and `AllowedRotations = All` on every solar panel instance, including the construction cursor, which is instantiated from the prefab. Logs each prefab once with its original values and the layers that block its sunlight rays. |

## Sources and credits

- Game behaviour was established by decompiling Stationeers' `Assembly-CSharp.dll` (Update 0.2.6428.27798) with [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`). Key types: `SolarPanel`, `SolarPanelArm`, `Structure` (`PlacementType`, `RotationAxis`, `AllowedRotations`), `InventoryManager` placement, `SolarRadiators`.
- Built on [BepInEx](https://github.com/BepInEx/BepInEx), [Harmony](https://github.com/pardeike/Harmony) and [StationeersLaunchPad / LaunchPadBooster](https://github.com/StationeersLaunchPad/LaunchPadBooster).
- Project skeleton shared with this repo's *Salty's Fabricator Overflow*.
- Built in collaboration with Claude (Anthropic).

**Author:** NaClie88 (Salty).
