# IC10 Airlock — Prototype Code & Chip Count

## Correction: I was wrong — Workshop does have close matches

You were right and I was wrong last turn. **"Custom Airlock V2"**
(Workshop ID 2978749569, by CowsAreEvil — the same author already cited
elsewhere in this project as "Cows Are Evil") does, in substantial part,
what we designed independently: it cycles as a normal airlock, but once
both sides' pressure/temperature/gas composition match, it **props both
doors open** and keeps monitoring — the instant a mismatch reappears
(pollutants, volatiles/methane, temperature or pressure drift) it seals
back up and returns to normal cycling. That's our Propped-Open state,
confirmed already built and working in the wild. **"Adaptive Airlock"**
(ID 2194510353) and **"Airlock Control"** (ID 1524868713, already cited
elsewhere) both separately confirm an **emergency override switch**
pattern — a lever/button that force-opens doors "while active," matching
what you described as the override lever. None of these combine
*everything* in this design (the staged Power-Tier failsafe and Deep
Idle Mode specifically aren't part of them), but the Propped-Open and
override-lever pieces you remembered are real, confirmed, and I should
have found them the first time.

## Validated against real production code

Custom Airlock V2's actual source (pulled directly, not just its
description) confirms several things this design either guessed at or
under-specified:

- **`brdns <device> <line>` — a real, better graceful-degradation
  instruction than what this doc used.** It branches to a line if the
  aliased device slot is empty — i.e., "if this optional device isn't
  connected, skip ahead." The production script uses it for an optional
  Diode Slide and an optional Occupancy Sensor: `brdns diode 2` and
  `brdns ocupationSensor 4`. This is more direct than the batch-vs-pin
  distinction this doc leaned on — **worth revising the Gas Sensor chip
  to use `brdns` for optional hardware instead of relying solely on
  batch addressing.**
- **The optional emergency-button pattern is confirmed working exactly
  as this doc designed it:** `lb r9 491845673 Activate Sum #reads in
  emergency button, will be 0 if no button exists`. Batch-by-hash
  reading a button that may not exist, defaulting harmlessly to 0 — this
  is precisely the Button-C-graceful-degradation approach already
  written into this design, now confirmed against real code rather than
  just reasoned through.
- **Confirmed real LogicTypes, resolving several TODOs below:**
  `RatioPollutant`, `RatioNitrousOxide`, `RatioNitrogen`, `RatioOxygen`,
  `Pressure`, `Temperature`, `Open`, `Setting`, `Lock`, `Mode`, `On` — all
  confirmed live in working code, not guessed.
- **`RatioVolatiles` → `RatioMethane` after the Gases Update — independently
  confirmed by a second source.** A comment on the script itself (dated
  this year) reads: <cite>"I think the latest gases update
  changed/removed RatioVolatiles and is instead RatioMethane
  (CH4)."</cite> This matches this project's own earlier finding about
  the March 2026 Gases Update — two independent confirmations of the
  same rename.
- **Real tolerance values for match-checking**, replacing the placeholder
  `2` used three times in this doc's Gas Sensor chip: pressure ratio
  tolerance ~0.1, temperature ~0.02, trace gases (volatiles/pollutant/
  NOx) ~0.005. These are what a live, community-used script actually
  ships with — worth adopting as starting values instead of the
  placeholder.
- **An alternative occupancy-detection approach**, worth knowing even if
  this design keeps its manual Button-C method: the script reads an
  optional Occupancy Sensor's `Activate` value each loop, compares it to
  the previous loop's stored value, and treats an *increase* as "someone
  just entered — don't let a queued button press force them back out."
  Automatic rather than manual, and a legitimate alternative if manual
  Button-C ever feels insufficient.

**Worth knowing separately:** in-game Workshop script publishing is
confirmed broken by at least one script author's own account, which is
why some community code lives on GitHub instead (`jhillacre/stationeers-scripts`,
`Zappes/Stationeers`, `drclaw1188/stationeers_ic10`) rather than
Workshop exclusively — but as the correction above shows, Workshop
itself still has real, working, actively-commented scripts too. Both are
worth checking, not just one.

## Things found while writing this that changed the design

**1. Line/character limits — resolved (2026-08-04).** Not actually a
conflict: 52 characters is the in-game editor's *typing* limit (a UI
constraint), 90 characters is the real execution/storage limit — a
pasted line up to 90 chars works fine even though the editor won't let
you type past 52 by hand. Confirmed directly in-game by the project
owner. Code below stays reasonably short per line as a matter of
in-editor readability, not because it has to.

**2. Stack is persistent — a confirmed, real gotcha.** Values pushed to
an IC10's stack survive script reloads and restarts. Community reports
describe scripts breaking after game updates specifically because of
stale stack garbage from before. **The code below avoids the stack
entirely** — registers and device I/O only — specifically to sidestep
this whole class of bug rather than remembering to clear it correctly
every time.

**3. Deep Idle needs its own switchable circuit, separate from the
watching logic — but not via Transformer (2026-08-04 correction).** An
earlier draft used a Transformer per Portal for this. **That's wrong: a
Transformer has no data port at all** — Community Wiki, verbatim,
"Data will not flow through a transformer." It's a passive wattage-cap
device; every `s XfmrExt On 1/0` write in the old draft was targeting a
device that can't receive it. The community-standard fix is a **Power
Controller (APC)**, confirmed data-networked and confirmed as what
people actually use for this since vanilla has no dedicated breaker
component. See "The Watcher/Cycle split" below for how this reshaped
the whole chip architecture, not just one device swap.

## Chip count: 3

| Chip | Role | Required or optional |
|---|---|---|
| **Watcher** | Always powered. Computes Power Tier from the dedicated Power Controller, reads all 3 Buttons, writes Tier to the shared Light, controls the Cycle-zone's power gate, broadcasts live button state via Logic Transmitter | Required |
| **Cycle** | Powered only when Watcher's zone gate is on. Owns both Portals, the Vent, and the chamber Gas Sensor; runs the full evacuate/pressurize/dwell state machine and the Critical-tier close→evacuate→unlock sequence | Required |
| **Gas Sensor / Propped-Open Monitor** | Reads the exterior- and interior-facing Gas Sensors, decides match/mismatch, broadcasts a flag the Cycle chip reads | **Optional — degrades gracefully if absent** |

The Gas Sensor chip is the one built to be skippable: if you never
install its two Gas Sensors, the Cycle chip's batch read of the
Propped-Open flag simply returns nothing to act on (see graceful
degradation note below), and the airlock just never enters
Propped-Open — every other feature keeps working normally. No error, no
crash, just one fewer capability.

## The Watcher/Cycle split — why this replaced the old Chip A/B design

This is the biggest structural change from earlier drafts, prompted by
two things surfacing together: the confirmed Transformer bug above, and
a direct question about whether the always-running IC10 chips
themselves were undermining Deep Idle Mode's whole point (each IC10
costs a flat 25W just for existing powered — three of them running
continuously is a 75W floor before anything moves).

**The shape:** one chip (**Watcher**) never powers down — it owns
everything that must always be true: Charge monitoring, Tier
computation, and reading all three Buttons (which cost nothing to
monitor regardless of their own power state, a fact already established
earlier in this project). A second chip (**Cycle**) owns the doors, the
Vent, and the actual cycling logic, and lives entirely on a separate,
switchable circuit — including its *own* IC Housing — that Watcher
gates on and off via a Power Controller instead of a Transformer.

**How Cycle learns what to do without a wired connection to the
buttons:** a **Logic Transmitter** on Watcher and a **Logic Receiver**
on Cycle, tuned to the same channel, confirmed in this project's own
sources as purpose-built for exactly this — signaling across circuits
with no direct wire needed. Watcher continuously relays each button's
*live* raw state (not a one-shot event) on three channels; Cycle reads
them fresh every loop, structurally identical to how the old Chip B
read buttons directly, just via `l r Receiver ChannelN` instead of
`lbn`.

**The safety-critical catch this surfaced:** if Cycle can be powered
off between uses, something has to guarantee it powers back on for
Critical tier even with zero buttons pressed — Critical's
close→evacuate→unlock sequence is the actual point of this whole
project and can't depend on someone having recently touched a button.
Watcher's gate logic handles this as an unconditional rule: Tier ==
Critical always holds the gate open, same priority level as Tier ==
Normal (which also holds it open continuously — Normal tier doors are
supposed to stay live and responsive, not Deep-Idled at all). Only Low
tier actually idles the zone, waking on any button press for a fixed
hold window.

**One combined gate, not two independent ones.** Both Portals, the
Vent, and Cycle chip itself share a single switchable zone rather than
each Portal getting its own independent gate. A real cycle already
needs both doors reachable (evacuate/pressurize inherently touches the
whole chamber), so splitting the gate would mostly add complexity for
a fast-path case (direct-open, no cycle needed) that's already the
cheapest case regardless.

**Bonus: this also answered a separate open question about chamber
pressure sensing.** Cycle no longer owns two Transformers or three
Buttons, freeing enough pins for a **dedicated chamber-interior Gas
Sensor** — resolving an earlier design shortcut that read pressure off
the Vent's own ambiguous `Pressure` field. A real Gas Sensor physically
inside the chamber gives an unambiguous reading with no assumptions
about what a Vent's own LogicTypes represent.

## How graceful degradation actually works here

Two different device-access methods exist, and they fail differently:

- **Pin-based (`d0`–`d5`) access to a specific missing device throws an
  error.** If you alias `d3` to a Gas Sensor that isn't physically
  connected, any `l`/`s` instruction touching it fails the whole script.
- **Batch access (`lb`/`sb`, addressing by device type-hash across the
  whole network) silently affects zero devices if none exist.** No
  error — the instruction just does nothing useful and execution
  continues.
- **Named-batch (`lbn`/`sbn`) works the same way as plain batch for
  degradation purposes, but additionally lets a build "bypass the 6-pin
  limit on IC housing device assignments"** per a confirmed community
  source — this is *why* it's used for required hardware here (the
  Buttons), not just optional hardware.

**Design choice:** optional hardware (the two Gas Sensors) uses plain
type-hash batch (`lb`) so it degrades to nothing if absent. Required
hardware splits by pin scarcity, not by required-vs-optional: Watcher's
four pins (Power Controller, Light, zone gate, Transmitter) leave
plenty of room, but the Buttons still use `lbn` for consistency with
how they were already validated. Atmosphere/safety-critical devices
(Portals, Vent, chamber sensor, Power Controllers) stay pin-based on
whichever chip owns them.

---

## Watcher — Power Tier, Buttons, and the Cycle-zone gate

Always powered. 85 of 128 lines as formatted below, comfortable margin
remaining.

**Pins:** `d0` dedicated Power Controller, `d1` shared Light, `d2`
Cycle-zone gate (a Power Controller, not a Transformer — see above),
`d3` Logic Transmitter.

**`BtnHash` (`-1591419276`)** is a community-sourced Logic Switch
structure hash, found from a single search result rather than
cross-confirmed — treat it as a strong lead, not a certainty, and
double-check it against your own Stationpedia entry. Same caveat for
the exact LogicType that gates a Power Controller's own output
(assumed `On` below, matching every other powered device confirmed in
this project, but not independently verified — flagged for an in-game
Logic Reader check).

```
# Watcher chip: Power Tier monitor + Button reader + zone-gate control.
# Always powered - never gated off, unlike the Cycle chip below.
# Owns: dedicated Power Controller, shared Light, Cycle-zone power gate,
# Logic Transmitter (broadcasts live E/I/C button state to Cycle chip).

alias PC d0
alias SigLight d1
alias ZoneGate d2
alias Xmit d3

define BtnHash -1591419276
define BtnEName HASH("AirlockBtnE")
define BtnIName HASH("AirlockBtnI")
define BtnCName HASH("AirlockBtnC")
define WakeHold 20

move r0 0
move r7 0

loop:
l r1 PC Charge
l r2 PC Maximum
div r1 r1 r2
mul r1 r1 100

beq r0 0 fromNorm
beq r0 1 fromLow
j fromCrit

fromNorm:
bgt r1 90 stay
move r0 1
j stay

fromLow:
bge r1 93 up
ble r1 10 down
j stay
up:
move r0 0
j stay
down:
move r0 2
j stay

fromCrit:
bgt r1 13 riseCrit
j stay
riseCrit:
move r0 1

stay:
s SigLight Setting r0

lbn r3 BtnHash BtnEName Activate 0
lbn r4 BtnHash BtnIName Activate 0
lbn r5 BtnHash BtnCName Activate 0
s Xmit Channel1 r5
s Xmit Channel2 r3
s Xmit Channel3 r4

move r6 0
beq r0 0 forceHold
beq r0 2 forceHold
bnez r3 forceHold
bnez r4 forceHold
bnez r5 forceHold
j checkHold
forceHold:
move r6 1
checkHold:
bnez r6 doHold
bgtz r7 stillHeld
s ZoneGate On 0
j endLoop
stillHeld:
sub r7 r7 1
j gateOn
doHold:
move r7 WakeHold
gateOn:
s ZoneGate On 1
endLoop:
yield
j loop
```

The hysteresis thresholds (`bgt r1 90`, `ble r1 10`, `bge r1 93`,
`bgt r1 13`) are unchanged from the original Chip A design — those were
dry-run verified tick-by-tick in an earlier pass (see project history)
and had a real off-by-one-percentage-point bug fixed there, long before
this Watcher/Cycle restructuring. Nothing about the split touches that
logic.

**Dry-run verification (2026-08-04):** loaded into
`stationeering/stationeers-ic`. The `HASH()` macro and `lbn` instruction
aren't supported by this specific emulator (it predates named-batch
entirely, same limitation already noted for `sbn`) — confirmed by
substituting numeric placeholders for the three `define ...Name
HASH(...)` lines and `lb` for `lbn`, which parses with zero errors,
isolating the emulator gap to exactly those lines and nowhere else.
Functionally verified five scenarios with the substituted version: (1)
Normal tier holds the gate on continuously with zero buttons pressed —
confirming Normal never Deep-Idles; (2) dropping to Low with no button
correctly idles the gate; (3) a single-tick Button E press in Low tier
holds the gate open for the full `WakeHold` window after release, then
correctly idles again; (4) Charge draining straight through to Critical
holds the gate open with **zero buttons pressed at all** — the
safety-critical case this whole restructuring had to preserve; (5) the
gate never drops while Critical persists, re-extending every tick for
as long as Tier stays 2.

---

## Cycle — Doors, Vent, chamber sensor

Powered only when Watcher's zone gate is on. 114 of 128 lines as
formatted below.

**Pins:** `d0` shared Light (read-only here — cross-circuit data wiring
to a device on Watcher's always-on power circuit, the same pattern the
original 3-chip design already relied on for Chip A/B sharing this same
Light), `d1`/`d2` exterior/interior Portal, `d3` Vent, `d4` Logic
Receiver, `d5` a **dedicated Gas Sensor physically inside the chamber**
(new hardware — see "Bonus" note above for why).

```
# Cycle chip: owns Doors, Vent, chamber Gas Sensor. Powered only when
# Watcher's zone gate is on - not running otherwise, no separate Deep
# Idle logic needed here, Watcher already handles that upstream.

alias SigLight d0
alias DoorExt d1
alias DoorInt d2
alias Vent d3
alias Receiver d4
alias ChamberSensor d5

define PropFlagHash -1234567
define TargetInt 100
define TargetExt 2

move r10 0
move r11 0
move r13 0

loop:
l r0 SigLight Setting
beq r0 2 tierCrit
beq r0 0 checkProp
j cycleCheck

checkProp:
lb r5 PropFlagHash Setting 0
beqz r5 cycleCheck
s DoorExt Open 1
s DoorInt Open 1
j endLoop

cycleCheck:
bgtz r11 doorTimer
bnez r13 continueCycle
l r14 DoorExt Open
l r15 DoorInt Open
bgtz r14 endLoop
bgtz r15 endLoop
l r6 Receiver Channel2
l r7 Receiver Channel3
bnez r6 reqExt
bnez r7 reqInt
j endLoop

reqExt:
beq r10 0 openExt
move r13 1
j endLoop
openExt:
s DoorExt Open 1
move r11 10
j endLoop

reqInt:
beq r10 1 openInt
move r13 2
j endLoop
openInt:
s DoorInt Open 1
move r11 10
j endLoop

continueCycle:
beq r13 1 evacuate
j pressurize

evacuate:
s Vent Mode 0
s Vent On 1
l r12 ChamberSensor Pressure
bgt r12 TargetExt endLoop
s Vent On 0
move r10 0
move r13 0
s DoorExt Open 1
move r11 10
j endLoop

pressurize:
s Vent Mode 1
s Vent On 1
l r12 ChamberSensor Pressure
blt r12 TargetInt endLoop
s Vent On 0
move r10 1
move r13 0
s DoorInt Open 1
move r11 10
j endLoop

doorTimer:
sub r11 r11 1
bgtz r11 endLoop
s DoorExt Open 0
s DoorInt Open 0
j endLoop

tierCrit:
l r8 Receiver Channel1
bnez r8 endLoop
s DoorExt Open 0
s DoorInt Open 0
s Vent Mode 0
s Vent On 1
l r12 ChamberSensor Pressure
bgt r12 TargetExt endLoop
s Vent On 0
s DoorExt Lock 0
s DoorInt Lock 0

endLoop:
yield
j loop
```

**What changed from the old Chip B besides the Receiver swap:** the
Propped-Open check (`checkProp`) now only runs when Tier is Normal,
matching the requirements doc's state enumeration (Propped-Open isn't
listed as available in Low Power tier at all — the old design didn't
enforce this distinction as cleanly). Both Transformer writes are gone
entirely — there's nothing left for Cycle to do about its own power,
that's now Watcher's job exclusively. Chamber pressure reads switched
from the Vent's own ambiguous `Pressure` field to a dedicated
`ChamberSensor`.

**The `r13` "pending cycle direction" register** still exists for the
same reason as before: a button held for one tick can't drive a
multi-tick evacuate/pressurize cycle by being re-checked every loop —
`r13` persists the decision across ticks after the triggering read.

**Dry-run verification (2026-08-04):** zero program errors (this chip
uses no `lbn`/`HASH()`, so it validates cleanly start to finish, unlike
Watcher). Six functional scenarios: (1) requesting entry from the
already-matched side opens directly, no cycle; (2) requesting the
*other* side correctly withholds the door, runs the Vent, and keeps
`r13` set across many ticks with the button long since released,
opening the door the instant target pressure is reached; (3) Critical
tier closes both doors and starts evacuating via the Vent; (4) doors
stay locked and the Vent keeps running until chamber pressure actually
reaches the near-vacuum target — unlock only happens after, not in the
same tick as closing; (5) Button C held during Critical (relayed live
on Channel1) skips the entire evacuation branch for that loop, leaving
doors/locks exactly as they were; (6) the Propped-Open branch parses
and wires correctly, though the actual Gas Sensor chip's flag write
still can't be exercised end-to-end in this emulator (`lb`/`sb` are
no-ops here, same limitation noted for the Gas Sensor chip below).

**Still not implemented (unchanged from before the restructuring):**
Stalled-phase detection/recovery, and the Propped-Open exit sequence
once a mismatch is detected mid-prop (close which door first — not
specified).

---

## Gas Sensor / Propped-Open Monitor (optional)

Unchanged by the Watcher/Cycle restructuring — this chip never touched
Transformers or Buttons, so nothing about the split affected it. Kept
here for completeness; see prior verification below.

```
# Gas Sensor chip: OPTIONAL. Only build this if you installed both
# Gas Sensors. Broadcasts match/mismatch via a type-hash batch flag
# the Cycle chip reads with its own "lb" call - both chips address by
# type-hash only, no device name/Labeller needed, so they always agree
# on what they're reading/writing.
# If this chip doesn't exist, Cycle's batch reads of the same flag
# simply return nothing - no error, Propped-Open just never triggers.
# No single "Ratio" field exists for composition - check Oxygen
# (breathable) plus Pollutant/Methane/NOx (hazard) per-gas instead.

alias SensExt d0
alias SensInt d1

define PropFlagHash -1234567   # must match the Cycle chip's constant
                                 # exactly - each chip defines its own
                                 # copy, they don't share a symbol table

loop:
l r0 SensExt Pressure
l r1 SensInt Pressure
l r2 SensExt Temperature
l r3 SensInt Temperature

move r6 0             # r6 = match flag, default 0 (no match)
sub r7 r0 r1
abs r7 r7
bgt r7 0.1 noMatch     # pressure tol ~0.1 (Custom Airlock V2)
sub r7 r2 r3
abs r7 r7
bgt r7 0.02 noMatch    # temperature tol ~0.02

l r4 SensExt RatioOxygen
l r5 SensInt RatioOxygen
sub r7 r4 r5
abs r7 r7
bgt r7 0.005 noMatch   # trace-gas tol ~0.005

l r4 SensExt RatioPollutant
l r5 SensInt RatioPollutant
sub r7 r4 r5
abs r7 r7
bgt r7 0.005 noMatch

l r4 SensExt RatioMethane
l r5 SensInt RatioMethane
sub r7 r4 r5
abs r7 r7
bgt r7 0.005 noMatch

l r4 SensExt RatioNitrousOxide
l r5 SensInt RatioNitrousOxide
sub r7 r4 r5
abs r7 r7
bgt r7 0.005 noMatch
move r6 1

noMatch:
sb PropFlagHash Setting r6
yield
j loop
```

Tolerance values are Custom Airlock V2's real, live-used figures:
pressure ratio ~0.1, temperature ~0.02, trace gases (methane,
pollutant, NOx) ~0.005. The composition check compares Oxygen,
Pollutant, Methane, and Nitrous Oxide individually — there's no single
field for "gas composition." 60 of 128 lines as formatted, well within
budget even with all five checks.

The flag write uses `sb` (plain type-hash batch), matching the Cycle
chip's `lb` read of the same flag — both address by type-hash alone,
no Labeller name needed, so they always agree on what they're touching.
An earlier draft used `sbn` with a wired-pin argument where a hash
constant belonged, a real type mismatch caught by loading it into
`stationeering/stationeers-ic` and getting `UNKNOWN_INSTRUCTION`
(that specific emulator predates `sbn`, but the underlying argument
type mismatch was the actual bug, independently confirmed via a
community source describing `sbn`'s real signature).

**Functional dry-run:** 9 scenarios covering full match, a
tolerance-boundary case, five independent single-field mismatches
(pressure, temperature, O₂, Pollutant, NOx, each shown to veto a match
on its own), and a matched→mismatched→re-matched sequence confirming
live recovery with no unwanted latching. All passed; zero program
errors.

---

## What's genuinely done vs. still open

**As of 2026-08-04 (Watcher/Cycle restructuring), all three chips load
with zero program errors** (setting aside the `HASH()`/`lbn` emulator
gap on Watcher, isolated and confirmed to be the emulator's limitation,
not the code's). Solid, dry-run-verified: the Tier hysteresis state
machine, the full evacuate/pressurize/dwell cycle, the Critical-tier
close→evacuate→unlock ordering, the Button-C override, the Watcher gate
logic including the safety-critical forced-wake-on-Critical rule, and
the Gas Sensor chip's match/mismatch branching.

**Genuinely still open:**
- The exact LogicType that gates a Power Controller's own output —
  assumed `On`, not independently confirmed.
- `BtnHash` — single-sourced, not cross-confirmed.
- Whether direct data wiring (Cycle chip's Light read) or the Logic
  Transmitter/Receiver pair actually behave as expected across two
  independently-power-gated circuits — reasoned through carefully
  against confirmed game mechanics (data network ≠ power network), but
  not verified in an actual running game.
- Stalled-phase detection/recovery, and Propped-Open's mid-mismatch
  exit ordering — unchanged gaps from before this restructuring.
- Whether `brdns` should replace pure batch addressing for the optional
  Gas Sensors — a real improvement per Custom Airlock V2, not yet made.

**Reference:** IC10 syntax and instruction patterns confirmed via
XGamingServer's IC10 programming guide (LogicType read/write syntax,
`l`/`s` instructions), Community Wiki "IC10" and "Integrated Circuit
(IC10)" pages (batch addressing via type-hash, alias syntax), and
GitHub repos `jhillacre/stationeers-scripts` and
`SnorreSelmer/stationeers_ic10`. Stack persistence gotcha and
`dr##`-style invalid register errors confirmed via Steam Community
discussion threads. **Production code validation:** Steam Workshop
"Custom Airlock V2" (ID 2978749569, by CowsAreEvil) — full source
inspected directly, confirming `brdns`, the optional-button batch
pattern, real LogicType names, and real match-tolerance values as
detailed above. "Adaptive Airlock" (ID 2194510353) and "Airlock Control"
(ID 1524868713) both independently confirm the emergency-override-lever
pattern. Transformer data-port limitation and the Power
Controller/Logic Transmitter-Receiver alternatives confirmed via
Community Wiki and Steam Community discussion threads — see
`SOURCES.md`.
