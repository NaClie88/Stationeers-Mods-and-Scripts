# Salty's Yield Multiplier

Configurable multipliers for smelting and ice-melting yield — how many
ingots you get out of a Furnace/Advanced Furnace/Arc Furnace, and how much
gas comes out of melting ice — adjustable live from StationeersLaunchPad's
config UI, no restart needed.

## What it does

Two sliders, both default to 5x, both live-adjustable:

- **Ingot Yield Multiplier** — more ingots out of the same ore, across the
  basic Furnace, Advanced Furnace, and Arc Furnace.
- **Ice Gas Yield Multiplier** — more gas out of melting ice (Volatile Ice,
  Oxite, etc.), whether melted in-world or inside a Furnace.

Neither slider touches mining yield, world difficulty, or the Centrifuge.

## Why it works this way

Most "5x yield" mods on the Workshop implement this by adding a custom
world Difficulty tier with a mining-yield stat. That approach has two real
problems this mod was built specifically to avoid:

1. **It breaks Droid and Zrilian respawn.** Vanilla Stationeers decides
   what a Droid or Zrilian character gets on respawn by matching the
   world's Difficulty against the literal vanilla IDs (`Easy`/`Normal`/
   `Stationeer`). A custom Difficulty ID matches none of those branches, so
   the character respawns with an empty kit — no battery for a Droid, no
   suit for a Zrilian. This is exactly what happened on a test save before
   this mod existed, traced back to a popular yield mod doing exactly
   that.
2. **It's baked into the save at world creation**, and can't be applied
   retroactively to an existing world — swapping mods after the fact
   doesn't help a save already in progress.

This mod instead multiplies the smelting/melting *output*, not the mining
input — a live calculation applied every time you smelt or melt something,
never touching Species/Difficulty respawn logic and never dependent on
when a save was created. It works immediately on ore or ice you've already
got sitting in a base.

It was also built to specifically not create a duplication exploit: the
ingot-yield patches were verified against the game's own decompiled source
to confirm they hook code paths used *only* by the Furnace family, with
zero code or data shared with the Centrifuge's ore-splitting/"dirty ore"
recycling logic (which has its own deliberate material loss built in). A
naive implementation reusing the game's generic recipe-reading code could
have accidentally cheapened Centrifuge output too — this one can't.

## Investigated edge case (suspected vanilla behavior, unconfirmed — not
## caused by this mod either way)

While verifying the PureIce freeze/melt patch pair, code review turned up
a theoretical scenario in vanilla Stationeers: condensed-atmosphere ice
(`PureIce`) stacks are capped at 50 pieces, and if a single freeze event
needed more than 50 pieces to represent its gas quantity, the stack would
silently clamp and the excess mass would be destroyed. Digging further,
though, found that the game already chunks large freeze events at exactly
the boundary that prevents this (`AtmosphereHelper.RemoveStackWorthOfFrozenGas`
caps each chunk at exactly 2500 mol per gas type — precisely 50 pieces
worth) — so this looks like something the game was deliberately engineered
to avoid, not a live bug. Flagged here for transparency since it came up
during development, but downgraded from "known issue" to "investigated,
likely already prevented" — not something this mod causes or can fix
either way, since it's entirely in code this mod doesn't touch.

## Requirements

- [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad)

## Source

https://github.com/NaClie88/Stationeers-Mods-and-Scripts/tree/yield-multiplier-mod/ore-yield-multiplier-mod

## AI disclosure

I would never have been able to bring this mod to the Workshop using only
my subject matter expertise and my systems engineering skills — I lack the
coding and architecture knowledge base the AI partnership brings. This mod
was built in direct collaboration with Claude (Anthropic), including
decompiling the game's own code to verify the design before writing a
single patch. No apology for that — it's how this mod exists at all.
