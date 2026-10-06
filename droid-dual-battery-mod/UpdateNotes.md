# Salty's Droid Dual Battery: running dev log

A running record of what was done and **why**, written as the work happens. Newest entries go at the bottom of each section.

## Goal
A HEM Droid gets a **second battery slot** on its body so batteries can be hot-swapped without the droid going dark. Drain order: slot 1 until empty, then slot 2.

A pop-out "battery pack" item (suit-style pane) was considered and rejected by the user in favour of a second body slot. The pop-out UI (`SlotDisplay.OnPlayerInteract`) only opens for an occupant with `HasSlots`, and a bare `BatteryCell` has none. The pane route would therefore have needed a whole new item prefab and recipe.

## 2026-10-05: Research (decompiled Assembly-CSharp)

**The droid battery slot is the Uniform slot in disguise.**
`Human.SetSpeciesSpecificSlots()` (called from `UpdateCosmeticIdentity`) does this for `SpeciesClass.Robot`:
```
UniformSlot.Type = Slot.Class.Battery; UniformSlot.StringKey = "Battery"; ...
```
That's why it sits on the #5 key (`KeyMap.UniformSlot = Alpha5`).

**Which battery the droid drains** is decided in one place, the `Human.RobotBattery` getter:
1. the uniform-slot battery (cached in `_robotBattery` by `OnUniformOccupantChanged`), unless it's null or empty
2. otherwise a non-empty `BatteryCell` in the left hand, then the right hand (vanilla already has a hand-held hot-swap fallback!)

Every consumer reads that getter, so patching it covers all of them:
- `Human.OnLifeTick` drains it
- `Brain` stuns the droid when it is null or empty
- `StatusUpdates` drives the HUD damage colour and the "no battery / no charge" text
- `DroidSleeper.ChargeRobot` charges it
- `DisposableBatteryCharger.GetTargetBattery` targets it

**How Human slots work.**
- `Thing.Slots` is a serialized `List<Slot>` on the prefab. `Thing.Awake → ConfigureSlots()` sets `slot.Parent = this` and `slot.Action = InteractableType."Slot{index+1}"`. The enum goes up to `Slot109`, so an extra index is fine. After that, `slot.Initialize()` sets the icon.
- `Human.Awake` calls `base.Awake()` first, then finds its named slots by Type or StringHash. A slot we append *before* that (Harmony prefix) gets configured by vanilla for free.
- `Slot.SlotIndex` = `Parent.Slots.IndexOf(this)` (lazily cached).
- **Save format**: a child item stores `ParentSlotId = ParentSlot.SlotIndex` (`DynamicThing` save data). On load, `MoveToParent` does `thing.Slots[parentSlotId]`. If that index doesn't exist, it prints *"Slot N does not exist on thing …"* and doesn't crash. So a save made with the mod and loaded without it should lose only the slot-2 battery placement. Not yet verified in-game.

**How HUD slot buttons work.**
- `InventoryManager.DisplaySlots` is a serialized `List<SlotDisplay>`. Each `SlotDisplay` has a `SlotId` (index into `parent.Slots`), a `SlotDisplayButton` (the GameObject), and a `SwapButton` (the hotkey name).
- `InventoryManager.Initialize(Entity)` loops `DisplaySlots`. It hides buttons whose `IsAvailableForSpecies` is false, and otherwise calls `LinkToSlot(parent)`, i.e. `Slot = parent.Slots[SlotId]`. It never calls `SetVisible(true)` on the hidden ones, so we manage our own button's visibility.
- The per-frame key loop does `if (displaySlot.SwapButton != string.Empty && CheckDisplaySlot(...))`. A `null` SwapButton would reach `KeyManager.GetKey(null)`, so **the clone must use `string.Empty`** to opt out of hotkeys.

**Slot naming**: `Localization.GetName(slot)` looks up `SlotsName[slot.StringHash]`. The extra slot reuses StringKey `"Battery"` so it gets the vanilla localized name instead of an error string.

## Design decisions
- **Every Human gets the slot**, not just droids. Slot indices are then identical for every player on every machine, which matters because saves and network messages address slots by index. Only the HUD button is droid-only. A human can't use the slot anyway: there's no button for them.
- **Multiplayer**: `MOD.Networking.Required = true`. A client without the mod would have a different Human slot layout.
- **No hotkey in v1**: the slot is used by click/drag. (Possible later: holding #5 again cycles to slot 2.)
- **Extra slot hides its occupant** (`HidesOccupant = true`), so two battery meshes don't stack at the same body anchor.

## Known follow-ups
- `DroidSleeper.ChargeRobot` only charges what `RobotBattery` returns. Slot 2 won't charge in a sleeper while slot 1 has charge. Fix it later if it matters in play.

## 2026-10-05: Implementation (v0.1)

Scaffolded from `fabricator-overflow-mod`: same csproj shape, `build-and-install.sh`, static Logger, `OnLoaded(ConfigFile)`, `_patched` guard, and `PatchSafely`. Extra references were needed: `UnityEngine.AnimationModule` (for `Animator.StringToHash`), plus `UnityEngine.UI` / `UIModule` for the HUD types.

**`Patches/ExtraBatterySlotPatch.cs`**
- `ExtraBatterySlotPatch`: **prefix** on `Human.Awake`. It appends a new `Slot` copied from the Uniform slot (found by `Slot.UniformHash`, because Human's named slot fields aren't assigned yet). It's a prefix so vanilla's own `base.Awake() → ConfigureSlots()` sets `Parent` and the `Slot{N}` action, and `Initialize()` sets the icon. The slot is tracked per-Human in a `ConditionalWeakTable` rather than by a fixed index, in case a Human prefab variant has a different slot count.
- `ExtraBatterySlotSpeciesPatch`: **postfix** on the private `Human.SetSpeciesSpecificSlots`. It retypes the slot `Battery` for droids and `Blocked` for everyone else.
  - *Why Blocked:* an item-stowing code check found that `InventoryManager` smart-stow (`ParentHuman.GetFreeSlot(type)`) and `WorldManager` spawn-kit placement both drop items into **any** free slot of matching Type. If the slot were Battery-typed on a human (who has no button for it), a battery could vanish into it. Vanilla already uses `Slot.Class.Blocked` the same way (`ProjectileLauncher` chamber).
  - Side benefit for droids: smart-stowing a battery from hand fills #5 first, because it comes first in `Slots`, and then slot 2.

**`Patches/RobotBatteryPatch.cs`**: **postfix** on the `Human.RobotBattery` getter. If the #5 battery is null or empty and the extra slot holds a non-empty battery, the extra battery is returned. Vanilla's hand-held fallback is only used when both slots are dry. Order: slot 1 → slot 2 → hands. With `VerboseLogging` on, it logs each switch once per change (droids only, since `StatusUpdates` reads the getter for every species).

**`Patches/HudSlotPatch.cs`**: **prefix** on `InventoryManager.Initialize(Entity)`:
- finds the `DisplaySlots` entry bound to the uniform slot index that is available for `SpeciesClass.Robot`
- `Instantiate`s its button GameObject as the next sibling
- marks it `SpeciesSpecific` / `Robot`
- hides the copied "5" `HotkeyDisplay`
- wraps it in `new SlotDisplay(button) { SlotId = extraIndex, SwapButton = string.Empty }` and adds it to `DisplaySlots`

Vanilla's loop then links, refreshes and (for non-droids) hides it.

**Postfix** on the same method: calls `SetVisible(isRobot)`, because vanilla never re-shows a hidden button.

The hotbar's layout isn't visible in the decompile (it's prefab data). So the patch logs the row's components and positions once. If there's no `LayoutGroup`, it shifts the clone one button-width right as a first guess. Expect to adjust this after the first in-game look.

Build: clean, 0 warnings.

## Open questions for the first in-game test
1. Does `HUD layout:` in the log show a LayoutGroup, and where did the button actually land?
2. Does a battery placed in slot 2 survive save → reload?
3. Do vanilla hand slots still work, i.e. that `DisplaySlots[0]`/`[1]` stayed the hands? (We append at the end, so they should.)
4. Is the button correct on a brand-new droid character, i.e. did species get set before `InventoryManager.Initialize`?

Installed v0.1 via `build-and-install.sh` (MD5 `77b41eaf…`). Waiting on the first in-game test.

## 2026-10-05: First test attempt (no result)

On launch, StationeersLaunchPad auto-updated itself to **v1.0.0** (replacing `LaunchPadBooster.dll`) and asked for a restart. The game was closed before any mods loaded, so v0.1 was never exercised. Checked the new `LaunchPadBooster.dll` by decompiling it: `new Mod(name, version)` and `Mod.Networking.Required` are unchanged, so this mod's source is compatible.

## 2026-10-05: Refactor to a droid-only slot (v0.2)

**Why.** The user challenged v0.1's design. It appended the slot to *every* Human and retyped it `Blocked` on humans and Zrilians. That was never the intent, and it was a workaround layered on a workaround. v0.1 did it to avoid a feared timing problem: species is unknown at `Human.Awake`, and saved batteries address their slot by index, so a slot added "late" might not exist when the battery loads.

**Verification that the fear doesn't hold.** Decompile trace:
- *Save load:* `XmlSaveLoad.Load` → `Thing.Create` → `DeserializeSave` back-to-back in one call. `Human.DeserializeSave` reads cosmetics → `UpdateCosmeticIdentity` → `SetSpeciesSpecificSlots`, all synchronous. An item loaded before its parent waits in `DynamicThing.MoveToParentWhenReady` and only re-checks on the next frame, after its parent has fully deserialized.
- *Client join:* `NetworkClient.ProcessThings` does `Create` → `DeserializeOnJoin` one thing at a time. `Human.DeserializeOnJoin` reads cosmetics → `UpdateCosmeticIdentity` synchronously. In `DynamicThing.DeserializeOnJoin` a child requires its parent to already exist (vanilla errors otherwise), so the server sends parents first.
- *Live play:* the slot only gets used when the player moves a battery into it, long after species is set.

**Prior art (web research).** [Suit Enhancement Suite](https://github.com/altmank/Stationeers-SuitEnhancementSuite) adds slots to suits and uniforms the same basic way. It appends `new Slot{...}` to `Slots`, is idempotent via a `StringKey` check, and always appends *after* the vanilla and other-mod slots (`[HarmonyPriority(Priority.Last)]` on its `Thing.Awake` prefix). The reasoning: "the game stores each item by slot position", so indices must never depend on mod load order. It also adds in a `DeserializeSave` prefix so slots exist before saved children are restored. Our slot is appended at species time, which is after every Awake-time slot (vanilla or other mods), so it always lands last. No other mod for a second HEM Droid battery was found. A Steam discussion shows players asking for "Main and Reserve" droid batteries.

**Changes.**
- `ExtraBatterySlotPatch` is now a **postfix on `Human.SetSpeciesSpecificSlots`**, the same moment vanilla turns the Uniform slot into the droid battery slot. **Robot only**: `EnsureAdded` appends a `Battery` slot (idempotent via the per-Human table) and does by hand what `Thing.Awake` does for prefab slots: `Parent`, `Action = Slot{index+1}`, `Initialize()`. The `Human.Awake` prefix and the `Blocked` retyping are **removed**, so humans and Zrilians are untouched.
- `HudSlotPatch` is now a **postfix** on `InventoryManager.Initialize` and also callable from the species postfix (a new character can learn its species after its HUD is built). It builds and links the cloned button itself, and **no longer adds it to `InventoryManager.DisplaySlots`**. Why: vanilla's per-frame key loop dereferences `displaySlot.Slot.Occupant` on every entry, so an unlinked entry (a human character after a droid one) would throw every frame. Clicks and drags use the button's own `SlotDisplay`, and `Slot.RefreshSlotDisplay` uses `Slot.Display`, so the list isn't needed.
- Version bumped to **0.2**.

**Still to verify in-game:** HUD button placement (`HUD layout:` log line), slot 1 → slot 2 switchover, save/reload with a battery in slot 2, a brand-new droid character, and a human character showing no button.

Installed v0.2 via `build-and-install.sh` (MD5 `d7e721ca…`).

## 2026-10-05: Q: what happens to the batteries if a character changes species?

**Vanilla never changes species on a live body.** Every caller of `Human.UpdateCosmeticIdentity` sets species on a body that was *just created*:
- `Human.CreateCharacter` (new character or respawn)
- `CryoTube.MoveBodyBagToSlot` (a revive builds a fresh body via `CreateEmptyHuman`)
- `Human.DeserializeSave` / `DeserializeOnJoin` (loading)

`HumanIdentityMessage.Process` updates an existing body's `CosmeticData` and looks, but does **not** call `SetSpeciesSpecificSlots`. Vanilla's `SetSpeciesSpecificSlots` also has no "undo" branch: Human/Zrilian do nothing, so a body that had been a droid would keep #5 as a Battery slot. The game doesn't support a mid-life switch; picking a different species in character creation always means a **new body**.

**What that means for the mod:**
- *Respawning as a different species:* the old droid body (corpse) keeps both of its battery slots and whatever is in them, exactly like vanilla #5. It can be looted through its inventory window, which lists every interactable slot, ours included. The new body only gets slot 2 if it's a droid. The HUD button follows the new body: `InventoryManager.Initialize` → `Refresh` hides it when the new body has no slot 2.
- *Save/reload of the droid corpse:* its cosmetics still say Robot, so the slot is re-added at load and its battery restored.
- `EnsureAdded` never removes the slot. A hypothetical live switch away from Robot would leave slot 2 (and its battery) in place, the same as vanilla leaves #5. Batteries only drain for `IsArtificial` bodies.

## 2026-10-05: First real test (v0.2): no second slot visible

The user loaded an existing droid save and saw only one battery slot. `LogOutput.log`:
- `Droid second battery slot patch succeeded`, `Robot battery selection patch succeeded`
- **`HUD slot button patch failed`**: `HarmonyException: Ambiguous match for ... InventoryManager.Initialize`

**Cause:** `InventoryManager` has two overloads, `Initialize()` and `Initialize(Entity)`, and the attribute didn't name one. The slot itself was most likely being added (its patch attached), but with no HUD button there was no way to see or use it. *Note: existing saves need no migration. The slot is added when a droid's species is applied at load.*

**Fix:** `[HarmonyPatch(typeof(InventoryManager), nameof(InventoryManager.Initialize), new[] { typeof(Entity) })]`. Compiled clean; to be installed once the game is closed.

Also confirmed in this run: the mod loads fine under LaunchPad **1.0.0**.

Installed the fix via `build-and-install.sh` (MD5 `5d6ec699…`).

## 2026-10-05: Second test run (HUD fix installed)

**Worked:** all three patches attached. The second button was created (`HUD: added second battery button (cloned from 'Uniform', hotkey 'UniformSlot')`). `HUD layout:` shows the button row is `DynamicGrid` with a **VerticalLayoutGroup** + ContentSizeFitter, so the sibling-index insert stacks the clone under #5 with no manual offset.

**Reported issues:**
1. *"An error on the first battery tick after swapping slot 1 and 2", shown in red at the lower edge of the screen.* The only exception in Player.log that session is from **another mod**: `filtercleanermod.StructureFilterCleaner.HandleItemDelay` → `MissingMethodException: UniTask.Delay(int,bool,PlayerLoopTiming,CancellationToken)`. That mod is incompatible with the current UniTask. No exception from this mod. To re-check the full log after the game closes.
2. *Both buttons labelled "Uniform".* The HUD label is baked into the button prefab: no code assigns `SlotText` for HUD buttons (only `InventoryWindow` does, for pop-out windows). So the clone inherited #5's text. **Fix:** set the clone's label to "Battery 2", and log the source label and its components once, in case a localisation component rewrites it. **Still open:** what vanilla #5 shows for a droid (user thinks it differed from "Uniform" before the mod).
3. *Droid Sleeper only charges battery 1.* This was the known follow-up. `DroidSleeper.ChargeRobot` charges `human.RobotBattery` and then the suit battery. **Fix:** new `DroidSleeperPatch` (postfix on `ChargeRobot`) charges slot 2 the same way vanilla charges the suit battery: `min(_chargePerTick, room)` per power tick, added to `_powerUsedDuringTick` so the sleeper draws it from the grid. Skipped when slot 2 *is* `RobotBattery` (#5 empty), since vanilla just charged it. Private fields are reached via `AccessTools.FieldRefAccess`.

Build needed a `Unity.TextMeshPro` reference for the label. Compiled clean; not yet installed (game running).

**Follow-up (same day):** the user confirmed that the *original* #5 button also reads "Uniform" on a droid. Vanilla never relabels the HUD button when it retypes the slot. The full session log after closing has exactly one exception (Filter Cleaner's), and none from this mod.

At the user's request, the **HUD labels are now "Battery 1" / "Battery 2" while the local player is a droid**. The vanilla #5 button's original text is captured when the clone is built and restored when the local player is a human or Zrilian (the button is shared across species). Installed (MD5 `b7f84324…`) with the sleeper and label fixes.

## 2026-10-05: Third test: Droid Sleeper misses battery 1

**User report:** with two dead batteries the sleeper charged both. ✅ But after swapping, with a **dead battery in #5 and a charged one in slot 2**, #5 never charged, even across getting out and back in. Moving the dead battery into slot 2 made it charge.

**Cause:** with #5 empty and slot 2 non-empty, our `RobotBattery` getter returns slot 2 (correctly: the droid runs on it). Vanilla `ChargeRobot` therefore charges slot 2. The first `DroidSleeperPatch` only ever topped up slot 2, and skipped it because it was already `RobotBattery`. Nothing charged #5.

**Fix:** the sleeper now charges **both slot batteries explicitly**. A **prefix** captures `human.RobotBattery` (what vanilla is about to charge) into `__state`. It's captured *before* vanilla runs, because charging an empty #5 flips the getter's answer. The **postfix** then tops up the #5 battery and the slot-2 battery, each skipped only if it's the one vanilla charged. The rate and grid accounting are the same as for vanilla's suit battery. Compiled clean; install pending (game running).

**Errors this session:** the red `Could not cast to InstructionData : /instruction.xml` is vanilla `InstructionData.GetFromFile` (the IC10 script library) loading an entry with an empty folder path. It's also present in the pre-mod `Player-prev.log`. All three local scripts' `instruction.xml` files are valid, so it's probably a Workshop or cloud entry. Unrelated to this mod.

Installed the sleeper both-slots fix (MD5 `4e4b6dcc…`).

**Confirmed in-game:** with a dead #5 and a charged slot 2, the sleeper now charges #5. ✅

## 2026-10-05: Fourth test run

- ✅ **Labels show "Battery 1" / "Battery 2"** (user confirmed).
- ✅ All four patches attached. No exceptions from this mod. The four exceptions in the session are all Filter Cleaner's `UniTask.Delay` `MissingMethodException`; the two `instruction.xml` lines are vanilla.
- Diagnostic line: `HUD: source label 'Uniform', label components [RectTransform, CanvasRenderer, TextMeshProUGUI, LocalizedText]`. This confirms vanilla #5 reads "Uniform" on a droid. It also shows the label has a **`LocalizedText`** component. *Open:* that component probably re-applies its localized key when the game language changes (`Localization.OnLanguageChanged`), which would put "Uniform" back on both buttons until the HUD is rebuilt. Fix candidate: remove `LocalizedText` from the clone's label, and disable it on #5 while the player is a droid (re-enable and restore when not).

**User confirmed all of the following in-game (2026-10-05):**
- ✅ Labels read Battery 1 / Battery 2
- ✅ When battery 1 drains, the droid keeps running on battery 2
- ✅ Hot-swapping a fresh battery into battery 1 while running doesn't cut power
- ✅ Both batteries survive save → reload
- ✅ Die as a droid, respawn, and both batteries can be looted from the old body

**v0.2 is functionally complete for single-player.** Multiplayer (dedicated server + client join) is untested.

## Deferred: localized labels (user's design)

The language-change issue is a non-issue for now, but if it gets fixed, the user wants labels that read correctly in **every** language. Use the game's own localized name for the slot, plus a Greek letter: **"<localized Battery> α"** / **"<localized Battery> β"**.

Implementation notes:
- Vanilla already renames the droid slot with `StringKey = "Battery"`, and our slot uses the same key, so `Localization.GetName(slot)` (→ `SlotsName[StringHash]`) returns the localized "Battery" in the current language. No new translation strings are needed.
- Re-apply on `Localization.OnLanguageChanged` (a `System.Action` delegate the game already combines into, e.g. `HotkeyDisplay`).
- Remove `LocalizedText` from the clone's label, and disable it on the #5 label while the player is a droid. Re-enable it, and restore the original text, for humans and Zrilians, so it doesn't overwrite the custom text on a language change.

## 2026-10-05: Localized α / β labels

Implemented the user's design: **"<localized Battery> α"** on #5 and **"<localized Battery> β"** on the new button.

- **Text:** `Localization.GetName(slot)` on a key-only `Slot` with `StringKey "Battery"`. That's the same key vanilla gives the droid battery slot, so the game's own translation table (`Localization.SlotsName`) supplies the word in the current language and no new strings are needed. A missing translation returns `"<N:lang:key>"`, which falls back to "Battery".
- **Why not remove `LocalizedText`:** decompiling `Assets.Scripts.UI.LocalizedText` shows that `Refresh()` (run on `Awake` and on every language change via `Localization.Register`) both sets the text **and swaps the font** for the language (`Localization.CurrentFont`, needed for CJK glyphs). Removing or disabling it would break fonts. Instead there's a new **postfix on `LocalizedText.Refresh`** (`BatteryLabelLocalizationPatch`) that re-applies α/β after vanilla runs. The clone's label always gets β; #5 gets α only while the local player is a droid. When the player isn't a droid, `SetDroidMode(false)` calls the #5 label's `Refresh()`, which hands it back to vanilla ("Uniform") in the right language.
- Label logic moved to its own file, `Patches/BatteryLabels.cs`; `HudSlotPatch` just calls `Attach` and `SetDroidMode`.
- α/β were verified in the installed DLL. The decompiler printed bytes 0xE0/0xE1, which are α/β in the console's CP437 code page.

Installed (MD5 `bff37231…`). In-game check pending: labels read "Battery α / Battery β" in English.
