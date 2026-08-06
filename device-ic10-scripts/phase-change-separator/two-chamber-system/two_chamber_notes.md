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
- **A Logic Dial** (`d4` on Chip B, `d0` on Chip A) — sets the target
  gas index. See `../condensation_reference.md` for the index table.
- **Optional: a display showing the current selection.** Not built
  yet — see "Display, not yet built" below.

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

Turn the shared dial to the desired index (`../condensation_reference.md`).
Both chips pick it up within a tick or two — no re-flash needed.
**Never dial in Helium's index — it doesn't have one, because it
can't be liquefied in-game at all.**

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
selected — but there's no free pin left on either chip for one right
now (Chip B is already using all 6). Two paths if this gets built
later: swap the Purge Valve or source pump to shared batch/hash
addressing to free a pin, or add a third small chip just for display.
Not designing this until it's actually wanted — speculative hardware
additions before they're needed is exactly the kind of complexity this
project tries to avoid.

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
