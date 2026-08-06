# Device IC10 Scripts

Curated IC10 scripts for devices that host their **own onboard IC chip
slot** — Air Conditioner, Filtration, and similar — as opposed to
`airlock-ic10-scripts/`, which is a freestanding multi-chip design
wired to separate IC Housings. One subfolder per device.

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
| Filtration | `ThingStructureFiltration` | Queued — a promising script ("The Ultimate Filtration IC10") found but not yet retrievable, see `SOURCES.md` |
| Filtration Liquid | `ThingStructureFiltrationLiquid` | Not started |
| Portable Air Conditioner | `ThingDynamicAirConditioner` | Not started |
| Rocket Gas Filtration | `ThingStructureRocketFiltrationGas` | Not started |

**Separately, a distinct category worth its own pass later:**
fabrication automation. Autolathe, Electronics Printer, Hydraulic Pipe
Bender, Tool Manufactory, Security Printer, and Rocket Manufactory all
expose Stack-based instructions an *external* IC10 (via IC Housing,
same pattern as `airlock-ic10-scripts/`) can read/write to automate a
build queue — a different mechanism from the onboard-chip-slot devices
this folder currently focuses on, not yet researched here.

## Structure

Each device gets its own subfolder:
- `<script>.ic10` — the actual script, ready to paste in-game.
- `<script>_notes.md` — setup guide (hardware, wiring, constants to
  adjust) plus any bug fixes or added features versus the cited
  source, if applicable.
