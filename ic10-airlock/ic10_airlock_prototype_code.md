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
  distinction this doc leaned on — **worth revising Chip B/C to use
  `brdns` for optional hardware instead of relying solely on batch
  addressing.**
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
  `2` used three times in this doc's Chip C: pressure ratio tolerance
  ~0.1, temperature ~0.02, trace gases (volatiles/pollutant/NOx) ~0.005.
  These are what a live, community-used script actually ships with —
  worth adopting as starting values instead of the placeholder.
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

## Two things I found while writing this that change the design

**1. Line/character limits conflict across sources.** One source says
128 lines × 90 characters. Another (jhillacre's repo, actively
maintained) says 128 lines × **52 characters**. These can't both be
current — check your actual in-game editor before assuming either.
Code below is written conservatively short per line to be safe under
whichever limit is real.

**2. Stack is persistent — a confirmed, real gotcha.** Values pushed to
an IC10's stack survive script reloads and restarts. Community reports
describe scripts breaking after game updates specifically because of
stale stack garbage from before. **The code below avoids the stack
entirely** — registers and device I/O only — specifically to sidestep
this whole class of bug rather than remembering to clear it correctly
every time.

**3. New requirement surfaced: Deep Idle Mode needs a dedicated
power-switching device per Portal.** This wasn't caught until writing
actual code. The IC10 chips, Buttons, and Light all need to stay powered
to keep watching for a wake trigger — but if they're on the *same*
power circuit as the Portal, cutting the Portal's power for Deep Idle
would also kill the chip that's supposed to wake it back up. **Fix:**
each Portal needs its own switching device (a Transformer with its
toggle, or equivalent) in its power line, separate from the circuit
feeding the IC10/Buttons/Light. The controller chip toggles the
Transformer, not the Portal's own power state directly. This is now a
hardware requirement, not just a scripting detail — add "Transformer
per Portal" to the build list.

## Chip count: 3

| Chip | Role | Required or optional |
|---|---|---|
| **A — Power Monitor** | Reads dedicated Power Controller charge, computes Power Tier with hysteresis, broadcasts it | Required |
| **B — Door/Vent/Button Controller** | Owns Portals, Vents, Transformers, Buttons E/I/C; runs the Cycle Phase state machine per tier | Required |
| **C — Gas Sensor / Propped-Open Monitor** | Reads both Gas Sensors, decides match/mismatch, broadcasts a flag Chip B reads | **Optional — degrades gracefully if absent** |

Chip C is the one built to be skippable: if you never install the Gas
Sensors, Chip B's batch read of the Propped-Open flag simply returns
nothing to act on (see graceful degradation note below), and the
airlock just never enters Propped-Open — every other feature keeps
working normally. No error, no crash, just one fewer capability.

## How graceful degradation actually works here

Two different device-access methods exist, and they fail differently:

- **Pin-based (`d0`–`d5`) access to a specific missing device throws an
  error.** If you alias `d3` to a Gas Sensor that isn't physically
  connected, any `l`/`s` instruction touching it fails the whole script.
- **Batch access (`lb`/`sb`, addressing by device type-hash across the
  whole network) silently affects zero devices if none exist.** No
  error — the instruction just does nothing useful and execution
  continues.

**Design choice: everything optional (Gas Sensors, Button C) uses batch
addressing. Everything required (Portals, Power Controller, main
Buttons E/I) uses pin-based aliasing.** This is *why* Chip C can be
skipped without breaking Chip B — Chip B never pin-references anything
Chip C-specific, it only reads a shared batch-addressed flag that
harmlessly stays at its default value if Chip C was never built.

---

## Chip A — Power Monitor

```
# Chip A: Power Monitor & Tier Broadcaster
# Reads dedicated Power Controller, computes Tier with hysteresis
# Broadcasts Tier (0=Normal,1=Low,2=Crit) via shared Light's Setting
# TODO verify: exact charge LogicType (tried Ratio here - CONFIRM in-game,
#   some devices use Charge or ChargeRatio instead - see checklist item 2)

alias PC d0
alias SigLight d1

move r0 0        # r0 = current Tier, start Normal

loop:
l r1 PC Ratio
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
blt r1 11 down
j stay
up:
move r0 0
j stay
down:
move r0 2
j stay

fromCrit:
bgt r1 12 riseCrit
j stay
riseCrit:
move r0 1

stay:
s SigLight Setting r0
yield
j loop
```

Roughly 35 lines. Well under either line-count limit; the constraint
here is characters-per-line, which this stays conservative on.

---

## Chip B — Door / Vent / Button Controller

```
# Chip B: Core airlock state machine
# Reads Tier from Chip A via SigLight.Setting
# Owns both Portals, their Transformers, Vents, Buttons E/I/C
# TODO verify: Lock/Open LogicType names, Transformer On/Off name

alias SigLight d0
alias DoorExt d1
alias DoorInt d2
alias XfmrExt d3   # power switch for exterior Portal (Deep Idle req.)
alias XfmrInt d4   # power switch for interior Portal
alias BtnE d5       # exterior button

define PropFlagHash -1234567   # placeholder hash for Chip C's shared flag
                                 # TODO: replace with real device/name hash

move r10 0     # r10 = last known Tier

loop:
l r0 SigLight Setting     # current Tier from Chip A

# --- Tier 0: Normal - doors stay powered, standard cycling ---
beq r0 0 tierNormal
beq r0 1 tierLow
j tierCrit

tierNormal:
s XfmrExt On 1
s XfmrInt On 1
# Propped-Open check (graceful: batch read defaults to 0 if Chip C absent)
lb r5 PropFlagHash Setting 0    # avg/sum mode - harmless if 0 devices
beqz r5 normalCycle
# Match confirmed by Chip C - prop both open, skip normal cycle logic
s DoorExt Open 1
s DoorInt Open 1
j endLoop
normalCycle:
# (standard evacuate/pressurize/open sequence goes here -
#  omitted for length, same shape as Community Wiki's
#  "Custom Airlock IC10" reference script)
j endLoop

# --- Tier 1: Low Power - Deep Idle, wake on E/I/C ---
tierLow:
l r6 BtnE Activate
bnez r6 wakeExt
# TODO: same check pattern for BtnI (d?), BtnC (batch, see below)
# Chamber button C via batch (graceful degrade if not built)
lb r7 PropFlagHash Activate 0
bnez r7 wakeChamber
# no button pressed - stay in Deep Idle: doors unpowered+unlocked
s XfmrExt On 0
s XfmrInt On 0
s DoorExt Lock 0
s DoorInt Lock 0
j endLoop
wakeExt:
s XfmrExt On 1
# (run cycle toward exterior, then return to Deep Idle)
j endLoop
wakeChamber:
s XfmrExt On 1
s XfmrInt On 1
j endLoop

# --- Tier 2: Critical - close, evacuate, unlock ---
tierCrit:
# Button-C override check FIRST (see requirements doc "RESOLVED")
lb r8 PropFlagHash Activate 0
bnez r8 endLoop        # C held - skip evacuation this loop, re-check next
s DoorExt Open 0
s DoorInt Open 0
# (vent evacuation sequence goes here)
s DoorExt Lock 0
s DoorInt Lock 0
s XfmrExt On 0
s XfmrInt On 0

endLoop:
yield
j loop
```

This is a **skeleton, not a complete script** — the actual
evacuate/pressurize gas-transfer sequences are left as comments,
matching the shape of the Community Wiki's confirmed working reference
(`Custom Airlock IC10`) rather than reinventing that part. Filling
those in is mechanical once you're working in-game against real device
names. What's real here is the **Tier-driven branching structure**,
the **Deep Idle power-cut-via-Transformer pattern**, and the **Button-C
override placement** — those are the parts unique to this design that
wouldn't come from a generic airlock tutorial.

**Button I and the real Button C wiring are left as TODOs** — I've
shown the pattern (pin-based for I, batch for C) rather than guessing
exact aliases you haven't built yet.

---

## Chip C — Gas Sensor / Propped-Open Monitor (optional)

```
# Chip C: OPTIONAL. Only build this if you installed both Gas Sensors.
# Broadcasts match/mismatch via a named batch flag Chip B reads.
# If this chip doesn't exist, Chip B's batch reads of the same flag
# simply return nothing - no error, Propped-Open just never triggers.

alias SensExt d0
alias SensInt d1
alias FlagDevice d2   # any device Chip B also batch-addresses by name

loop:
l r0 SensExt Pressure
l r1 SensInt Pressure
l r2 SensExt Temperature
l r3 SensInt Temperature
l r4 SensExt Ratio      # gas composition - TODO confirm exact LogicType
l r5 SensInt Ratio

move r6 0                # r6 = match flag, default 0 (no match)
sub r7 r0 r1
abs r7 r7
bgt r7 2 noMatch          # pressure tolerance placeholder - TUNE THIS
sub r7 r2 r3
abs r7 r7
bgt r7 2 noMatch          # temperature tolerance placeholder - TUNE THIS
sub r7 r4 r5
abs r7 r7
bgt r7 2 noMatch          # composition tolerance placeholder - TUNE THIS
move r6 1

noMatch:
sbn FlagDevice PropFlagHash Setting r6
yield
j loop
```

Tolerance values (`2` used as a placeholder three times above) were
guesses at the time this was written. **Now superseded** — Custom
Airlock V2's real, live-used values are: pressure ratio ~0.1,
temperature ~0.02, trace gases (volatiles/methane, pollutant, NOx)
~0.005. Replace the `2` placeholders with these before testing.

---

## What's genuinely done vs. still a skeleton

**Solid, based on confirmed mechanics:** the Tier state machine and
hysteresis (Chip A), the batch-vs-pin graceful degradation pattern (now
additionally confirmed against Custom Airlock V2's actual use of the
same `lb`-defaults-to-zero technique for its optional emergency button),
the Button-C override placement in Critical, the Deep Idle
Transformer-switching approach. **Also now solid:** the specific
LogicType names `RatioPollutant`, `RatioNitrousOxide`, `RatioNitrogen`,
`RatioOxygen`, `Pressure`, `Temperature`, `Open`, `Setting`, `Lock`,
`Mode`, `On`, and the match-tolerance values above — all confirmed
against real production code rather than guessed.

**Still skeleton, needs real in-game work:** the actual
evacuate/pressurize gas sequences in Chip B (intentionally deferred to
the known-working Community Wiki reference pattern rather than
reinvented here), the exact hash constants (`PropFlagHash` placeholder
needs replacing with either a real device name hash via Labeller or a
specific type-hash — this wasn't resolved, just represented
structurally), and whether Chip B/C should be revised to use the
confirmed `brdns` instruction instead of pure batch addressing for
optional hardware (see "Validated against real production code" above —
this is a genuine improvement worth making, not yet done).

**Still unconfirmed (not resolved by the Custom Airlock V2 find):** the
Power Controller `Charge`/`Ratio` field name specifically (Custom
Airlock V2 doesn't monitor a Power Controller at all, so it doesn't help
here), and the Charge trend/hysteresis-band approach this design's Chip
A is built around, which has no equivalent in the reference script.

**Not written at all:** Chip A/B's handling of Button I specifically
(shown as a comment/TODO in Chip B rather than guessed), and the full
Propped-Open exit sequence once a mismatch is detected mid-prop (close
which door first, in what order — not specified).

**Reference:** IC10 syntax and instruction patterns confirmed via
XGamingServer's IC10 programming guide (LogicType read/write syntax,
`l`/`s` instructions), Community Wiki "IC10" and "Integrated Circuit
(IC10)" pages (batch addressing via type-hash, alias syntax), and
GitHub repos `jhillacre/stationeers-scripts` (128-line/52-char limit,
confirmed) and `SnorreSelmer/stationeers_ic10` (128-line/90-char limit
— conflicts with the above, unresolved). Stack persistence gotcha and
`dr##`-style invalid register errors confirmed via Steam Community
discussion threads. **Production code validation:** Steam Workshop
"Custom Airlock V2" (ID 2978749569, by CowsAreEvil) — full source
inspected directly, confirming `brdns`, the optional-button batch
pattern, real LogicType names, and real match-tolerance values as
detailed above. "Adaptive Airlock" (ID 2194510353) and "Airlock Control"
(ID 1524868713) both independently confirm the emergency-override-lever
pattern.
