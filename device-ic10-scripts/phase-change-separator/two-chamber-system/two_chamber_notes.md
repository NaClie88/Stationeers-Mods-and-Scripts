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
  (`d0`), required. `brdns`-guarded (2026-08-07) — an unplugged/removed
  Sensor idles the source pump to a known-safe off state instead of
  faulting Chip B mid-tick; the drain/purge timers below don't depend
  on it and keep running regardless.
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
- **`d5` on Chip B is free.** A display readout was attempted here and
  reverted — see "Display" below for why and what's still needed
  before trying again.

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
thermostat, just retargeted each tick — including a 2026-08-07 bug fix
shared with it: the original `scaleSetting` carried an extra term that
canceled its own derivative term, so it was silently running as
pure-P (double the intended gain, no damping) despite tracking
`PreviousError` every call. See
`../../air-conditioner/ac_thermostat_notes.md`'s second bug section
for the full trace; both files were fixed together. Chip B does three
things on independent schedules:
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

## Display — attempted 2026-08-07, reverted, real status unconfirmed

Built and then **reverted** — the hardware assumption behind it turned
out to be wrong, or at least unconfirmed enough that it shouldn't ship.

What happened: added a readout using a device found via a community
GitHub scripts repo, calling itself "LED Display," writable
`Mode`/`Setting`/`On`/`Color`, `Mode 0` a plain raw-number display.
That source itself hedged — "doesn't specify an official in-game
name" — a flag that should have blocked shipping it, not just been
noted in passing. Further digging found the real Community Wiki
entries: **"Kit (Consoles) LED Display (Small/Medium/Large)."**
"Kit (Consoles)" is the same naming pattern as other Console-*mounted*
components — meaning this is very likely a card that slots into a
Console structure, the same `ButtonCommands`/`Motherboard` system
extensively established elsewhere in this repo (see
`../../logic-network-reference/devices/motherboards.md`,
`console-ui-mod`'s whole design problem) as **not reachable by a plain
IC10 pin write at all**. The project owner independently confirmed
seeing exactly this — a Console with a display card slotted in, not a
freestanding LED — while unsure whether what it displayed was vanilla
or modded content. Whichever it turns out to be, `s Display Setting
GasIndex` the way this was written almost certainly wouldn't have
worked as a standalone pin device the way a Light or Diode does.

**Why not just swap to a Color-coded LED instead** (the Watcher chip's
Tier-display approach, already confirmed via direct decompilation, not
just a wiki summary): that mechanism is real, but this system needs to
distinguish 11 gases, and this repo's own Color enum research
(`../../airlock-ic10-scripts/watcher.ic10`) only ever confirmed 3
values (green/yellow/red) — how many colors actually exist is still on
that project's own "genuinely still open" list. Reaching for it here
would repeat the exact mistake just corrected: shipping a hardware
assumption that sounds plausible without confirming it first.

**Status: genuinely unbuilt, not just deprioritized.** Two real paths,
neither confirmed yet:
1. A Color-coded LED, once the actual number of usable `Color` values
   is confirmed in-game (would also resolve that open item for the
   airlock scripts, not just this one).
2. A true Console-mounted display, once `console-ui-mod` (or direct
   research into the Kit (Consoles) LED Display card specifically)
   confirms how to drive one from IC10 output.
`d5` stays free on Chip B until one of these is actually confirmed.
**Both ideas, including the exact reverted code, are filed away in
`shelved_display_ideas.md`** rather than thrown away — the design/
implementation cost is already spent, and either could become
buildable the moment its blocking unknown resolves.

## Known limitations

- **Single target gas at a time**, by design (project owner: not
  enough IO ports to run pumps for every gas/liquid simultaneously,
  and the player already knows what's in the pipe from experience —
  no need for the script to guess).
- **No stall handling**, same category of gap as `phase_separator.ic10`
  and other scripts in this repo.
- **No display** — attempted, reverted on a wrong hardware assumption,
  see "Display" above.
- **Two-point reference data** — see `../condensation_reference.md`'s
  own caveat; these are chart-gridline readings, not exact curves.
