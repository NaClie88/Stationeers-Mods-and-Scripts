# Droid Standby — Phase 1 in-game checks

Log lines expected once each: `Cognition floor patch succeeded`, `Drain scaling patch succeeded`, `Jump power patch succeeded`, `Wake panel patch succeeded`.

- [ ] Tap Z: "Power Save Mode" shows; cognition ~40 %, walking ~64 %, jumps lower, jetpack unchanged.
- [ ] Battery drains at about half the normal rate (time 60 s normal vs Power Save; VerboseLogging on).
- [ ] Hold Z: Deep Standby, near-black, ~24 % speed, panel opens with live readings; still conscious.
- [ ] Battery drains at about a quarter of normal.
- [ ] Confirm collapses to status line; clicking it reopens the panel.
- [ ] Tap or hold Z in Deep Standby (after Confirm) wakes; vision recovers over a few seconds.
- [ ] Z does nothing while typing in chat / console or in the pause menu.
- [ ] Light: enter at night with Light ticked — wakes at sunrise with "Woke: sunrise". Entering at noon does NOT wake instantly.
- [ ] Storm: outdoors, storm start and end each wake; indoors they don't. Solar storm reported as "solar storm".
- [ ] Battery: with Battery ticked, charge past 90 % with a handheld charger → wakes "battery charged". (Entering a Droid Sleeper clears standby by design — vanilla's sleeper already has zero drain.)
- [ ] Danger: take damage → wakes immediately; depressurise the room → wakes "pressure change".
- [ ] Safety net (single-player): battery ≤ 10 %, idle 60 s → Deep Standby + game paused + "Saved you" prompt; Resume works.
- [ ] Safety net as the only player on a hosted multiplayer game: pauses like single-player; when a second player joins, the pause ends (droid stays in Deep Standby).
- [ ] Safety net with two players connected: Deep Standby only, no pause.
- [ ] Safety net does not loop after a "battery low" auto-wake while still AFK.
- [ ] Entering a bed / Droid Sleeper / dying clears standby.
- [ ] With Salty's Droid Dual Battery: drain still alpha → beta; battery % reading includes both slots.
- [ ] Save while in standby, reload: starts in Normal.
- [ ] Multiplayer: client's standby applies (host sees the client droid slow + low drain); client without the mod is refused.

## Added after the final code review (2026-10-07)

- [ ] Panel buttons and checkboxes are clickable WITHOUT holding Alt; Enter confirms the panel.
- [ ] Z wakes from Deep Standby even while the panel is still open.
- [ ] An automatic wake plays a sound.
- [ ] Lie down in a Droid Sleeper at <= 10 % battery and go idle 60 s+: no safety net, no pause, no flicker; it just charges.
- [ ] Esc-pause the game and go idle at low battery: the safety net does NOT fire.
- [ ] Run a droid's battery flat in Deep Standby until it goes unconscious: standby clears, and after a battery swap the droid wakes up normally (not stuck unconscious).
- [ ] Multiplayer client: the panel's battery % matches the HUD (not 0 %), and an idle client at a healthy charge is NOT put into standby.
- [ ] Multiplayer: client disconnects while in Deep Standby, reconnects: normal vision and speed (standby cleared on the server).
- [ ] Quit to menu while the safety pause dialog is up, load again: no stale "Saved you" dialog, cursor behaves normally.
- [ ] Sunrise auto-wake while AFK at low battery: the safety net can still catch the droid again later (only a "battery low" wake disarms it until input).
- [ ] Logs show no `CognitionFloorPatch:` / `DrainPatch` errors (those run on the game-tick thread).
- [ ] Jump is reduced in both levels (`JumpPatch` now matches by `parentEntity`); also confirms Harmony accepted the Finalizer's `__state` (else `Jump power patch failed` at startup).
