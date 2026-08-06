# Device IC10 Scripts

Curated IC10 scripts for useful automation beyond the airlock design —
started with devices that host their **own onboard IC chip slot** (Air
Conditioner, Filtration), broadened to general multi-device automation
systems too (phase change separation), as long as it's a genuinely
useful script with a real setup guide and honest sourcing. One
subfolder per device or system.

## Not everything here was written by this project

**Important:** several of these scripts are adapted from existing
community sources (GitHub repos, Steam Workshop, forum posts), not
written from scratch here. Every script's header comment names its
source, and the full citation trail lives in `SOURCES.md` in this
folder — same discipline the rest of this repo already uses (see the
root `SOURCES.md` for the airlock design). Where a script has been
changed from its source (bug fixes, added features), the per-device
notes file explains exactly what changed and why, quoting the
original where relevant. Nothing here is presented as this project's
own original work unless its citation says so.

## Status: living, growing collection — not exhaustive

This is being built out device by device, not delivered as a complete
survey. Confirmed so far:

| Device | Real class | Status |
|---|---|---|
| Air Conditioner | `ThingStructureAirConditioner` | Done — see `air-conditioner/` |
| Filtration | `ThingStructureFiltration` | Done — see `filtration/` |
| Filtration Liquid | `ThingStructureFiltrationLiquid` | Not started |
| Portable Air Conditioner | `ThingDynamicAirConditioner` | Not started |
| Rocket Gas Filtration | `ThingStructureRocketFiltrationGas` | Not started |
| Phase Change Separator (multi-device system, not a single device) | n/a | **Two variants done** — see `phase-change-separator/`. Real Stationpedia-derived data backs 11 gases (`condensation_reference.md`). A single-chamber/Active-Vent variant (`phase_separator.ic10`) and the real two-chip Condensation/Evaporation Chamber system (`two-chamber-system/`, dial-selected target gas, AC-driven heat exchange) matching the project owner's own proven manual build. Precooling (skimming easy condensate before full separation) documented as a pattern reusing the existing Air Conditioner script, no new code needed. |

**Separately, a distinct category worth its own pass later:**
fabrication automation. Autolathe, Electronics Printer, Hydraulic Pipe
Bender, Tool Manufactory, Security Printer, and Rocket Manufactory all
expose Stack-based instructions an *external* IC10 (via IC Housing,
same pattern as `airlock-ic10-scripts/`) can read/write to automate a
build queue — a different mechanism from the onboard-chip-slot devices
this folder currently focuses on, not yet researched here.

## Structure

Each device gets its own subfolder:
- `<script>.ic10` — the actual script, ready to paste in-game. This is
  the maintained, working copy — fixes and complementary features
  (clearly flagged as such, see below) land here.
- `<script>_notes.md` — setup guide (hardware, wiring, constants to
  adjust) plus any bug fixes or added features versus the cited
  source, if applicable.
- `original/<source-filename>` — **only present when the script is
  adapted from an external source.** A pristine, unmodified copy of
  whatever was pulled from the cited source, kept as a permanent
  diff/citation reference. **Set read-only at the filesystem level
  (`chmod 444`) as a safety net against accidental edits** — but that
  permission bit is local-only: git tracks the executable bit, not
  read/write permissions, so a fresh clone won't come back read-only
  automatically. The real, durable protection is this convention
  itself: **never edit anything under `original/` in place.** If a
  source needs a fix, edit the maintained copy one level up and
  explain the change in that device's `_notes.md`, the same way
  `air-conditioner/ac_thermostat_notes.md` documents its bug fix
  against `air-conditioner/original/air-conditioner-controller.ic10`.
