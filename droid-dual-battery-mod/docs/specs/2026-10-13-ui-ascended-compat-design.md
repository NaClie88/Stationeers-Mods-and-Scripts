# Design: Stationeers UI Ascended compatibility (battery β on the HUD)

Date: 2026-10-13. Status: approved in chat, awaiting written-spec review.

## Goal

When **Stationeers UI Ascended** (Workshop 3776545141, ModID `StationeersUIAscended`, plugin GUID
`com.stationeersuimod.ui`, v1.0.3, by FlorpyDorp + JXSN) is loaded, a droid player can **see battery β on
the HUD and swap it**, without β sitting in line with the six equipment boxes. β is shown as a
**child of #5**, the way UI Ascended shows a bag's or jetpack's contents.

Success: as a droid with UI Ascended active, the player can see β's battery and charge, drag a battery
onto β, and take/install β from the #5 wheel. Humans and Zrilians see no change. Without UI Ascended,
the mod behaves exactly as v0.2 does today.

## What already works (no change needed)

Read from UI Ascended's decompiled source (kept in a scratchpad, never committed):

- **Battery % / low-power warning:** `HudSampler.Sample` reads `human.RobotBattery`, which our
  `RobotBatteryPatch` already redirects to β when α is empty or flat.
- **Universal Inventory:** `InventoryScanner.Scan` walks every entry of `parentHuman.Slots`, so the
  appended β slot is already listed there.

## What breaks today

- UI Ascended's `HideVanillaClothing` (default on) hides the vanilla clothing panel, which hides our
  cloned β button with it.
- Its `EquipmentColumnWidget` is hard-coded to six boxes (`SlotFor` index 0..5, #5 = `UniformSlot`).
  On droids it labels #5 "BATTERY". β has nowhere to appear.
- Its #5 key wheel (`EquipmentKeyRadialFeature("UniformSlot", ...)`) only manages the #5 occupant, and
  `CanOpen` is false when #5 is empty.

## Design

### 1. β box attached to #5 (HUD)

Precedent: UI Ascended attaches a "Meson tag" panel beside the glasses box for the Meson Scanner mod
(`EquipmentColumnWidget.UpdateMesonTag` / `LayoutMesonTag`). We do the same for #5.

- **Build:** for each `EquipmentColumnWidget` instance (tracked in a `ConditionalWeakTable`, so a
  theme reload or F9 rebuild that recreates the widget gets a fresh β box), clone box index 4's
  `Panel`, `Icon` and `Label` GameObjects under the same root. Clones inherit the theme's panel,
  glass and text styling.
- **Layout** (postfix on `Layout(float)`): size = 0.6 × #5's box size. Put it on the side of #5 away
  from the screen centre, the same side rule `LayoutMesonTag` uses. For a vertical column, use the
  right side when the column is on the left half of the screen, otherwise the left side. For a
  horizontal row, put it above or below. Hidden whenever box 4 is hidden (the theme's
  `first`/`count` slice can exclude #5).
- **Update** (postfix on `UpdatePanel(HudSnapshot, float)`), droids only (`HudSnapshot.IsRobot`),
  otherwise hidden:
  - colours copied from box 4 each frame, dimmed (fill α × 0.55, border α × 0.45) when β is empty,
    matching UI Ascended's empty-box look
  - icon = β battery thumbnail
  - label = `β NN%` from `BatteryCell.CurrentPowerPercentage` (networked, so correct on clients), or
    `β` when empty
  - box 4's label overridden from "BATTERY" to "BATTERY α"
- **Drop target** (postfix on `CollectDropZones(List<HudDropZone>, HudSnapshot, float)`): find the
  zone the original added for `UniformSlot` and add a zone for the β slot with the same rect, scaled
  and offset to the β box. Its `Label` is `"BATTERY β"`. UI Ascended's own drag/drop code then moves
  items into β through the game's server-checked path.

### 2. β as "contents" of the #5 wheel (swap)

- **`EquipmentKeyRadialFeature.BuildRoot` postfix**, only when the feature's `_buttonName` is
  `"UniformSlot"`, the local human is a droid, and β exists:
  - α present: append `ItemMenuBuilder.BuildSlotEntry(βSlot)` after vanilla's entries.
  - α empty: the result is empty, so return `[BuildSlotEntry(αSlot), BuildSlotEntry(βSlot)]`.
  - `BuildSlotEntry` is UI Ascended's own builder, used for a bag's slots. An occupied slot gives
    "Take to hand", a drop resolver and options; an empty slot gives a stow entry for what's in hand.
- **`EquipmentKeyRadialFeature.CanOpen` postfix**, same conditions: also true when β is occupied, so
  the wheel opens with α empty.

### 3. Wiring and failure handling

- New file `Patches/UiAscendedCompat.cs`. **No compile-time reference** to UI Ascended. Its types and
  members are resolved by name (`AccessTools.TypeByName`, `AccessTools.Field`/`Method`, `Traverse`)
  and patched with `harmony.Patch(...)`.
- **When to patch:** load order between LaunchPad mods isn't guaranteed, so UI Ascended's types may not
  be loaded at our `Awake`. Patching happens once, on the first `HudSlotPatch.Postfix` (the first HUD
  build in a world), guarded by a static flag. If `com.stationeersuimod.ui` isn't loaded, log
  `UI Ascended not loaded -- compat skipped` once and do nothing.
- **One log line per hook** on success or failure (repo rule: every Harmony patch is verified
  in-game by a log line).
- **Fail-safe:** every postfix body is wrapped in try/catch. On the first exception, log it once and
  disable that hook (static flag), so a UI Ascended update that renames something can't spam the log
  or break its HUD. Battery logic, the vanilla β button and the labels are untouched.
- Main thread only (HUD and radial code), so Unity object APIs are safe. Nothing runs on the
  life-tick thread.
- The vanilla cloned β button is unchanged. UI Ascended hides it with the clothing panel; if the user
  turns that hiding off, both show.

## Out of scope

- A hotkey for β, or cycling with the 5 key.
- A β entry in UI Ascended's F9 HUD Designer (no new designer element or props).
- Changing UI Ascended's files, or anything for non-droid species.

## Testing (in-game; no automated test harness for game mods)

1. Add UI Ascended to the LaunchPad "My Mods" profile (game closed, backup first, CRLF kept). It's
   currently absent, so it's force-disabled.
2. `LogOutput.log`: one success line per UI Ascended hook; no exceptions from this mod in `Player.log`.
3. Droid: β box visible beside #5 in each shipped theme. Its charge % updates as β drains. #5 reads
   "BATTERY α".
4. Drag a battery from hand or Universal Inventory onto the β box: it installs.
5. Tap 5 with both installed: the wheel shows α (take to hand) and β. Take β to hand, then install it
   back via the β stow entry. With α empty, 5 still opens the wheel.
6. Human or Zrilian character: no β box and no extra wheel entry.
7. Remove UI Ascended from the profile: log says compat skipped, and the vanilla β button works as in
   v0.2.
