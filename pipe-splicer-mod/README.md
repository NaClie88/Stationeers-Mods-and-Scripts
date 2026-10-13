# Salty's Pipe Splicer

A Stationeers mod (StationeersLaunchPad + LaunchPadBooster) that lets you put a junction into the middle of a long straight pipe or cable.

## What it changes

The 3, 5 and 10-long straights can't be wrench-merged in the base game: the game's merge would replace the whole run with one single-cell junction. With this mod, wrench-merging a piece onto any cell of a long straight:

1. builds the junction in that cell, exactly as a normal wrench-merge onto a single straight, and
2. rebuilds the rest of the run on either side from the same kind of straight (10, 5, 3 and 1 long), with the same rotation and paint.

Works for gas, liquid and insulated pipes, and for normal, heavy and super-heavy cables.

- **Gas stays in the pipe.** Nothing vents. The run keeps the same number of cells, so the network's volume and pressure end up where they started. The game shares a network's gas by volume when pieces change, and the new pieces go in during the same frame as the old one comes out.
- **Power stays on.** The cable network is rebuilt within a single frame, between power ticks.
- **Cost:** long straights are cheaper than the same number of singles, so splitting one costs the difference, taken from the kit in your hand. The mod picks the cheapest mix of lengths. If the kit is short, the build outline turns red and says how many more you need. The junction itself costs what a normal merge costs.

## Multiplayer

Required on the host and every client: the host does the rebuilding, and each player's build outline has to accept the merge.

## Installing

Close Stationeers, run `build-and-install.sh`, then add the mod to your LaunchPad profile (see the repo's `CLAUDE.md`).

## Sources and credits

- Behaviour found by decompiling `Assembly-CSharp.dll` (Update 0.2.6428.27798): `MultiMergeConstructor.Construct`, `Piping`/`Cable.CanReplace` and `_IsCollision`, `Pipe.OnDestroy`, `NetworkAtmosphereEvent`, `Constructor.SpawnConstruct`. Decompiled source is not included.
- Project skeleton from Salty's Rotation Cycle.
