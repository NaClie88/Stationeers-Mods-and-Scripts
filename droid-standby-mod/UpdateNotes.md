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

## 2026-10-07: First in-game test, fixes and the vanilla keybind

**Test results (user):**
- Vision and vitals worked "100% as intended": the stun floor applies and vanilla's own vignette, blur and vital-cap effects follow.
- **Bug: walking stayed at full speed.**
- The user expected "Confirm" to freeze controls and fast-forward to dawn. That's the phase 2 Time Skip, not built yet. Recorded in spec §7: controls are frozen while time is accelerated.

**Cause of the speed bug (a spec assumption, now corrected).** `MovementController.MovementHandler` scales only the per-step headroom `(maxSpeed − currentSpeed)` by `(1 − 0.9 × stun/100)`. Every step still closes part of the gap, so a stunned droid reaches full top speed; it just accelerates more slowly. The spec had read it as a top-speed multiplier.

**Fixes and additions:**
- `SpeedPatch`: scales the local player's `characterMaxSpeed` by the movement factor around `MovementHandler`, then restores it. This uses the same scale-and-restore shape as `JumpPatch`.
- `LookPatch`: scales `CameraController.CameraSensitivity` around `SetMouseLook`, so mouse look is sluggish in proportion (user request). The player's own sensitivity setting is never changed.
- `StandbyConfig.JumpFactor` is renamed `MovementFactor`. It's one factor for speed, jump and look: Power Save ≈ 64 %, Deep ≈ 24 %.
- **Vanilla keybind (user-approved design).** The standby key is now "Droid Standby" in Settings > Controls > Inventory (default Z). The LaunchPad `StandbyKey` entry is removed; `LongPressSeconds` stays in LaunchPad config. `KeyBindingPatch` adds a postfix on `KeyManager.SetupKeyBindings`, using reflection to call the private `AddKey`. Vanilla then applies the saved rebind by name, and the Controls screen builds the row from `AllKeys`. A late-load fallback registers the key itself, applies the saved binding from `Settings.CurrentData.KeyList`, and builds the row with `Settings.ControlItemPrefab`. The row is needed so "reset to defaults" (which touches every key's `Display`) can't throw. The log line says which path ran.

**Also found:** LaunchPad 1.0's active profile (`profiles/my mods.xml`) force-disabled this mod and the airlock until they were added to it. See the repo's `CLAUDE.md`.

**UI follow-up (same test):** the overlay showed over the Escape menu. It now hides while `InventoryManager.ShowMenu` (the game menu) is up or the HUD is hidden (`ShowUi` false), and hands the cursor back. At the user's request, every window is now centred horizontally: status lines sit in the middle of the bottom fifth, and the panel and prompt grow upward from just above the bottom edge. Installed (MD5 `b6965444…`).

## 2026-10-07: Phase 1b (Revision 2 states)

**Why:** after the first in-game test, the user redefined the states one by one (spec, Revision 2). The old Deep Standby still let the droid crawl about at about 24 %. Now there are three states on one key:
- **Power Save** (single tap): 12.5 % movement and look, ×0.5 drain.
- **Standby** (double tap): no movement, 6.25 % look, ×0.25 drain, inventory only.
- **Deep Standby** (hold 3 s): a hidden menu, then Start freezes controls and drain.

**What changed and why:**
- **Gestures.** `PressDetector` now reports SingleTap, DoubleTap and LongPress.
  - A single tap waits out the 0.35 s double-tap window, so it can't be mistaken for half of a double tap.
  - In Standby and Deep Standby, every tap means "wake", so the detector switches to immediate taps and waking is instant.
  - `KeyActions.Decide` holds the table. Every gesture in Deep Standby wakes, so holding the key there can't reopen the menu.
- **Renamed hold setting.** The old `LongPressSeconds = 0.6` in an existing `.cfg` would have made the "3 s" hold fire at 0.6 s. The setting is now `HoldSeconds` (3 s), alongside `DoubleTapSeconds`.
- **Numbers live in `LevelProfile`** (pure, tested). Speed, jump and look patches read it. The old stun-derived "movement factor" is gone; vision still comes from the stun floor.
- **Standby input limits.** World interaction is blocked by skipping `InventoryManager.NormalMode` / `PlacementMode` / `PrecisionPlacementMode`. Vanilla already skips those while the cursor is visible (Ctrl/Alt mouse mode), so moving batteries between slots keeps working. Slot hotkeys run before those methods and are untouched. The user confirmed battery swaps must work in Standby.
- **Jetpack.** `HandleJetpack` is skipped in Standby and Deep, but the stabilizer still runs, so a droid parked mid-air in zero-g stays steady.
- **Deep Standby freeze.** `CheckDisplaySlotInput` and the `SlotDisplayButton` click and drag handlers are blocked too, look is 0, and drain is ×0.
- **Built-in night vision.** It's switched off on Start, and the N key is blocked until wake. Night Vision Goggles are a tool and are untouched (user clarification). Both share one camera effect, so the mod only switches it off when no lit goggles are worn.
- **Vanilla helmet-light drain bug, found and fixed** (user: "patch the bug so the mod user does not have to deal with it ... if they patch it, the mod will not break").
  - **The bug:** `Human.ToggleHelmetLight` calls `SetPowerDrain(105f)` on a *static* shared by every human, and it does so even when switching the light off. Only `ToggleNightVision` resets it to 100. So one player's light key raises every droid's drain, and it stays raised. The lights also drain their own batteries.
  - **The fix (`LightDrainFix`):** it blocks `SetPowerDrain`. `DrainPatch` then charges each droid +5 % of its own drain, only while its own helmet light is on and powered, at full rate in every state ("lights drain normally").
  - **Self-disabling:** if `SetPowerDrain(float)` or the static field is missing after a game update, the fix logs one line and stays off; nothing else depends on it. Config: `[Fixes] HelmetLightDrainFix`.
- **Menu and wakes.**
  - Holding the key opens the menu without changing state. Start or Enter enters Deep Standby; Cancel or a tap closes it.
  - Standby arms the config-default wake conditions and wakes silently with a status line. Deep Standby's wake keeps its sound.
- **Safety net** now enters Standby, never Deep, and doesn't fire while the menu is open.
- **Overlay** is anchored just above the hand-slot panel (`InventoryManager.PanelHandsGameObject`'s on-screen top), with a fallback at 80 % of the screen height. The user saw it covering the hand-slot cards.
- **Build notes:**
  - The csproj lists source files explicitly, so the new patch files were added there.
  - `UnityEngine.UI` is referenced for `PointerEventData`, and `UnityEngine.UIModule` for `RectTransformUtility`.

### Final review fixes (fresh reviewer, 2026-10-07)

A fresh reviewer checked the whole branch against the decompile. I confirmed all five Important findings in the decompile before fixing them.

1. **Deep Standby didn't freeze inventory.**
   - **Cause:** two of the patched handlers do nothing that matters. `SlotDisplayButton.OnPointerDown` is empty in vanilla, and `OnPointerClick` only opens Stationpedia. The real paths are `OnPointerUp` (move to hand, smart stow) and the drag pair (`OnEndDrag` also drops into world slots).
   - **More gaps:** KeyManager dispatches the swap-hands, hand-power, smart-stow, inventory-select and drop keys itself.
   - **Fix:** in Deep, `SlotButtonFreezePatch` now targets `OnPointerUp`, `OnBeginDrag` and `OnEndDrag`, and the new `InventoryKeyFreezePatch` blocks those KeyManager handlers.
2. **Ctrl/Alt mouse mode bypassed the Standby world block.**
   - **Cause:** in mouse mode, `InputMouse.Idle` and `Click` interact with the world on their own: switches, door buttons, picking items up.
   - **Fix:** the new `MouseWorldPatch` blocks those two in Standby and Deep, and resets a pending click. Slot moves go through `SlotDisplayButton`, so battery swaps still work.
3. **Clicks on the Deep Standby menu could reach the world behind it.** ImGui doesn't mark the pointer as over UI, so a click on Start or Cancel could also flip a switch. `MouseWorldPatch` now also blocks while the menu is open (`LevelProfile.BlocksMouseWorld`, tested).
4. **The jetpack kept burning propellant in Standby.**
   - **Cause:** skipping `HandleJetpack` left `CurrentEmission` at its last thrust value. That value is networked, and `Jetpack.OnAtmosphericTick` burns propellant from it. A stale `_jetpackUsed` also stopped the stabilizer's damping.
   - **Fix:** the prefix now runs the no-input path itself:
     - it clears `_jetpackUsed`;
     - the stabilizer runs only with propellant;
     - emission is 1 when stabilizing against gravity, otherwise 0;
     - an empty tank clears emissions (no free hover).
5. **Standby's silent low-battery wake could kill an AFK droid sooner.** At 5 %, it put the droid back at full drain, and the safety net couldn't re-engage until input. Standby now keeps the "charged" wake but never wakes on "low" (`WakeEvaluator(..., wakeOnLow: false)`, tested).

### Double tap while asleep (user, 2026-10-07)

A double tap in Standby woke the droid on the first tap, because taps are immediate while asleep. The second tap then started a fresh single tap and dropped the droid into Power Save. Fix: after an immediate (waking) tap, a tap that starts within the double-tap window is ignored, so a double tap simply wakes the droid. A hold started in that window still opens the menu. Covered by 3 new PressDetector tests (106 total).
