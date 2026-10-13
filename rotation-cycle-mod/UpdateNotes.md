# Salty's Rotation Cycle: running dev log

A running record of what was done and **why**, written as the work happens. Newest entries go at the bottom.

## 2026-10-13: Idea

Request: reorder the C key's orientation cycle so a part spins through every variation about its main axis, instead of whatever it does now. "We may need to map the orientations manually."

## 2026-10-13: What the game code says (decompile of Update 0.2.6428.27798)

- **C is `KeyMap.QuantityModifier`**, a hold modifier shared with stack splitting and more, so it isn't rebound. In `InventoryManager.PlacementMode` the first frame C is held calls `SmartRotate.GetNext` (`GetPrevious` with Shift), and each wheel notch while held calls one of them. Grid, Face and FaceMount placement all end up in `SmartRotate._GetNext`.
- **Why the order feels random:** `_GetNext` calls `_BestMatchCount`, which rotates the cursor through every orientation in the part's `ConnectionType` table and jumps to the first valid one with the most connections. The tables themselves (`RotationsList`) are fixed per ConnectionType; the 24-entry `Exhaustive`/`FaceAllAll` ones already do 3 world-Y spins then a tip, but the auto-pick hides that.
- **No rotation index is stored.** The current table index is recovered every call from the cursor's rotation (Euler angles rounded to 90°, applied to the prefab's `OpenEndsPermutation`). Only the final Quaternion is networked (`CreateStructureMessage.Rotation`) and saved. So the mod is client-side only.
- Rotations go through `ISmartRotatable.Rotate` → `Structure.Rotate`, which for Grid/Face with `RotationAxis.All` does `RotateAround(centerOfRotation, axis, angle)` in world space. Face placement pivots about the grid cell centre, which is what moves a face part between faces.

## 2026-10-13: Design (v0.1)

- **No hand-made table needed.** A Harmony prefix on `SmartRotate._GetNext` replaces the step with a generated 24-orientation cycle: for each main-axis direction (up, north, east, south, west, down), four quarter-turn spins about it. Main axis = local up for Grid parts, local forward (the mount face normal) for Face parts.
- **Stateless:** the current position in the cycle comes from the nearest of the 24 orientations to the cursor's rotation, so it works from whatever rotation the cursor starts in and corrects float drift (it rotates straight to the exact target).
- **Look-alikes skipped by default:** orientations that give the same open-end permutation as an earlier one (the game's own notion of "same", via `GetOpenEndLocationPermutation`) are dropped, so a straight pipe has 3 stops, not 24 with invisible spins. `ShowEverySpin` config turns that off.
- **Restricted parts** (RotationAxis not All, Grid+Floor-only, Face not All/All, FaceMount) would be rotated into orientations they don't allow, so they step through the game's own `RotationsList` table one entry per press, without `_BestMatchCount`.
- **Merging pipes/cables** (`IGridMergeable.WillMergeWhenPlaced`) keep the game's code untouched.
- Compile-checked on the PC against the real game DLLs (0 errors). Any exception falls back to the game's own smart rotate for that press.
- Trade-off: the base game's auto-pick of the best-connected orientation is gone.

## 2026-10-13: Per-part main axis

Feedback: one axis for every part is wrong. Tanks should spin about vertical, inline pipe devices (pumps, valves) about the pipe's length.

- The game has no "main axis" field. What it does store per part is `SmallGrid.OpenEnds` (`List<Connection>`): each `Connection` has `ConnectionType` (a `NetworkType` flag: Pipe, PipeLiquid, Chute, Power, Data, ...), `ConnectionRole` (Input/Output/...) and a `Transform` whose `forward` points out of the part into the neighbouring cell (that's how `GetFacingGrid` works). On the construction cursor these are live children of the cursor, so `InverseTransformDirection(end.Transform.forward)` gives a local direction per end.
- `MainAxis` rule: config override → the line through an opposite pair of pipe/chute ends (only if exactly one such line) → the line all pipe/chute ends sit on → the same two tests on every end → local up. The result is a positive local axis, so a bottom-port tank starts upright.
- The cycle is built per prefab and cached: for each world direction (up, N, E, S, W, down) the orientation that puts the main axis there, then four spins about it. Face parts keep local forward.
- Port layouts live in the asset bundles, not code, so the mod logs each prefab's ends and chosen axis once (`Main axis for …`) for checking after a play session. `AxisOverrides` config is the manual-mapping escape hatch.
- Review of the rule against the decompile: a tee has one opposite pair so it spins about its through-run (kept, it's the pipe line); a cross has two pairs, so it now uses the axis at right angles to both instead of local up (right however the prefab is modelled). Ends with a degenerate forward are skipped.
