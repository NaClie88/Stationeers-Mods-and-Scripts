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

## 2026-10-08: In-game test 1 of phase 1b, jetpack exception

**User report:** in Normal, the jetpack threw errors every frame and didn't work. Jumping also seemed stuck limited.

**Cause:** `JetpackPatch` resolved `MovementController.Stabilizer` as a *field*, but it's a private *property* (decompile line 188; I misread `if (Stabilizer)` as a field read). The lookup sat in a static initializer, so it threw a TypeInitializationException on the first jetpack frame. After that, every call to the prefix threw, in every state including Normal, and vanilla's `HandleJetpack` never ran. While the jetpack is switched on, the droid is in jetpack mode, and vanilla's ground jump (`HandleJump`) only runs in walking mode. So the broken jetpack is the most likely cause of the jump report too. In Normal, `JumpPatch` doesn't change anything.

**Fix:**
- `Stabilizer` is read through `AccessTools.PropertyGetter`.
- All three members are resolved in Harmony's `Prepare()`. If any is missing, the patch isn't applied, a log line says so, and vanilla's jetpack runs untouched.
- The prefix is wrapped in try/catch and falls back to vanilla for that frame, logging once.

**Lesson:** resolve reflected members at patch time, never in a static initializer that first runs mid-game.

### In-game test 1 follow-ups (2026-10-08)

- **Wake sound.** The user heard nothing on a Deep Standby wake, only the vanilla low-battery alert afterwards. `UIAudioManager.NarrationPanelHash` is declared but vanilla never plays it, so that clip is probably missing. The wake now uses `StageCompleteHash`, the helper-hint chime, which vanilla plays through the same 2D `Play` path.
- **Overlay still over the hand cards.** The position measured from `PanelHandsGameObject` landed too low; that rect is evidently not the visible cards. The anchor is now capped at 80 % down the screen, one card height above the cards, whose top is about 90 % down at 4K. The measurement is logged once (`Overlay anchor:`) so the rect can be understood or the measurement dropped.
- **Throw power (user request).** Holding Q charges `ThrowItemBehaviour._throwForce` up to `_maxThrowForce` (6). The new `ThrowPatch` clamps it to max × `LevelProfile.Throw(level)`, which equals the movement factor:
  - Power Save: 12.5 %.
  - Standby: 0, so Q just drops the item and a dead battery can still be put down.
  - Deep Standby: the key is already blocked.
  - Its members are resolved in `Prepare()`, and it fails open to vanilla.

## 2026-10-08: Revision 3 (two states, wake into Power Save, ramp and cooldown)

**Why (user, after in-game test 2):**
- "we should just remove standby and only have the Deep Standby which will inherit the name. remove the double press."
- "Waking should wake in to low power mode. but keep the toggle cool down."
- A 5 s ramp-down on battery usage and a 5 s toggle cooldown, "with appropriate messages".

**What changed:**
- **States.** `StandbyLevel` is now Normal, PowerSave and Standby (wire values 0, 1, 2). The old double-tap Standby is gone, and the old Deep Standby is renamed Standby.
- **Gestures.** `PressDetector` is back to Tap and LongPress. A tap fires at once on release, since there's no double tap to wait for. The `DoubleTapSeconds` setting is removed.
- **Waking** goes to Power Save (`KeyActions.WakeTarget`), from a tap or an automatic wake. Every gesture in Standby wakes.
- **Drain ramp** (`DrainRamp`, pure and tested). `StandbyRegistry` now stores, per Human, when the level changed and the drain factor in effect at that moment. `DrainPatch` asks it for the ramped factor using `StandbyClock`, a Stopwatch, because the life tick runs on a worker thread where Unity's `Time` isn't usable. Easing down takes `[Timing] RampDownSeconds` (5). Going up is immediate, so quick toggling can't bank cheap seconds. A change mid-ramp starts from the current effective rate.
- **Toggle cooldown** (`ToggleCooldown`, `KeyActions.IsGatedByCooldown`, pure and tested).
  - After any state change, key actions that change state (Power Save on or off, wake) wait `[Timing] ToggleCooldownSeconds` (5).
  - Opening the menu isn't gated, but **Start** shows "Start in N s" until the cooldown ends.
  - Automatic wake conditions bypass the cooldown. They aren't toggles, and a danger wake must never wait.
  - My reading of "keep the toggle cool down": it applies to waking with the key too.
- **Messages:** "powering down… N s" on the status line during the ramp; "Standby systems cycling – ready in N s" when the key is pressed during the cooldown; "Woke into Power Save: <reason>" on an automatic wake.
- **Safety net** enters Standby (drain frozen) with the config-default wake conditions, minus the low-battery wake, and turns off night vision.
- **Overlay.** The measured `PanelHandsGameObject` rect (top 90 px above the bottom at 1080p) isn't the cards the user sees: the overlay still covered them even at 80 %. The anchor is now a live config value, `[Overlay] BottomPercent` (default 72), and the rect measurement is gone.
- **Config.** The `DoubleTapSeconds` key and the old `[Standby] DrainFactor` and `[DeepStandby]` sections are no longer read; stale lines in an existing `.cfg` are ignored. `[Standby] CognitionLossFloor` keeps its meaning (85).

### Overlay default 72 → 82 % (2026-10-08)

At 72 % the user saw the overlay "almost in the middle of the screen". The earlier "still on the hands" report probably came from a build before the 80 % cap (the user: "or i gave you instructions at the wrong development cycle"). ImGui draws into a Screen-sized render texture (`ImGuiManager`), so the percentages are true screen fractions. The new default follows the original instruction, "raise by one card height": the overlay started centred at 90 % and a card is about 8.5 % of the screen, so its bottom edge now sits at about 82 %. **Note for players:** BepInEx doesn't overwrite a value already saved in the `.cfg`. An existing `BottomPercent = 72` stays at 72 until it's edited or deleted.
