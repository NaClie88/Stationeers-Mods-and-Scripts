# Salty's Droid Standby

H.E.M. Droids trade cognition for battery life — ride out a night or a storm on unreliable renewables.

## Controls (default key `Z`, rebind in Settings > Controls > Inventory: "Droid Standby")

| | **Power Save** | **Standby** |
|---|---|---|
| **Key** | tap | hold 3 s → menu → **Start** (or Enter) |
| **Top speed, jump, throw** | 12.5 % | frozen |
| **Mouse look** | 12.5 % | frozen |
| **Battery drain** (droid's own) | ×0.5 | frozen |
| **Cognition loss held** (vision) | 40 | 85 |
| **World interaction** (doors, switches, tools) | yes | no |
| **Inventory** | yes | no |
| **Built-in night vision** | yes | switched off, blocked |
| **Wakes on** | tap → Normal | tap, or a condition ticked in the menu → **Power Save** |

- **Only the standby key wakes you.** Ctrl/Alt mouse mode and every other key are left alone.
- **Ramp-down:** entering a lower-drain state eases the battery drain down over 5 s ("powering down… N s"). Raising the drain is immediate.
- **Toggle cooldown:** after any state change, the key can't change state again for 5 s ("Standby systems cycling – ready in N s"). That includes waking. Automatic wake conditions ignore the cooldown, so a danger wake is never delayed.
- **Standby menu:** opened by holding the key 3 s. It doesn't change your state until Start; Cancel or a tap closes it. Wake conditions you can tick:
  - **light** (sunrise);
  - **wind**;
  - **storm start/end** incl. solar storms (outdoors only);
  - **battery** charged or low;
  - **danger** (damage, pressure swing, temperature).

  Time acceleration comes in phase 2.
- **Lights and tools drain normally.** Helmet lights and headlamps run on their own batteries. Night Vision Goggles are a tool and are not affected.
- **AFK safety net:** total battery ≤ 10 % and no input for 60 s → Standby (drain frozen). In single-player, or when you're the only player on your own hosted game, it also pauses until you come back.
- **Fixes a vanilla bug:** the helmet-light key raised *every* droid's battery drain by 5 % until night vision was toggled. Now each droid pays the 5 % only while its own helmet light is on. The fix is optional (`[Fixes] HelmetLightDrainFix`) and switches itself off if a game update changes that code.
- **Overlay position:** `[Overlay] BottomPercent` (default 72) sets how far down the screen the overlay sits, read live.

The visuals and slowdown are vanilla's own cognition (stun) effects — the mod only holds a minimum. Beds, cryo tubes and the Droid Sleeper keep their vanilla behaviour (zero drain, charging). Standby is never saved: loading always starts at Normal.

## Install / requirements

- StationeersLaunchPad 1.0+. Mod folder in `Documents/My Games/Stationeers/mods/`. Developers: `build-and-install.sh` (game closed).
- **Multiplayer: required on server and every client.**
- Compatible with vanilla and Salty's Droid Dual Battery (no dependency; drain is scaled on whichever battery is in use, and battery readings sum every battery slot).

## How it works

| File | Patch | Purpose |
|---|---|---|
| `Patches/CognitionFloorPatch.cs` | postfix `Brain.OnLifeTick` (server) | hold the stun floor; clear on death / bed / sleeper |
| `Patches/DrainPatch.cs` | prefix+postfix `Human.OnLifeTick` (server) | scale the droid's own drain; charge the helmet-light share |
| `Patches/JumpPatch.cs` | prefix+finalizer `MovementController.HandleJump` | scale jump force for the local player |
| `Patches/SpeedPatch.cs` | prefix+finalizer `MovementController.MovementHandler` | cap top speed (vanilla stun only slows acceleration) |
| `Patches/LookPatch.cs` | prefix+finalizer `CameraController.SetMouseLook` | sluggish mouse look |
| `Patches/KeyBindingPatch.cs` | postfix `KeyManager.SetupKeyBindings` | "Droid Standby" in vanilla Settings > Controls |
| `Patches/JetpackPatch.cs` | prefix `MovementController.HandleJetpack` | no jetpack thrust in Standby/Deep (stabilizer kept) |
| `Patches/InteractionPatch.cs` | prefixes:<br>• `InventoryManager.NormalMode` / `PlacementMode` / `PrecisionPlacementMode`<br>• `InputMouse.Idle` / `Click` (Ctrl/Alt mouse mode)<br>• `CheckDisplaySlotInput`<br>• `SlotDisplayButton.OnPointerUp` / `OnBeginDrag` / `OnEndDrag`<br>• KeyManager swap / stow / select / drop / hand-power keys | no world interaction in Standby/Deep, or through the open menu; no inventory in Deep |
| `Patches/ThrowPatch.cs` | prefix+postfix `ThrowItemBehaviour.DropKeyHeld` / `Throw` | throw power capped like movement |
| `Patches/NightVisionPatch.cs` | prefix `Human.ToggleNightVision` | built-in night vision off and blocked in Deep |
| `Patches/LightDrainFix.cs` | prefix `Human.SetPowerDrain` (optional) | vanilla helmet-light drain bug fix |
| `Game/LocalController.cs` | plugin `Update` | key gestures, wake checks, safety net |
| `UI/WakePanel.cs` | postfix `ImGuiWindowManager.Draw` | wake panel, status line, safety prompt |
| `src/*.cs` | — | pure logic, unit-tested in `tests/` (110 tests) |

Design: `docs/superpowers/specs/2026-10-07-droid-standby-design.md`. Plans: `docs/superpowers/plans/2026-10-07-droid-standby-phase1.md`, `...-phase1b.md`. Dev log: `UpdateNotes.md`.

## Roadmap

- Phase 2: real time acceleration after Deep Standby's Start (single-player / solo host). It starts with a probe of the game-tick pacing.
- Phase 3: multiplayer Time Skip vote (everyone in Deep Standby or asleep).

## Sources and credits

- Game behaviour from decompiling `Assembly-CSharp.dll` with [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`): `Human.OnLifeTick` drain, `Brain.OnLifeTick` stun, `Entity.OnCameraUpdate` / `MovementController` stun effects, `WeatherManager`, `OrbitalSimulation`, `WindTurbineGenerator`, `ImGuiManager`, `KeyManager`, `WorldManager.SetGamePause`, `NetworkBase.Clients`.
- [BepInEx](https://github.com/BepInEx/BepInEx), [Harmony](https://github.com/pardeike/Harmony), [StationeersLaunchPad / LaunchPadBooster](https://github.com/StationeersLaunchPad/LaunchPadBooster) (config + networking), RG.ImGui (the game's own ImGui).
- Built in collaboration with Claude (Anthropic). Author: NaClie88 (Salty).
