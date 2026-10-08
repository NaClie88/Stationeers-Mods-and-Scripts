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

## Added after the first in-game test (2026-10-07)

- [ ] Power Save: top walking speed is clearly lower (about 64 %), not just slower to accelerate. Deep Standby: about 24 %.
- [ ] Mouse look is sluggish in proportion in both levels, and back to normal on waking. Your sensitivity setting is unchanged afterwards.
- [ ] Settings > Controls > Inventory shows a "Droid Standby" row bound to Z. Rebind it, and the new key works immediately.
- [ ] Restart the game: the rebind is kept.
- [ ] "Reset to defaults" in Controls works without errors, and Droid Standby goes back to Z.
- [ ] The log shows `Standby key registered … (with vanilla setup)` or `(late path)`. Note which one.
- [ ] The standby overlay hides while the Escape/game menu is open (and when the HUD is hidden), and comes back when it closes.
- [ ] Status line and messages sit centred in the bottom fifth of the screen; the wake panel and "Saved you" prompt are centred near the bottom edge.

## Phase 1b (Revision 2 states)

**Gestures and levels**
- [ ] A single tap enters Power Save after about ⅓ s: very slow walking, sluggish look. Tapping again returns to Normal.
- [ ] A double tap enters Standby: no walking, jumping or jetpack, very slow look, and the status line reads "Standby - tap the standby key to wake".
- [ ] A tap in Standby wakes instantly.

**Standby limits**
- [ ] Doors and switches can't be used, and a held tool does nothing to the world.
- [ ] Ctrl/Alt + mouse still moves a battery between slots.
- [ ] Slot hotkeys still work: slot 5 (battery) swaps a battery with the hand, including with Salty's Droid Dual Battery installed (its second battery slot too).
- [ ] Jetpacking in zero-g, then entering Standby: no thrust, and the droid stays stable.
- [ ] Standby auto-wakes at dawn (default: Light) with a status line and **no sound**.

**Deep Standby menu**
- [ ] Holding the key 3 s opens the menu, and the droid stays in its current state while it's open. Readings update live.
- [ ] Cancel closes the menu with no state change. A tap closes it too.
- [ ] Enter or Start enters Deep Standby, after which:
  - no movement, look, inventory or world use;
  - the battery % doesn't drop over a minute (helmet light off);
  - a tap wakes;
  - **holding** the key also wakes, and does not reopen the menu.
- [ ] Built-in night vision on (N), then Start: it switches off, N does nothing until you wake, and works again afterwards.
- [ ] Night Vision Goggles worn and on, then Start: the goggles stay on.

**Helmet light fix**
- [ ] Helmet light on during Deep Standby: the battery drops slowly (5 % of normal). Light off: it holds still.
- [ ] Outside standby, toggle the helmet light on and then off with its key: the drain returns to normal at once, with no need to press N. The log shows `Helmet-light drain fix: active`.
- [ ] Set `[Fixes] HelmetLightDrainFix = false`: the log says `off (config)` and everything else works.

**Overlay, safety net and config**
- [ ] The overlay sits above the hand-slot cards, centred, and hides under Esc.
- [ ] Safety net (≤10 %, 60 s idle): enters **Standby**, not Deep.
- [ ] An old `LongPressSeconds` line in the `.cfg` is ignored, and the hold takes 3 s. The log line `Config bound:` shows `hold=3s`.

**From the final review**
- [ ] Deep Standby blocks every inventory route:
  - Ctrl/Alt + click on the battery slot does nothing;
  - dragging a slot does nothing;
  - the swap-hands, drop and smart-stow keys do nothing.
- [ ] Standby: Alt + click a door switch or lever does nothing, but Alt + dragging a battery between slots still works.
- [ ] With the Deep Standby menu open over a switch, clicking Start or Cancel doesn't flip the switch.
- [ ] Jetpack: thrust forward in zero-g, then double-tap into Standby:
  - the exhaust stops;
  - the droid slows to a stop (stabilizer on);
  - propellant stops draining (watch the tank over a minute).
- [ ] Standby at 6 % battery, left idle: the droid does **not** wake at 5 %.
- [ ] Double tap while in Standby: the droid wakes to Normal and stays there (no Power Save).

## Results, in-game test 1 of phase 1b (2026-10-08)

- **Passed:**
  - the three states and gestures (single tap, double tap, hold → menu, Start, wake);
  - Standby limits (world blocked, battery swaps work, jetpack);
  - Deep Standby extras (night vision, goggles, menu click-through, inventory locked);
  - wakes fire;
  - the overlay hides under Esc;
  - save and reload starts in Normal.
- **Fixed after this test, needs a retest:**
  - [ ] The jetpack in Normal no longer throws errors (Stabilizer is a property). Jumping in Normal is full height.
  - [ ] The Deep Standby wake makes an audible chime (now StageComplete; NarrationPanel was silent).
  - [ ] The overlay sits clearly above the hand-slot cards (capped at 80 % down the screen). Look for the `Overlay anchor:` log line.
- **Not tested yet:**
  - the helmet-light fix (no way to test);
  - the Droid Sleeper;
  - storm wakes;
  - multiplayer (no second player).
  - [ ] Holding Q in Power Save throws only weakly (the meter stops at about ⅛). In Standby, Q just drops the item. In Normal, full throws.
