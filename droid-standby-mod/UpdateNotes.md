# Salty's Droid Standby — running dev log

Newest entries at the bottom. Design: `docs/superpowers/specs/2026-10-07-droid-standby-design.md`; plan: `docs/superpowers/plans/2026-10-07-droid-standby-phase1.md`.

## 2026-10-07 — Phase 1 (standby core) built

Implemented per the plan, executed inline (one commit per task):
- Pure logic in `src/` (tap/long-press, level transitions, summed battery, wake evaluator with armed thresholds + hysteresis + storm edges, safety-net rule, effectively-solo rule) — 44 xunit tests, all written failing first.
- Server: stun floor on `Brain.OnLifeTick` (only ever raises; cleared on death / sleeping / life suspender); per-droid drain refund around `Human.OnLifeTick` (the static `PowerDrainedPerTick` is never touched).
- Client: jump scaled around `MovementController.HandleJump`; key/wake/safety net in the plugin's `Update` (single driver instance, since LaunchPad creates the component twice); ImGui panel via `ImGuiWindowManager.Draw` postfix with vanilla's CinematicCamera cursor save/restore pattern.
- Networking: one `StandbyRequestMessage` client → host, owner-checked; `Networking.Required = true`.
- `LaunchPadBooster`'s C# 14 extension `SendToHost` compiled fine as a static call from C# 9.

Time Skip (phases 2–3) not started. In-game verification pending (`TESTING.md`).

## 2026-10-07: Final whole-branch review and fix pass

A fresh reviewer read the whole branch against the spec and the decompile. Fixed in one pass. Decisions were moved into the pure logic where possible, test-first (57/57 tests):
- **C1, thread safety.** `Brain.OnLifeTick` and `Human.OnLifeTick` run on a thread-pool thread (`GameManager` → `AtmosphericsManager.LifeTicksTick`). `StandbyRegistry.Set` read `Human.name`, a Unity API, there; an exception would make `GameTick` skip electricity, logic and atmosphere ticks world-wide. Now it logs `ReferenceId` only when verbose; both patches use `ReferenceEquals` and catch everything.
- **C2, client battery.** `BatteryCell.PowerStored` isn't networked (only `CurrentPowerPercentage` is), so clients read 0 % and idle clients were forced into standby. Clients now use the synced percentage (`BatteryMath.Cell`).
- **C3, safety net gate.** It ignored beds, sleepers, death and existing pauses, and flip-flopped every frame in a sleeper. It's now gated on the shared `StandbyRules.ShouldClear` and `!WorldManager.IsGamePaused`.
- **I1, cursor.** `Cursor.lockState` was re-locked by `MouseModeController.Check()` every frame. The panel now uses vanilla's `MouseModeController.AddModal` (as `ConsoleWindow` does).
- **I2, standby key.** The panel no longer claims `KeyInputState.Typing`, so the key works with it open.
- **I3, unconscious.** Standby clears when the droid isn't Alive, so the floor can never hold a droid below vanilla's 50-stun wake threshold.
- **I4, safety net after wakes.** Only a "battery low" wake stands it down until input.
- **I5, disconnect.** In multiplayer, standby clears when the owner disconnects (`Brain.IsOnline`).
- **I6, stale state.** All static controller and panel state resets when the local body changes.
- **I7, joins.** On a join the safety pause defers to vanilla's join pause (`NetworkBase.IsPaused`) and only unpauses itself if vanilla isn't holding one.
- **I8 and spec gaps.** Wake sound added (`UIAudioManager.NarrationPanelHash`). Added a one-time on-screen note if standby disabled itself. Boxes reset to the config defaults on each entry. Enter confirms. `JumpPatch` matches by `parentEntity`.
- Ruling: the light default stays 20 % (planned, now documented in the spec), not the spec's earlier 30 %.
- Deferred minors: per-frame list allocations; the wake-message timer only runs at Normal; the drain refund misses a same-tick α→β spill under the Dual Battery mod (under-refund only); levels on a body the player leaves aren't cleared; no-battery reads as 0 %.
