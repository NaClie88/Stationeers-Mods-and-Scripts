# Salty's Droid Standby

H.E.M. Droids trade cognition for battery life — ride out a night or a storm on unreliable renewables.

## Controls (default key `Z`, rebindable)

| | Tap | Hold (0.6 s) |
|---|---|---|
| Normal | Power Save Mode | Deep Standby |
| Power Save | back to Normal | Deep Standby |
| Deep Standby | wake | wake |

- **Power Save Mode** — battery drain ×0.5, slower (cognition loss held at 40 → ~64 % speed), weaker jumps. Jetpack unaffected.
- **Deep Standby** — battery drain ×0.25, barely conscious (cognition loss 85 → ~24 % speed, near-black vision). Opens a panel to choose what wakes you: **light** (sunrise), **wind**, **storm start/end** incl. solar storms (outdoors only), **battery** charged/low, **danger** (damage, pressure swing, temperature).
- **AFK safety net** — total battery ≤ 10 % and no input for 60 s → Deep Standby. In single-player — or when you're the only player on your own hosted game — it also pauses until you come back (a joining player ends the pause).

The visuals and slowdown are vanilla's own cognition (stun) effects — the mod only holds a minimum. Beds, cryo tubes and the Droid Sleeper keep their vanilla behaviour (zero drain, charging). Standby is never saved: loading always starts at Normal.

## Install / requirements

- StationeersLaunchPad 1.0+. Mod folder in `Documents/My Games/Stationeers/mods/`. Developers: `build-and-install.sh` (game closed).
- **Multiplayer: required on server and every client.**
- Compatible with vanilla and Salty's Droid Dual Battery (no dependency; drain is scaled on whichever battery is in use, and battery readings sum every battery slot).

## How it works

| File | Patch | Purpose |
|---|---|---|
| `Patches/CognitionFloorPatch.cs` | postfix `Brain.OnLifeTick` (server) | hold the stun floor; clear on death / bed / sleeper |
| `Patches/DrainPatch.cs` | prefix+postfix `Human.OnLifeTick` (server) | refund the unspent share of the tick's drain |
| `Patches/JumpPatch.cs` | prefix+finalizer `MovementController.HandleJump` | scale jump force for the local player |
| `Game/LocalController.cs` | plugin `Update` | key gestures, wake checks, safety net |
| `UI/WakePanel.cs` | postfix `ImGuiWindowManager.Draw` | wake panel, status line, safety prompt |
| `src/*.cs` | — | pure logic, unit-tested in `tests/` (44 tests) |

Design: `docs/superpowers/specs/2026-10-07-droid-standby-design.md`. Plan: `docs/superpowers/plans/2026-10-07-droid-standby-phase1.md`. Dev log: `UpdateNotes.md`.

## Roadmap

- Phase 2: Time Skip (explicit, only inside Deep Standby; battery drain frozen) for single-player / solo host — starts with a feasibility probe.
- Phase 3: multiplayer Time Skip vote (everyone in Deep Standby or asleep).

## Sources and credits

- Game behaviour from decompiling `Assembly-CSharp.dll` with [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`): `Human.OnLifeTick` drain, `Brain.OnLifeTick` stun, `Entity.OnCameraUpdate` / `MovementController` stun effects, `WeatherManager`, `OrbitalSimulation`, `WindTurbineGenerator`, `ImGuiManager`, `KeyManager`, `WorldManager.SetGamePause`, `NetworkBase.Clients`.
- [BepInEx](https://github.com/BepInEx/BepInEx), [Harmony](https://github.com/pardeike/Harmony), [StationeersLaunchPad / LaunchPadBooster](https://github.com/StationeersLaunchPad/LaunchPadBooster) (config + networking), RG.ImGui (the game's own ImGui).
- Built in collaboration with Claude (Anthropic). Author: NaClie88 (Salty).
