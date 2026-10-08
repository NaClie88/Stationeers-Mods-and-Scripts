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
