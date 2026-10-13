# Salty's Rotation Cycle

A Stationeers mod (StationeersLaunchPad + LaunchPadBooster) that makes the **C** key's rotation cycle predictable while you place a structure.

## What it changes

In the base game, tapping **C** while placing ("smart rotate") tries every orientation the part allows and jumps to the first valid one that connects to the most things around it. The order you see depends on what's nearby, so it feels random.

With this mod, every tap goes to the next orientation in a fixed order:

1. **Spin about the main axis** a quarter turn at a time (4 taps).
2. **Tip the main axis** to the next direction and spin again: up, north, east, south, west, down.

The main axis is the part's own "up" for parts that snap to the grid (pipes, cables, most devices), and the face it is mounted on for face-placed parts.

- **Shift+C** goes backwards through the same order.
- **Hold C and scroll** steps forwards or backwards.
- Orientations that look identical to the game, such as spinning a straight pipe about its own length, are skipped. Set `ShowEverySpin = true` in the mod's config to stop at all 24.
- Parts that can't turn on every axis (wall-only, floor-only, one-axis or wall-mounted parts) step through the game's own orientation list one entry per tap, without the jump-to-most-connections behaviour.
- A pipe or cable that will merge into the one you're aiming at keeps the game's behaviour.
- The other rotate keys (Insert/Delete/Home/End/PageUp/PageDown) are unchanged.

You lose the base game's "auto-pick the orientation with the most connections". The fixed order is the trade.

## Multiplayer

Client-side only. The game sends just the final rotation when you place something, never a cycle position, so other players and the server don't need the mod.

## Installing

Close Stationeers, run `build-and-install.sh`, then add the mod to your LaunchPad profile (see the repo's `CLAUDE.md`).

## Sources and credits

- Behaviour found by decompiling `Assembly-CSharp.dll` (Update 0.2.6428.27798): `SmartRotate._GetNext` / `_BestMatchCount` and `InventoryManager.PlacementMode`. Decompiled source is not included.
- Project skeleton from Salty's Ceiling Solar and Salty's Fabricator Overflow.
