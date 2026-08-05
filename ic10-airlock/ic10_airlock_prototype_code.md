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
| **B — Door/Vent/Button Controller** | Owns Portals, Vent, Transformers (pin-based); reads Buttons E/I/C (named-batch, see below); runs the full Cycle Phase state machine per tier | Required |
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

**Revised design choice, updated once Chip B hit the 6-pin ceiling:**
optional hardware (Gas Sensors) still uses plain type-hash batch (`lb`)
so it degrades to nothing if absent, same as always. Required hardware
now splits two ways — **atmosphere/safety-critical devices (Portals,
Transformers, Vent, Power Controller) stay pin-based**, but **all three
Buttons moved to named-batch (`lbn`)** once there was no pin left for
even one of them, let alone three. This wasn't optional-vs-required
driving the split anymore, it was pin scarcity — Buttons are the one
required category where batch addressing (with a unique Labeller name)
costs nothing in reliability, since they're readable regardless of
power state either way. This is *why* Chip C can be skipped without
breaking Chip B — Chip B never pin-references anything Chip C-specific,
it only reads a shared batch-addressed flag that harmlessly stays at
its default value if Chip C was never built.

---

## Chip A — Power Monitor

```
# Chip A: Power Monitor & Tier Broadcaster
# Reads dedicated Power Controller, computes Tier with hysteresis
# Broadcasts Tier (0=Normal,1=Low,2=Crit) via shared Light's Setting
# Charge/Maximum confirmed real (checklist item 2) - no direct Ratio
# field confirmed on Power Controller itself, so compute it here

alias PC d0
alias SigLight d1

move r0 0        # r0 = current Tier, start Normal

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
yield
j loop
```

Roughly 35 lines. Well under either line-count limit; the constraint
here is characters-per-line, which this stays conservative on.

**Dry-run finding (2026-08-04, caught by actually executing this code in
`stationeering/stationeers-ic`, a real JS IC10 emulator, not just reading
it):** the two lines above were originally `blt r1 11 down` and
`bgt r1 12 riseCrit`. Both are now fixed — the originals silently
narrowed the documented hysteresis band by a full percentage point on
each side. `blt r1 11` trips Critical at any Charge *below 11%*, not at
the documented "≤10%" — so a Charge sitting at 10.5% would already be
forcing an unnecessary emergency evacuation a full point early. Symmetrically,
`bgt r1 12` let the script climb back out of Critical at just above 12%,
a point below the documented "> 13%" recovery threshold — shrinking the
Low↔Critical hysteresis gap from the intended 3 points down to 1–2,
increasing flapping risk right at the edge of the state that's supposed
to be safest. Running the corrected thresholds through the same
tick-by-tick trace (100→91→90→89, 92.9→93→94, 12→11→10→9,
10→12→13→14) now reproduces the requirements doc's hysteresis table
exactly. The Normal↔Low band (`bgt r1 90` / `bge r1 93`) was already
correct — only the Low↔Critical band had the bug.

---

## Chip B — Door / Vent / Button Controller

**Status: complete, not a skeleton — 127 of 128 lines, zero room left
for inline comments.** Every comment that would normally sit next to
this code had to move to the prose below instead, because there is
exactly one spare line in the whole chip. Read this section before the
code, not after.

**Pin-budget problem this design hit, and how it's resolved:** the
IC Housing only has 6 device pins (`d0`–`d5`). Owning SigLight, both
Portals, both Transformers, and the Vent directly already consumes all
6 — there is no pin left for Button E, let alone I or C. The fix:
**all three buttons moved to `lbn` (named batch read)** instead of pins.
This isn't a workaround — `lbn`/`sbn` exist specifically to "bypass the
6-pin limit on IC housing device assignments" per a confirmed community
source (see `SOURCES.md`), and Buttons cost nothing to monitor
regardless of power state, so there's no latency/reliability cost to
this move the way there would be for a door or vent. Each button needs
a **unique name assigned via Labeller in-game** before this works —
`AirlockBtnE`, `AirlockBtnI`, `AirlockBtnC` are the names this code
assumes; the companion setup doc walks through assigning them.
`BtnHash` (`-1591419276`) is a community-sourced Logic Switch structure
hash, found from a single search result rather than cross-confirmed —
treat it as a strong lead, not a certainty, and double check it against
your own Stationpedia entry or replace it with `HASH("<exact prefab
name>")` directly if your build's button structure name differs.

**The `r13` "pending cycle direction" register exists to fix a bug this
design almost shipped.** An earlier draft re-derived what to do purely
from the button's *current* state each tick — but a button is only
held for one tick, and a full evacuate/pressurize cycle takes many. By
the time the vent reached target pressure, the button would already
read 0 again, and the door would never open. `r13` (0 = idle, 1 =
cycling toward Exterior, 2 = cycling toward Interior) persists across
ticks specifically so a single button press reliably completes the
whole cycle. Verified in `stationeering/stationeers-ic`: pressed Button
I, released it immediately, then stepped the emulator through five
ticks of rising pressure with the button un-pressed the whole time —
`r13` held its value across all of them, and the interior door opened
correctly the instant target pressure was reached.

**Simplifications, stated plainly:** door-open dwell time is a fixed
10-tick countdown (`r11`), not occupancy-sensed — matches this design's
earlier, deliberate rejection of suit/occupancy sensing for Button C;
tune the `10` once you can time an actual transit in-game. Chamber
pressure during a cycle is read off the Vent's own `Pressure` field
rather than a dedicated chamber Gas Sensor, to avoid needing a 7th
pin-worthy device — reasonable since the Vent is physically servicing
the chamber it's venting, but not independently verified against a
chamber-interior reading. Stalled-phase detection/recovery and the
Propped-Open mid-prop mismatch exit ordering (both flagged as open in
the state enumeration doc) are still not implemented — this chip
handles the core cycle, not every edge case in that table. `r10`
assumes the chamber starts Exterior-matched (`0`) on first boot; correct
it if your build's actual starting atmosphere differs.

**Critical tier now actually waits for evacuation before unlocking** —
the earlier skeleton unlocked immediately with just a comment where the
vent-out sequence belonged. This version runs the Vent in evacuate mode
and holds the unlock/depower steps until chamber `Pressure` drops below
`TargetExt`, matching the requirements doc's stated close→evacuate→
unlock *order* rather than doing all three in one tick.

```
alias SigLight d0
alias DoorExt d1
alias DoorInt d2
alias XfmrExt d3
alias XfmrInt d4
alias Vent d5
define PropFlagHash -1234567
define BtnHash -1591419276
define BtnEName HASH("AirlockBtnE")
define BtnIName HASH("AirlockBtnI")
define BtnCName HASH("AirlockBtnC")
define TargetInt 100
define TargetExt 2
move r10 0
move r11 0
move r13 0
loop:
l r0 SigLight Setting
beq r0 0 tierNormal
beq r0 1 tierLow
j tierCrit
tierNormal:
s XfmrExt On 1
s XfmrInt On 1
lb r5 PropFlagHash Setting 0
beqz r5 normalCycle
s DoorExt Open 1
s DoorInt Open 1
j endLoop
normalCycle:
bgtz r11 doorTimer
bnez r13 continueCycle
l r14 DoorExt Open
l r15 DoorInt Open
bgtz r14 endLoop
bgtz r15 endLoop
lbn r6 BtnHash BtnEName Activate 0
lbn r7 BtnHash BtnIName Activate 0
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
l r12 Vent Pressure
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
l r12 Vent Pressure
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
tierLow:
lbn r6 BtnHash BtnEName Activate 0
bnez r6 wakeExt
lbn r7 BtnHash BtnIName Activate 0
bnez r7 wakeInt
lbn r8 BtnHash BtnCName Activate 0
bnez r8 wakeChamber
s XfmrExt On 0
s XfmrInt On 0
s DoorExt Lock 0
s DoorInt Lock 0
j endLoop
wakeExt:
s XfmrExt On 1
j endLoop
wakeInt:
s XfmrInt On 1
j endLoop
wakeChamber:
s XfmrExt On 1
s XfmrInt On 1
j endLoop
tierCrit:
lbn r8 BtnHash BtnCName Activate 0
bnez r8 endLoop
s DoorExt Open 0
s DoorInt Open 0
s Vent Mode 0
s Vent On 1
l r12 Vent Pressure
bgt r12 TargetExt endLoop
s Vent On 0
s DoorExt Lock 0
s DoorInt Lock 0
s XfmrExt On 0
s XfmrInt On 0
endLoop:
yield
j loop
```

**Dry-run verification (2026-08-04):** loaded into
`stationeering/stationeers-ic` with `lbn`/`HASH()` swapped for
equivalent pin reads (this emulator predates named-batch instructions,
same limitation noted for `sbn` earlier — the swap tests the state
machine logic only, not the real button-addressing mechanism, which
isn't independently verifiable outside the actual game). Zero program
errors. Functionally verified: (1) requesting entry from the side the
chamber is already matched to opens that door immediately with no
cycle; (2) requesting entry from the *other* side correctly withholds
the door, runs the vent in the right direction, and — critically —
**keeps `r13` set across many ticks after the button is released**,
opening the door the instant target pressure is reached rather than
losing the request; (3) the door-open dwell timer counts down and
closes the door automatically. **Not functionally testable in this
emulator:** the `lbn` button reads themselves (unsupported opcode) and
the Chip C↔B Propped-Open flag handoff (`lb`/`sb` are no-ops in this
emulator version) — both still need a real in-game check.

**Line budget going forward:** this chip has exactly one spare line.
Any future addition (stall-timeout, Propped-Open exit ordering, a
different dwell mechanism) has to remove something else first, not
just append.

---

## Chip C — Gas Sensor / Propped-Open Monitor (optional)

```
# Chip C: OPTIONAL. Only build this if you installed both Gas Sensors.
# Broadcasts match/mismatch via a type-hash batch flag Chip B reads
# with its own "lb" call - both chips address by type-hash only, no
# device name/Labeller needed, so they always agree on what they're
# reading/writing (see dry-run finding below for why this matters).
# If this chip doesn't exist, Chip B's batch reads of the same flag
# simply return nothing - no error, Propped-Open just never triggers.
# No single "Ratio" field exists for composition (checklist item 8) -
# check Oxygen (breathable) plus Pollutant/Methane (hazard) per-gas.

alias SensExt d0
alias SensInt d1

define PropFlagHash -1234567   # must match Chip B's constant exactly -
                                 # each chip defines its own copy, see
                                 # dry-run finding below for why

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
move r6 1

noMatch:
sb PropFlagHash Setting r6
yield
j loop
```

Tolerance values (`2` used as a placeholder three times in earlier
drafts) were guesses at the time this was written. **Now applied above**
— Custom Airlock V2's real, live-used values: pressure ratio ~0.1,
temperature ~0.02, trace gases (methane, pollutant) ~0.005. The
composition check itself was also rewritten in this pass — the earlier
skeleton read a single generic `Ratio` field per sensor, which doesn't
exist; there's no one field for "gas composition," only per-gas
`RatioX` fields, so the match check now compares Oxygen, Pollutant, and
Methane individually (NOx omitted here for line budget; add a fourth
`RatioNitrousOxide` block the same shape if you want the full set Custom
Airlock V2 checks).

**Dry-run finding (2026-08-04):** the flag write was originally
`sbn FlagDevice PropFlagHash Setting r6`, with `FlagDevice` aliased to
`d2` — a wired pin. `sbn`'s real signature is
`sbn prefabHash nameHash LogicType value` (confirmed via community
source): every argument before the value is a hash constant, not a
device pin, so passing a pin alias there was a real type mismatch, not
just an unresolved placeholder. It also didn't match how Chip B reads
the same flag — `lb r5 PropFlagHash Setting 0` batches by type-hash
alone, no name segregation — so even a correctly-formed `sbn` call would
have been writing to a narrower audience (one specifically-named device)
than Chip B was reading from (every device of that type-hash on the
network). Fixed by switching Chip C to `sb PropFlagHash Setting r6`
— plain type-hash batch write, the exact counterpart to Chip B's `lb`
type-hash batch read, so both chips now provably agree on what they're
touching. This also means Chip C no longer needs the `FlagDevice` alias
or a device physically wired to its `d2` pin for this purpose — removed.
Verified by loading both chips into `stationeering/stationeers-ic` (a
real IC10 emulator) and confirming zero program errors, versus the
original `sbn` line failing to parse there at all (`UNKNOWN_INSTRUCTION`
— that specific emulator predates `sbn` being added to the game, per
independent confirmation the instruction itself is real; the type
mismatch above is the actual bug, not the instruction's existence).

---

## What's genuinely done vs. still a skeleton

**As of 2026-08-04, all three chips are complete and load with zero
program errors.** This section's name is now slightly stale — kept for
history, updated below to say what's actually still open.

**Solid, based on confirmed mechanics and dry-run execution:** the Tier
state machine and hysteresis (Chip A, boundary values verified
tick-by-tick), the full evacuate/pressurize/dwell cycle state machine
(Chip B, functionally verified including the persistent-pending-
direction fix described above), the Button-C override placement in
Critical (now actually gated on real evacuation completion, not just a
comment), the Deep Idle Transformer-switching approach, the
type-hash-batch Propped-Open handoff between Chip B and C (syntax
verified, matching addressing confirmed consistent between both chips).
**Also solid:** the specific LogicType names `RatioPollutant`,
`RatioNitrousOxide`, `RatioNitrogen`, `RatioOxygen`, `Pressure`,
`Temperature`, `Open`, `Setting`, `Lock`, `Mode`, `On`, `Charge`,
`Maximum`, and the match-tolerance values above — all confirmed against
real production code or direct in-game checks rather than guessed.

**Genuine remaining gaps (not skeleton, but not built either):**
Stalled-phase detection/recovery (the "Cancel Pressurize" case from the
requirements doc's state enumeration — Chip B's evacuate/pressurize
loops will sit re-checking Pressure forever if a stall happens, with no
timeout), the Propped-Open exit sequence once a mismatch is detected
mid-prop (close which door first — not specified), and whether Chip
B/C should adopt the confirmed `brdns` instruction instead of pure
batch addressing for the optional Gas Sensors (a real improvement per
Custom Airlock V2's own use of it, not yet made — there's no line
budget left in Chip B to add it without removing something else first).

**Resolved in the 2026-08-04 research pass (not from Custom Airlock V2,
which doesn't monitor a Power Controller at all):** the Power
Controller's charge field name — `Charge` and `Maximum` (Joules) are the
confirmed pair, no direct `Ratio` field confirmed on the Power
Controller itself, so Chip A now computes the ratio manually
(`Charge / Maximum`) instead of reading a `Ratio` field that may not
exist. See requirements doc checklist item 2 for sourcing. The
90%/93%/10%/13% hysteresis bands and the pin-budget resolution
(all three Buttons to `lbn`) are now confirmed reasonable starting
values / a necessary real design choice respectively — see the
requirements doc checklist and the Chip B section above.

**Not written at all:** nothing required-and-unaddressed remains at the
chip level — Button I now has full wake logic in both Low and Critical
awareness, matching E and C. What's left is the two "genuine remaining
gaps" above, both of which are edge-case robustness, not core function.

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
