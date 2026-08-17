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
- `elemental_lifecycle.md` — where every element/substance in the game
  actually comes from and goes to (ore/ice acquisition, smelting,
  electrolysis, phase change, combustion, recycling, farming), confirming
  mass is conserved everywhere except one documented vanilla soft spot
  (farming/composting). Tags every claim as locked-in, new-since-the
  2026-03-19 Gases Update, a known abstraction, unconfirmed, or explicit
  speculation. Has three open follow-ups (Silanol/Ozone/Hydrochloric Acid
  acquisition — see its own §9) — see "Planned" below.
- `trade_economy.md` — companion to the above: which trader actually buys
  what (traders don't buy back what they sell — matching goods to the
  right sink matters more than the intuitive trader name), a
  fuel-first → mining → refining profit progression sourced from a
  community quantitative case study, the landing-pad-size bulk-multiplier
  lever, and the post-Gases-Update sell-list additions (Salt, Helium,
  Hydrogen, Silanol, Ozone, and four new liquids) most guides predate.
  Several trader buy/sell details are flagged ❓ unconfirmed — see its own
  §2 — and see "Planned" below for the automated-selling follow-up.

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

### `device-ic10-scripts/`
Curated IC10 scripts for devices with their own **onboard** IC chip
slot (Air Conditioner, Filtration, etc.) — a different category from
`airlock-ic10-scripts/`'s freestanding IC-Housing design. One
subfolder per device, each with a setup guide and a citation trail in
this folder's own `SOURCES.md`. **Not everything here is original
work** — several scripts are adapted from community sources, clearly
marked per-script. Living collection, built out device by device, not
delivered as a complete survey.

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
`logic-network-reference`, with two C# Harmony mods in progress on
their own branches, not yet merged: `airlock-card-mod` (branch
`airlock-mod-card`) and `console-UI-mod` (branch `console-ui-mod`).
**Each mod is self-contained in its own top-level folder** — own
project file, own dependencies —
specifically so Steam Workshop packaging is just "zip that one
folder," independent of everything else here. Shared reference
material like `logic-network-reference` is the payoff of keeping
these together instead of splitting into separate repos per mod.

**Versioning:** git tags/releases are repo-wide, not folder-scoped, so
per-mod releases use a `<mod>-vX.Y` prefix (e.g. `airlock-card-v1.0`)
rather than plain semver tags, to keep each mod's version history
distinguishable in one shared tag list.

**Compatibility layering (project owner, 2026-08-06):** every mod here
must work fully standalone against vanilla first — no mod is allowed
to require another mod to function. Only after that does cross-mod
interoperability (with other mods in this repo, or external ones like
Re-Volt) get built, and when it does, that compatibility code and its
reference material live in a subfolder inside the mod that depends on
it — never assumed by that mod's core functionality, and never
scattered elsewhere. Steam Workshop packaging of a mod's base folder
stays clean and dependency-free regardless of how much compatibility
work exists underneath it.

**Shelve, don't discard (project owner, 2026-08-07):** when work gets
built on a wrong premise — a misread requirement, a hardware
assumption that turns out false — but the underlying idea is still
conceptually useful, file it away instead of deleting it. The
design/implementation cost is already spent, and storage is cheap;
revert the code from wherever it was wrong, but keep the actual
attempt (exact code, the reasoning, what would need to be true to
revive it) somewhere findable — a `shelved_*.md` file next to the
work it came from is the pattern used so far (see
`device-ic10-scripts/phase-change-separator/two-chamber-system/shelved_display_ideas.md`
for a worked example). Applies to any project in this repo, not just
where it started.

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
- `guide/elemental_lifecycle.md` open follow-ups (added 2026-08-17):
  confirm the actual acquisition/synthesis method for **Silanol** and
  **Ozone** (hypothesis: player-synthesized rather than mined — not yet
  confirmed either way), and confirm whether **Hydrochloric Acid** (source
  already confirmed: Venus's atmosphere) has any crafting use beyond being
  a hazard gas. Wiki pages to check are listed in that doc's §9 — needs
  either an in-game Stationpedia check or the same decompile treatment
  this project already gave `Furnace`/`Ore.Smelt`/`Centrifuge`.
- `guide/trade_economy.md` open follow-ups (added 2026-08-17): confirm
  in-game what the **Ore Trader** actually buys (research only confirmed
  its sell side), and the buy/sell specifics for the **Food, Hydroponic/
  Seed, Hardware, Consumable, and Appliance Traders** (all five came up
  empty in research — see that doc's §2 table). Also unconfirmed: whether
  **Far-tier trades pay better than Close-tier**, or whether any
  reputation/unlock system affects pricing over time. Longer-term: build
  an actual **automated IC10 trade-terminal script** (see that doc's §3 —
  a community case study found manual selling unsustainable at the volumes
  that matter), which would also be the natural place to benchmark whether
  Hydrogen's new liquefied/clean-combustion profile beats the classic
  Methane fuel-trade route referenced in that doc.

## In progress: `console-UI-mod`

Started 2026-08-06 (originally logged below as a backlog idea to
revisit later, but begun early since the project owner has spare time
before in-game testing access on the other branches). See that
branch's `console-UI-mod/README.md` for the plan — a generic
IC10-to-Console UI bridge card, since raw IC10 circuits have no screen
at all (confirmed via `logic-network-reference`: Console/Motherboard UI
runs through a completely separate `ButtonCommands` dispatch system
that IC10's `GetLogicValue`/`SetLogicValue` mechanism can't reach —
see `logic-network-reference/devices/motherboards.md`). A separate mod
from `airlock-card-mod`, its own scope (a UI-description protocol, a
rendering layer, a way to get player input back into IC10 registers).
Design not yet decided — currently just the problem statement and open
questions, same process `airlock-card-mod` went through before any
code got written.

## License

Not yet set. Add one before publishing if you want to be explicit about
reuse terms — MIT is a common choice for the code/scripts, CC-BY for the
documentation, but that's your call.
