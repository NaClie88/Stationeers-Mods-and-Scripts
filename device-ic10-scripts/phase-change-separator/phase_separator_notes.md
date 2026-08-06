# Phase Change Separator — Design Notes & Setup Guide

**Original design for this project, not adapted from an external
source.** Requested as "even more functionality" beyond the
onboard-chip devices this folder started with — this is a general
multi-device automation system (chamber + Gas Sensor + Vent + drain
Valve), driven by an external IC Housing, same general category as
`airlock-ic10-scripts/`, not a single device's onboard slot. This
folder's scope has broadened to cover both — see the root
`device-ic10-scripts/README.md`.

## The pressure/temperature table is a placeholder — read this first

I could not find a reliable, complete per-gas condensation
pressure/temperature table through available tools. The Community
Wiki's "Phase Change Mechanics" page and the most detailed guide found
(a Substack article specifically about thermal gas separation) both
403'd on every fetch attempt — the same Cloudflare/bot-protection
block this project has hit repeatedly on wiki and long-form guide
pages. Search snippets surfaced exactly **one** semi-reliable data
point: Nitrogen condensing around **6000 kPa / -83.2°C (189.95 K)** —
itself search-aggregated, not a direct primary-source quote, so treat
even that one as a starting guess, not a confirmed fact.

**The `TargetPressure`/`TargetTemp` defines in `phase_separator.ic10`
use that Nitrogen figure as an illustrative placeholder — verify it,
and every other gas's numbers you want to add, against your own
in-game Stationpedia phase-change diagram before relying on this
script.** Shipping a plausible-looking but unverified table would be
worse than not having one at all — this project has been burned by
exactly that mistake before (see `logic-network-reference/README.md`'s
"Why this exists"). The framework below is solid; the numbers inside
it are not, yet.

## What this does

Holds a separation chamber at a target pressure and waits for it to
also reach a target temperature, then opens a drain valve to remove
the condensed liquid, holds it open briefly, closes it, and repeats.
Only Nitrogen's placeholder stage is wired up (`StageCount` is
implicitly 1 — see "Extending to multiple stages" below); the
structure is built to extend to a real per-gas table once the numbers
are verified.

## Hardware

- **A separation chamber** — any sealed volume the gas mixture flows
  through, sized to your setup. Not a specific device; this script
  doesn't care what the chamber physically is, only what it reads and
  drives.
- **`d0`: a Gas Sensor inside the chamber** — live `Pressure` and
  `Temperature` readings drive every decision this script makes.
- **`d1`: an Active Vent controlling the chamber's pressure** —
  `Mode 0` evacuates (lowers pressure), `Mode 1` pressurizes (raises
  it), confirmed convention already used elsewhere in this repo
  (`airlock-ic10-scripts/cycle.ic10`, `SOURCES.md`).
- **`d2`: a Valve (or Pump) on the chamber's liquid output** — opened
  once pressure and temperature both settle within tolerance, to
  drain the condensed liquid to wherever you're routing it.
- **A cooling loop is your own responsibility, not this script's.**
  This script does not attempt to directly drive a radiator/heat
  exchanger setup — those vary too much build-to-build to assume a
  specific interface, and IC10 has no simple universal "target
  temperature" control the way the Vent gives for pressure. Build
  your own passive or active cooling loop to bring the chamber near
  the target temperature; this script just **waits** for the Gas
  Sensor to confirm it's there before draining, the same way it
  actively drives pressure but only monitors temperature.

## Constants to adjust

- `TargetPressure` (kPa), `TargetTemp` (K) — the stage's setpoint.
  **Placeholder values, see above.**
- `PressureTolerance` (kPa), `TempTolerance` (K) — how close is "close
  enough" before considering the stage stable.
- `SettleTicks` — how many consecutive ticks both readings must stay
  within tolerance before the drain opens. Prevents draining on a
  brief, noisy blip.
- `DrainTicks` — how long the drain valve stays open per cycle.

## Extending to multiple stages

Real multi-gas separation usually needs sequential stages — condense
out the highest-condensation-point gas first, drain it, adjust to the
next gas's setpoint, drain that, and so on until what's left is your
target output. This first version only implements one stage
end-to-end since the underlying data isn't verified yet — extending it
is mechanical, not a redesign: replace the single `TargetPressure`/
`TargetTemp` defines with a small pushed table (same static-data-on-
the-stack idiom `filtration/onboard_filtration.ic10` uses for its
filter-hash list) indexed by a stage counter, and advance the counter
after each successful drain cycle instead of looping back to the same
stage forever. Worth building once there's a real, verified table to
drive it with — not before.

## Known limitations

- **No stall handling.** If the Vent can never reach target (e.g. the
  chamber is disconnected from a network large enough to supply the
  pressure swing), this script will keep adjusting indefinitely rather
  than giving up or alerting. Same category of gap flagged elsewhere
  in this repo's other scripts — deliberately not built here yet,
  since unlike the airlock (which was replicating vanilla's own
  documented Cancel-button behavior), there's no existing precedent
  to match for this script, and adding speculative stall-detection
  logic before it's needed would be exactly the kind of unrequested
  complexity this project tries to avoid.
- **Single stage only** — see "Extending to multiple stages" above.
- **No optional alarm/status output** — unlike the Air Conditioner and
  Filtration scripts, this one doesn't yet expose an optional light or
  alarm pin. Straightforward to add the same `brdns`-graceful-
  degradation way if useful once this sees real use.
