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

### `airlock-ic10-scripts/`
- `ic10_failsafe_airlock_requirements.md` — full requirements spec for a
  custom IC10-scripted airlock: staged power-failure response with
  hysteresis, a 3-button system (including a chamber-interior button for
  someone caught inside during a power event), a Propped-Open state for
  matched-atmosphere connections, and a complete discrete-state
  enumeration so no edge case gets missed.
- `watcher.ic10`, `cycle.ic10`, `gas_sensor.ic10` — copy-paste-ready
  IC10-MIPS code, one file per chip, nothing else: an always-powered
  **Watcher** (Power Tier + Buttons), a **Cycle** chip powered only
  while active (Doors/Vent, gated by a Power Controller rather than a
  Transformer), and an optional **Gas Sensor** chip for the
  Propped-Open feature. Open the matching file in-game per chip.
  `ic10_airlock_scripts.md` is a one-page index to these three.
- `ic10_airlock_code_notes.md` — the explanations behind that code:
  design rationale, corrections, and dry-run verification, validated
  against a real production Workshop script (Custom Airlock V2 by
  CowsAreEvil) and tested in a real IC10 emulator. Marked clearly where
  remaining unknowns (`BtnHash`, the LED Color enum, an untested real
  wireless Transmitter/Receiver link) still need in-game confirmation.
- `ic10_airlock_setup_guide.md` — first-time build checklist: hardware
  list, wiring per chip/pin, Labeller naming steps for the three
  buttons, constants to verify before power-on, and a troubleshooting
  section.

### `logic-network-reference/`
Decompiled ground truth for what every device actually does over the
LogicType network — built after this project got burned more than once
trusting secondhand/community-sourced LogicType documentation (see its
own `README.md` for the specific examples). `ground-truth-database.md`
covers 120 device classes' real `GetLogicValue`/`SetLogicValue`/
`CanLogicRead`/`CanLogicWrite` implementations, extracted straight from
`Assembly-CSharp.dll`; `base-behavior.md` documents the shared
implementation most devices inherit; `devices/*.md` are hand-written,
narrative deep-dives for a handful of devices this project cares about
most (Power Controller, Door, Motherboards/Circuitboards, Logic
Transmitter). Already resolved one real bug in `watcher.ic10` (the
Power Controller `Charge`/`Maximum` misread) and one open design
question (the Power Controller's own output-gating LogicType).

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

## Repo organization

This is a monorepo for multiple independent Stationeers projects, not
one thing — currently the IC10 airlock scripts and the shared
`logic-network-reference`, with a C# Harmony mod (`airlock-card-mod`,
currently on its own `airlock-mod-card` branch, not yet merged) and a
planned Console-UI-bridge mod to follow. **Each mod is self-contained
in its own top-level folder** — own project file, own dependencies —
specifically so Steam Workshop packaging is just "zip that one
folder," independent of everything else here. Shared reference
material like `logic-network-reference` is the payoff of keeping
these together instead of splitting into separate repos per mod.

**Versioning:** git tags/releases are repo-wide, not folder-scoped, so
per-mod releases use a `<mod>-vX.Y` prefix (e.g. `airlock-card-v1.0`)
rather than plain semver tags, to keep each mod's version history
distinguishable in one shared tag list.

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

## Backlog — separate future mod, revisit at the end of airlock development

**A generic IC10-to-Console UI bridge card.** Raw IC10 circuits have no
screen — only networked Buttons/LEDs (confirmed via
`logic-network-reference`: Console/Motherboard UI runs through a
completely separate `ButtonCommands` dispatch system that IC10's
`GetLogicValue`/`SetLogicValue` mechanism can't reach at all — see
`logic-network-reference/devices/motherboards.md`). The idea (project
owner, 2026-08-06): a new Circuitboard, in the spirit of the vanilla
Computer/Programming Motherboard's own code-editing screen, but where
the code you write just maps out a button/page UI and exposes/reads
logic values to and from the network — giving *any* IC10 build a real
Console screen, not just this airlock. A separate mod from
`airlock-mod-card`, its own scope (a UI-description protocol, a
rendering layer, a way to get player input back into IC10 registers) —
explicitly not something to fold into the current airlock work. Revisit
once the IC10 scripts and `airlock-mod-card` both ship.

## License

Not yet set. Add one before publishing if you want to be explicit about
reuse terms — MIT is a common choice for the code/scripts, CC-BY for the
documentation, but that's your call.
