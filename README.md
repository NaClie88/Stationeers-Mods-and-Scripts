# Stationeers Mods and Scripts

A complete progression framework for Stationeers — vanilla-first, with
optional mod-enhanced configurations layered on top — plus a from-scratch
IC10 fail-safe airlock design with prototype code and full source
citations.

**Vanilla is the default, fully self-sufficient build.** Nothing here
requires a mod. Modded configurations (currently: Re-Volt) are tracked
separately and layered on as an optional variant for anyone who has them
installed — most people this gets shared with won't, so vanilla always
stands on its own.

## Contents

### `guide/`
- `stationeers_progression.md` — the full progression guide. 32+ projects
  across 16 lettered categories (A–P), ordered by actual survival urgency
  rather than theme. Covers Day 1/Night 1 bootstrap (both Normal and
  Brutal starts), a Minimum Viable Base milestone, room-standard tiers,
  environment-specific priority shifts per world, high-pressure
  structural guidance, and more.

### `ic10-airlock/`
- `ic10_failsafe_airlock_requirements.md` — full requirements spec for a
  custom IC10-scripted airlock: staged power-failure response with
  hysteresis, a 3-button system (including a chamber-interior button for
  someone caught inside during a power event), a Propped-Open state for
  matched-atmosphere connections, and a complete discrete-state
  enumeration so no edge case gets missed.
- `ic10_airlock_prototype_code.md` — complete IC10-MIPS code for the
  3-chip design: an always-powered **Watcher** (Power Tier + Buttons),
  a **Cycle** chip powered only while active (Doors/Vent, gated by a
  Power Controller rather than a Transformer — see doc for why),
  and an optional **Gas Sensor** chip for the Propped-Open feature.
  Validated against a real production Workshop script (Custom Airlock
  V2 by CowsAreEvil) and dry-run tested in a real IC10 emulator. Marked
  clearly where remaining edge cases (stall recovery, mid-prop mismatch
  ordering) still aren't handled.
- `ic10_airlock_setup_guide.md` — first-time build checklist: hardware
  list, wiring per chip/pin, Labeller naming steps for the three
  buttons, constants to verify before power-on, and a troubleshooting
  section.

### `database/`
Structured data backing a future per-world/per-difficulty guide
generator (not yet built — the data model is ready, generation is the
next step):
- `worlds.json` — 7 worlds with hazard profiles, priority adjustments,
  radiator requirements
- `starts.json` — Normal and Brutal starting-condition data
- `recipes.json` — craftable item costs (ingredients, power draw), with
  a `source` field separating vanilla from mod content
- `project_requirements.json` — join table linking guide projects to
  their recipe costs
- `mods.json` — mod registry (currently: Re-Volt), tracking what each
  mod adds and what new goals it enables, kept fully separate from the
  vanilla recipe data
- `core_tiers.md` — the universal Tier 0–3 project content shared across
  all worlds
- `generate_guides.py` — the generator script that stitches world data +
  universal content into a combined guide
- `RESOURCE_DB_README.md` — how the pieces fit together

### `SOURCES.md`
Every non-obvious claim across the IC10 design traces to a real,
checkable URL here — Community Wiki pages, GitHub repos, Steam Workshop
scripts, official patch notes, and community discussion threads.
Organized by topic. If something in the two IC10 files doesn't have a
matching entry here, it hasn't been properly sourced yet.

## Status

Actively developed. Several things are explicitly flagged as
unconfirmed or skeleton rather than finished — see the "In-Game
Verification Checklist" in the requirements doc and the "What's
genuinely done vs. still a skeleton" section in the prototype code doc.
This is deliberate: claims are marked by confidence level throughout
rather than presented as uniformly finished.

## Planned

- Regenerate the 7 per-world guides under the current lettered
  numbering (an earlier version exists but is stale — built before a
  later renumbering pass)
- An example creative-mode Stationeers world demonstrating the build,
  published alongside the code
- Fill in the resource-cost database enough to actually run the
  generator end-to-end

## License

Not yet set. Add one before publishing if you want to be explicit about
reuse terms — MIT is a common choice for the code/scripts, CC-BY for the
documentation, but that's your call.
