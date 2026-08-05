# Gap Analysis — Vanilla Advanced Airlock Circuitboard vs. This Design

Point of this doc: confirm what vanilla already does well enough that
we don't reimplement it, and isolate exactly what's actually new. This
is what makes "patch the existing card" viable instead of building
from scratch.

Vanilla facts below are sourced from the Community Wiki (search
snippets — the live pages block automated fetches for this project,
same as everywhere else in this repo; verify directly once you're at
your PC and can check Stationpedia/the actual console UI). See
"Sources" at the bottom.

## What vanilla's Advanced Airlock Circuitboard already does

- Automates cycling between two environments ("interior"/"exterior"),
  same concept as this project's `Cycle` chip.
- Pressurize/Evacuate against a configured target pressure
  ("Internal #Pa"/"External #Pa" settings) — directly equivalent to
  this project's `TargetInt`/`TargetExt` constants and the
  `pressurize`/`evacuate` branches in `cycle.ic10`.
- **Stall handling, and a Cancel button** — if a phase can't reach
  target, it stalls, and a "Cancel Pressurize" button on the console
  lets a player skip it. **This is something our own docs flagged as a
  known gap** (`ic10_airlock_setup_guide.md` section 9: "No
  stall-timeout... the game's own 'Cancel Pressurize' button aren't
  handled in script") — vanilla already has this covered. One less
  thing to build.
- **Lock persists through power loss** — the mechanical property this
  whole project's fail-safe design exists to respond to in the first
  place (see `ic10_failsafe_airlock_requirements.md`).
- Console Slaves — multiple consoles can sync to one "master," for
  multi-entrance setups. This project's IC10 design has no equivalent
  (single Console assumed) — not a gap to close, just a vanilla
  capability to make sure a patch doesn't accidentally break.

## What this design adds that vanilla doesn't have

This is the actual scope of new work — everything else above, we
inherit for free by patching instead of replacing.

1. **Tier-based power monitoring with hysteresis** (Normal/Low/Critical,
   driven by a dedicated Power Controller's `Charge`/`Maximum`).
   Nothing found in vanilla's documented behavior resembles this — the
   "lock persists through power loss" property is passive (a
   mechanical fact about the door), not an active monitored response
   with staged behavior. This is `watcher.ic10`'s entire job, and it's
   wholly new.
2. **The chamber-interior Button C override** — someone caught inside
   during a forced Critical-tier evacuation can hold Button C to skip
   it. This is the specific, motivating problem this whole project was
   built to solve (see `SOURCES.md`: the Steam Community "Unlock (not
   open) airlock door when the power is cut" discussion, which
   confirms vanilla has no native answer to this at all). Wholly new.
3. **Propped-Open state** — both doors held open when a Gas Sensor
   pair confirms matched atmosphere across the airlock, avoiding
   needless cycling. Not found in vanilla's documented behavior.
   Wholly new, and already optional/isolated in this project's own
   design (a separate `gas_sensor.ic10` chip) — cleanly separable as
   an add-on here too.
4. **The warning LED as a Tier indicator** — vanilla's console
   presumably has its own status UI, but not a player-facing physical
   light cycling green/yellow/red with Tier. Minor, but new.

## What this means for the patch

Everything in "what vanilla already does" stays untouched — the patch
should **add** behavior around the existing cycling logic, not replace
it. Concretely: extra checks that run *before* or *around* the
existing pressurize/evacuate/lock logic (a Tier check that can force a
lock/evacuate sequence in Critical, an override read for Button C, an
optional Propped-Open bypass), not a rewritten cycle state machine.
The exact attachment points (which method to `Prefix`/`Postfix`) are
still Milestone 1.5's job — this doc fixes *what* needs to attach,
`fail_safe_logic.cs` in this same folder is the *how* (game-independent
so it's ready the moment real hooks are known).

## Sources

- Community Wiki, "Circuitboard (Advanced Airlock)" — cycling,
  interior/exterior settings, Console Slaves, stall behavior (search
  snippet).
- Community Wiki, "Circuitboard (Airlock)" — basic-vs-advanced
  distinction (search snippet).
- Community Wiki, "Guide (Airlock) Atmosphere to Atmosphere" — Stalled
  phase and the "Cancel Pressurize" button, confirmed by name (search
  snippet). Already cited in this project's `SOURCES.md` for the IC10
  side, re-cited here for the mod-card side.
- `ic10-airlock/ic10_failsafe_airlock_requirements.md`,
  `ic10_airlock_setup_guide.md`, `ic10_airlock_code_notes.md` — this
  project's own design and its already-documented known gaps.
