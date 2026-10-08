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
