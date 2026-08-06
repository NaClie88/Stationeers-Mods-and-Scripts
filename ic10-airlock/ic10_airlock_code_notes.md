# IC10 Airlock — Code Notes

Design rationale, corrections, and dry-run verification for
`watcher.ic10`, `cycle.ic10`, and `gas_sensor.ic10` — those are the
files to open in-game and paste from (`ic10_airlock_scripts.md` is a
one-page index to them). This one's for *why* it looks the way it
does: what got fixed, what got restructured, and what's still
unverified. For hardware and wiring, see `ic10_airlock_setup_guide.md`.

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
owner. The scripts stay reasonably short per line as a matter of
in-editor readability, not because they have to.

**2. Stack is persistent — a confirmed, real gotcha.** Values pushed to
an IC10's stack survive script reloads and restarts. Community reports
describe scripts breaking after game updates specifically because of
stale stack garbage from before. **All three scripts avoid the stack
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

Always powered. Full code in `watcher.ic10` — 112 of 128
lines as formatted there, comfortable margin remaining.

**Pins:** `d0` dedicated Power Controller, `d1` warning LED (device
type `StructureDiode`/"LED" — see correction below for why this isn't
a plain Light), `d2` Cycle-zone gate (a Power Controller, not a
Transformer — see above), `d3` Logic Transmitter (Active mode).

**Correction (2026-08-04, confirmed via in-game screenshots):** the
original design across this entire project — since before this
Watcher/Cycle restructuring even existed — assumed a Light exposes a
`Setting` LogicType for exactly this kind of "repurpose an unrelated
field as a signal flag" trick. **It doesn't.** The project owner
screenshotted a standard Light's Logic panel (`Power`, `Lock`, `On`,
`RequiredPower`, `PrefabHash`, `ReferenceId`, `NameHash` — no
`Setting`) and the "battery backup" Light variant too (same set plus
`Mode`, still no `Setting`). **Fix, part 1:** Tier now goes out via the
Logic Transmitter instead of through any Light-family device at all.

**Correction, part 1b (2026-08-05) — the Transmitter/Receiver mechanism
itself was also wrong, caught from a saved copy of the Community Wiki's
"Logic Transmitter" page.** Earlier drafts assumed a "Logic Receiver"
device paired to a Transmitter over one of 8 numbered `Channel0`–
`Channel7` fields, tuned via a console channel setting. **None of that
exists.** There is only one device, **Logic Transmitter**
(`StructureLogicTransmitter`), used in either **Active** or **Passive**
`Mode` (0/1) — a "Receiver" is just a second Logic Transmitter set to
Passive. It has exactly one value field, `Setting` (type "Any"), not
eight channels. Pairing is a physical, in-game, one-time action: adjust
a dial on the Passive unit until it shows the Active unit's name — not
an IC10-settable numeric channel at all. **Fix:** Watcher sets its unit
to Active (`s Transmitter Mode 1`, once, before the loop) and packs all
four values it needs to send — Tier plus the three live button states —
into a single number written to `Setting`: `BtnC*1000 + BtnI*100 +
BtnE*10 + Tier`. Cycle's unit is set to Passive (`s Receiver Mode 0`)
and unpacks that same number back into four registers via chained
`mod`/`div`/`floor`, once at the top of every loop iteration.

**Confirmed via decompilation, 2026-08-06 (`logic-network-reference`
branch, `devices/logic-transmitter.md`):** the mechanism above is
accurate, and one previously-unconfirmed assumption now has real
evidence behind it — **a Passive unit ("Receiver") needs its own power
to read its paired Active unit's `Setting` at all.** A Passive
Transmitter's `CanLogicRead`/`GetLogicValue` for `Setting` return
false/0 whenever it has no power of its own, *regardless* of whether
the Active unit it's paired to is broadcasting fine. This doesn't
change anything about this design specifically — Cycle (which houses
the Receiver) is only ever powered when it would need to read `Setting`
anyway — but it was previously just an assumption, not a confirmed
fact, and is worth knowing if this architecture is ever adapted to a
build where the Receiver might sit on an always-on circuit.

**Fix, part 2 — the player-facing indicator got upgraded, not just
patched.** A further screenshot of the LED (`StructureDiode`, 25W) showed
it has a `Color` LogicType (Read/Write) that neither Light variant
does. Rather than settle for the simplest fix (see footnote below),
Watcher now drives the LED's actual color per Tier: green in Normal,
yellow in Low, red in Critical — closer to the three visually-distinct
states the original design wanted than a plain on/off ever could be,
and through a mechanism that's confirmed to exist rather than one that
wasn't. **`ColorGreen`/`ColorYellow`/`ColorRed` (2/5/4) are not yet
independently confirmed** — sourced from aggregated search results
citing the Community Wiki's "Data Network Colors" page, which has
resisted every direct fetch attempt this project has tried. Verify
against that page (or in-game trial) before trusting the exact values;
the branching structure that picks a color per Tier is solid regardless
of what the three numbers turn out to be.

**Footnote — the simpler on/off version, kept for posterity.** Before
settling on the LED/Color approach, this fix was implemented as a
plain binary indicator on a standard Light: `sgt r8 r0 0` then
`s Light On r8` (off in Normal, on for Low or Critical) in place of the
whole color-branch block above, with `alias Light d1` instead of
`alias LED d1`. Dry-run verified working at the time. If the Color enum
values above turn out to be wrong and a quick fix is needed, this
three-line swap is the fallback — real, tested, just less informative
than the color version.

**`BtnHash` (`-1591419276`)** is a community-sourced Logic Switch
structure hash, found from a single search result rather than
cross-confirmed — treat it as a strong lead, not a certainty, and
double-check it against your own Stationpedia entry. Same caveat for
the exact LogicType that gates a Power Controller's own output
(assumed `On`, matching every other powered device confirmed in this
project, but not independently verified — flagged for an in-game Logic
Reader check).

**Real bug, found 2026-08-05 via direct decompilation of
`Assembly-CSharp.dll` (on the `airlock-mod-card` branch, cross-checked
against this file since it affects `watcher.ic10` too) — fixed
2026-08-06.** Lines 32-35 used to compute `r1` (the Tier-decision
percentage) as:

```
l r1 Battery Charge
l r2 Battery Maximum
div r1 r1 r2
mul r1 r1 100
```

Ground truth from `AreaPowerControl.GetLogicValue` (the Power
Controller's real C# class — confirmed the same device as "Area Power
Controller," there's only one class in the whole game):

```csharp
LogicType.Charge => AvailablePower,   // InputNetwork.PotentialLoad + Battery.PowerStored
LogicType.Maximum => Battery.PowerMaximum,
LogicType.Ratio => Battery.PowerStored / Battery.PowerMaximum,   // clean 0-1, no division needed
```

`Charge` is **not** the dedicated battery's own stored charge — it's
that plus whatever the Power Controller's input network is currently
drawing. If the dedicated Power Controller has any live charging input
at read time (a small solar panel, a trickle from the main grid,
anything), `r1` comes out inflated above the battery's true percentage,
undermining every hysteresis threshold below — all of which assume a
clean 0-100 reading. `LogicType.Ratio` gives exactly that in one read,
already confirmed legally readable on this device (`CanLogicRead`'s
own range check, verified against real `LogicType` enum ordinals:
`Ratio` = 24, `Maximum` = 23, and the check is `logicType - 23 <=
LogicType.Mode(3)`, i.e. `logicType <= 26` — both pass). Fix applied:
those four lines are now `l r1 Battery Ratio` + `mul r1 r1 100`,
dropping the `Maximum` read and `div` entirely.

**Why this wasn't caught earlier**: `SOURCES.md`'s entry for this
(the "real working script" citation) could only confirm `Ratio` exists
as a *general* LogicType, not that it's specifically exposed on Power
Controller — the Charge/Maximum-division approach was the
secondhand-sourced fallback when that couldn't be pinned down. Direct
decompilation resolves it now. This is exactly the class of mistake
prompting a broader project (see `logic-network-reference` branch,
2026-08-06): Stationeers' community-sourced LogicType documentation is
good but not always complete or precise per-device, and this project
has been burned by it more than once (see the Light `Setting` and
Logic Transmitter/Receiver corrections above, both in this same
file) — worth building a decompiled ground-truth reference instead of
re-guessing per script.

**Fixed in `watcher.ic10` itself, 2026-08-06** (project owner
confirmed applying the fix without waiting for a live-Charge-inflation
observation first, given the decompiled source, ordinal check, and
cross-check against `devices/power-controller.md` left no ambiguity).
Still worth a sanity check with a Logic Reader on a real Power
Controller with active charging input, if only to see the now-fixed
`Ratio` read behave as expected end-to-end in-game.

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

**Additional dry-run pass (2026-08-04, the LED/Color fix):** the same
emulator confirms `LED On` stays 1 throughout (always lit, color is
what carries meaning now), and `LED Color` lands on green/yellow/red
exactly at the tier boundaries, including recovering to green on the
trip back to Normal — all consistent with the one-tier-per-tick
transition behavior already established for the hysteresis state
machine itself.

**Further dry-run pass (2026-08-05, the Transmitter/pack-unpack fix):**
verified the packing math in isolation across several combinations of
Tier and button states (e.g. Tier=1, only BtnI held → packs to `101`;
Tier=2 with all three buttons held → packs to `1112`) — each correctly
round-trips through Cycle's unpack sequence (below) to the exact
original values. Also ran a full Watcher-packs → Cycle-unpacks handoff
end to end (packed value produced by one chip fed directly into the
other), confirming the two sides agree on the encoding. Line count rose
from 103 to 112 — comfortable margin remains.

---

## Cycle — Doors, Vent, chamber sensor

Powered only when Watcher's zone gate is on. Full code in
`cycle.ic10` — 122 of 128 lines as formatted there — **tighter than
before** (was 115), see the unpack-sequence note below for why, and
keep this in mind before adding anything further to this chip.

**Pins:** `d1`/`d2` exterior/interior Portal, `d3` Vent, `d4` Logic
Transmitter (Passive mode — "Receiver" is this project's alias name for
it, not a separate device; see Watcher's part 1b correction above for
the full mechanism), `d5` a **dedicated Gas Sensor physically inside
the chamber** (new hardware — see "Bonus" note above for why). `d0` is
unused — this chip no longer needs a Light pin at all: Tier arrives
over the Receiver instead, which also retires the "cross-circuit data
wiring to the Light" item that used to sit in the still-open list
below, since there's no longer any Light-related wiring on this chip to
verify.

**The unpack sequence at the top of the loop** (`mod`/`div`/`floor`
chained three times) is why this chip's line budget got noticeably
tighter — 9 lines to undo Watcher's `BtnC*1000 + BtnI*100 + BtnE*10 +
Tier` packing back into four separate registers (`r0`=Tier, `r6`=BtnE,
`r7`=BtnI, `r8`=BtnC), once per loop iteration, before any tier
branching happens. Those four registers are then referenced exactly
where the old direct-channel reads used to be — `r6`/`r7` inside
`cycleCheck`, `r8` inside `tierCrit` — so nothing downstream of the
unpack changed at all.

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
same tick as closing; (5) Button C held during Critical (relayed live,
unpacked into `r8` from the Transmitter's `Setting` value) skips the
entire evacuation branch for that loop, leaving doors/locks exactly as
they were; (6) the Propped-Open branch parses
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
Transformers or Buttons, so nothing about the split affected it. Full
code in `gas_sensor.ic10`; verification below.

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
- `ColorGreen`/`ColorYellow`/`ColorRed` (2/5/4) — sourced from
  aggregated search results citing the Community Wiki's "Data Network
  Colors" page, which blocked every direct fetch attempt. The
  color-per-Tier branching logic itself is solid regardless of the
  exact numbers.
- The pack/unpack encoding (`BtnC*1000 + BtnI*100 + BtnE*10 + Tier`) is
  dry-run verified as internally consistent — Watcher's packer and
  Cycle's unpacker agree with each other — but the actual wireless
  Active/Passive link between two real, physically-paired Logic
  Transmitters across two independently-power-gated circuits hasn't
  been tested in a running game. Also: the two units need a one-time
  manual pairing step in-game (adjusting the Passive unit's dial to the
  Active unit's name) — not scripted, easy to forget, worth calling out
  clearly in the setup guide.
- Stalled-phase detection/recovery, and Propped-Open's mid-mismatch
  exit ordering — unchanged gaps from before this restructuring.
- Whether `brdns` should replace pure batch addressing for the optional
  Gas Sensors — a real improvement per Custom Airlock V2, not yet made.

**Roadmap to 1.0 (agreed with project owner, 2026-08-06):** step 1,
next session — close out the Charge/Ratio bug, `BtnHash`, and the
Color enum above using the `logic-network-reference` branch's
`ilspycmd` decompilation toolchain (the same approach that already
resolved the Charge/Ratio question and the Logic Transmitter `Setting`
mechanism there), for a genuinely no-known-issues IC10-only 1.0. Steps
2-3 (real mod hardware wiring, then a full rename) continue on
`airlock-mod-card` — see that branch's `README.md` for the full
sequence.

**Resolved in-game (2026-08-04):** a standard Light — and separately,
the "battery backup" Light variant — have no `Setting` LogicType at
all, confirmed by the project owner's own in-game Logic panel
screenshots. This was a project-wide assumption baked in since before
any of this session's work, silently broken the entire time. Fixed by
moving Tier onto the Logic Transmitter instead of any Light-family
device, and upgrading the player-facing indicator to an LED driven by
its `Color` field — a real mechanism the Light variants don't have,
giving back the three visually-distinct states the original design
wanted. See the Watcher section above for the full explanation, the
simpler on/off fallback kept for posterity, and dry-run verification.

**Resolved in-game (2026-08-05):** the Logic Transmitter/Receiver
mechanism itself was also wrong from the start — there is no "Logic
Receiver" device, no `Channel0`–`Channel7`, and no console-set numeric
channel. Confirmed from a locally-saved copy of the Community Wiki's
"Logic Transmitter" page (the live page, like most on that wiki, blocks
automated fetches). The real device is a single "Logic Transmitter"
used in Active or Passive `Mode`, exposing one `Setting` field, paired
by physically tuning the Passive unit's dial to the Active unit's name
in-game. Fixed by packing all four values Cycle needs (Tier + 3 button
states) into one number written to `Setting`, unpacked back into four
registers on the other end. See the Watcher and Cycle sections above
for the full mechanism and dry-run verification.

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
