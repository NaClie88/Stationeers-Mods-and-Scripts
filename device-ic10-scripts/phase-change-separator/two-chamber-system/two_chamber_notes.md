# Two-Chamber Phase Separator — Design Notes & Setup Guide

Automates a manual process the project owner already had working —
a Condensation Chamber / Evaporation Chamber pair, sharing gas and
liquid lines, with the pair's heat-exchange port driving temperature
via an external Air Conditioner. Two paired chips, each independently
useful, documented here as a set.

## Why two chips, not one

- **Chip A (`separator_ac_driver.ic10`) must run on the Air
  Conditioner's own onboard chip slot** — that's the only way to
  reach the AC's `db` self-reference and drive `Setting`/`Mode`
  directly, same as `../../air-conditioner/ac_thermostat.ic10`.
- **Chip B (`separator_sequencer.ic10`) needs 5 device connections**
  (Gas Sensor, source pump, Condensation Valve, Purge Valve, dial) —
  the AC's onboard slot only has two free pins, confirmed nowhere
  near enough. It has to run in a separate IC Housing.
- **One shared dial, read by both** — no Transmitter bridge needed
  the way the airlock's Watcher/Cycle split requires one. That split
  exists because Watcher and Cycle sit on two different power
  circuits; there's no equivalent power-domain separation here, so
  both chips can just wire to (or batch-address) the same physical
  dial directly.

## Hardware

- **Condensation Chamber + Evaporation Chamber**, sharing a gas line
  and a liquid line between them, per the project owner's own proven
  manual setup.
- **An Air Conditioner, wired into the Chamber pair's heat-exchange
  port** — this is what actually controls temperature; Chip A drives
  it.
- **A Gas Sensor** on the gas side, feeding Chip B's pressure reading
  (`d0`).
- **A pump from your source/waste tank into the gas side** (`d1`) —
  tops up pressure as the target gas condenses out and gets removed.
- **A Condensation Valve** (`d2`) — one-way, moves liquid from the gas
  side to the liquid side once it forms. Low risk to pulse
  periodically even when nothing's condensed; it only ever moves
  liquid, so an empty attempt is a no-op.
- **A Purge Valve** (`d3`) — clears gas that ends up on the liquid
  side back to the gas side. **Used briefly and infrequently on
  purpose** (`PurgeInterval`/`PurgeHoldTicks` in the script) — the
  Community Wiki warns this valve can trigger unwanted re-evaporation
  if run too aggressively, potentially flooding/bursting pipes. Don't
  shorten the interval without understanding that risk.
- **Optional: a Logic Dial** (`d4` on Chip B, `d0` on Chip A) — sets
  the target gas index live. See `../condensation_reference.md` for
  the index table. Skip it entirely and edit each script's
  `DefaultGasIndex` instead if you'd rather not build one — see
  "Switching target gas" below for the tradeoff.
- **Optional: a display showing the current selection.** Not built
  yet — see "Display, not yet built" below.

**Pin map:**

| Chip | Pin | Device |
|---|---|---|
| A (AC's onboard slot) | `db` | the AC itself (`Self`) |
| A | `d0` | Dial (optional) |
| A | `d1` | free — the AC's onboard slot only has 2 pins total, only 1 used |
| B (IC Housing) | `d0` | Gas Sensor |
| B | `d1` | source pump |
| B | `d2` | Condensation Valve |
| B | `d3` | Purge Valve |
| B | `d4` | Dial (optional) |
| B | `d5` | free |

## How it works

Every tick, both chips independently read the same dial and look up
their own value from the gas's table row (Chip A: temperature; Chip
B: pressure). Chip A runs the same PID logic as the standalone AC
thermostat, just retargeted each tick. Chip B does three things on
independent schedules:
1. **Pressure top-up** — if chamber pressure is below target, runs the
   source pump; if above target, does nothing (there's no
   down-pressure device in this design, matching the manual process,
   which also never actively released excess pressure).
2. **Periodic drain** — pulses the Condensation Valve open on a timer
   (`DrainInterval`/`DrainHoldTicks`) to move any condensed liquid to
   the liquid side.
3. **Periodic purge** — pulses the Purge Valve open on a longer,
   shorter timer (`PurgeInterval`/`PurgeHoldTicks`) to clear stray gas
   off the liquid side, kept infrequent given the flooding risk above.

## Switching target gas

**The dial is optional, not required — both chips work either way.**
If a dial is wired to `d0`/`d4`, its live value picks the gas index
each tick, no re-flash needed. **If no dial is wired**, each script
falls back to its own `DefaultGasIndex` constant (defaults to `10`,
Pollutant — see "Why Pollutant, not Nitrogen" below) — the same
`brdns` graceful-degradation pattern used throughout this repo (see
`../../air-conditioner/ac_thermostat.ic10`,
`../../filtration/onboard_filtration.ic10`). This is a genuine, real
fix, not just a nice-to-have: the first version of these two scripts
read the dial unconditionally, which would have **faulted the whole
chip** on an unwired pin rather than just not working — a pin read
(unlike a batch read) errors out on an empty slot, the same lesson
this project already learned building the Gas Sensor chip's `brdns`
guards.

So you get real flexibility either way: build the dial for live
in-game switching, or skip it entirely and just edit
`DefaultGasIndex` in both scripts before flashing to lock in a fixed
target. **Both files' `DefaultGasIndex` need to match** if you're
using the fixed-target path — nothing enforces that automatically, so
double-check both when you change it.

**Never set either `DefaultGasIndex` or the dial to Helium's index —
it doesn't have one, because it can't be liquefied in-game at all.**

### Why Pollutant, not Nitrogen — the default, explained

Originally defaulted to Nitrogen (index 8, matching the single-chamber
script's own original hardcoded target). Project owner (2026-08-07)
pointed out this is actually a poor default: per
`../condensation_reference.md`, Nitrogen needs ~190 K even at the
table's max charted pressure (6000 kPa) — genuinely one of the more
demanding gases here, not a good target to fall back to blind. **Pollutant's
curve runs the opposite way**: its condensation temperature *rises*
with pressure rather than requiring extreme cold, so at typical
high-pressure storage tank conditions it tends to condense first, with
comparatively little active cooling needed. Matches real hands-on
experience, not just the chart reading. Changed the default to index
10 (Pollutant) in both scripts.

## Precooling pattern — no new script needed

Project owner's observation: some environments make certain gases
condense easily even before reaching the full separation setup — e.g.
precooling a waste gas tank gets the easiest liquid out for free. This
doesn't need a new script: run `../../air-conditioner/ac_thermostat.ic10`
**unmodified**, hardcoded to whatever gas condenses easiest for you
(edit its `TARGET` define, or wire its existing optional dial input),
directly on an AC cooling your waste tank, plus a Condensation Valve
left on the tank to passively skim off whatever forms. No sequencing
logic needed for this simpler case — it's a genuinely different,
much lighter use of hardware already built, not a variant worth its
own script.

## Display, not yet built

Would be genuinely nice — a readout showing which gas is currently
selected. **Correction (2026-08-07): there is a free pin for this** —
Chip B only uses 5 of its 6 pins (`d5` is free, see the pin map
above); an LED or Diode there could show a color/number keyed to
`GasIndex`. Not built yet regardless — speculative hardware additions
before they're actually wanted is exactly the kind of complexity this
project tries to avoid — but the pin exists whenever it's wanted, no
rewiring of anything else required.

## Known limitations

- **Single target gas at a time**, by design (project owner: not
  enough IO ports to run pumps for every gas/liquid simultaneously,
  and the player already knows what's in the pipe from experience —
  no need for the script to guess).
- **No stall handling**, same category of gap as `phase_separator.ic10`
  and other scripts in this repo.
- **No display yet** — see above.
- **Two-point reference data** — see `../condensation_reference.md`'s
  own caveat; these are chart-gridline readings, not exact curves.
