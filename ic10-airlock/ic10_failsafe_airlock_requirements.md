# IC10 Fail-Safe Airlock — Script Requirements

## Correction: language

IC10 is **not Lua**. It's a low-level assembly language directly inspired by
MIPS — confirmed via the Community Wiki and multiple player-written guides.
Characteristics that matter for writing this script:

- 16 general-purpose registers (`r0`–`r15`), plus a few special ones (`ra`,
  `sp`) that aren't meant for everyday variable storage
- A 512-value stack (`sp`), last-in-first-out
- **Hard limit: 128 lines of code, 90 characters per line** — this bounds
  how ambitious a single script can be before it needs splitting across
  multiple IC10 chips
- No types — everything is a number. "On" is `1`, "off" is `0`, temperature
  is a float in Kelvin. No strings, no booleans as a distinct type.
- Devices expose readable/writable **LogicTypes** (named properties) over
  6 device pins (`d0`–`d5`) per IC Housing, or a couple on select devices
- Higher-level compilers exist (compIC10, BasIC-10) that compile down to
  raw IC10-MIPS for people who'd rather write loops/if-statements in
  something more familiar — worth considering once the logic below gets
  complex, but the requirements themselves are language-agnostic.

## Chip type distinction (confirmed)

| Chip/Board | Programmable? | What it does |
|---|---|---|
| **Circuitboard (Airlock)** | No — hardcoded | Basic two-portal atmosphere-to-vacuum or atmosphere-to-atmosphere cycling, fixed behavior set via console settings, not scriptable logic |
| **Circuitboard (Advanced Airlock)** | No — hardcoded | Same category, adds interior/exterior distinction and a few more built-in behaviors, still fixed logic, not a script |
| **IC10 chip** | **Yes** | Fully programmable via IC10-MIPS assembly, placed in an IC Housing (or the programmable slot on select devices like a Console). This is the only option that can implement custom multi-stage logic like the fail-safe design below — neither airlock circuitboard can. |

**Implication:** the fail-safe behavior in this spec requires an IC10 chip
running a custom script — it cannot be built with either stock airlock
circuitboard alone, confirmed by their hardcoded nature. This is why
Project H2/H3 in the main guide specify IC10, not just "Advanced Airlock."

**Architecture: the IC10 replaces the circuitboard, it doesn't run
alongside it.** For this design, the Portals and Active Vents wire
directly to the IC Housing (or an IC10-equipped Console) instead of to a
Circuitboard (Airlock) or Circuitboard (Advanced Airlock). The IC10 owns
Lock/Open/Close on those doors outright — there's no separate hardcoded
circuit also trying to control the same door, so there's no conflict to
resolve between two controllers. This replaces the earlier open question
in this doc about whether an IC10 write can "override" a circuitboard's
lock — it doesn't need to, because in this design there is no
circuitboard in the loop at all.

## Multiple IC10 chips, coordinating via shared device state

Confirmed as standard, established community practice (not a workaround):
IC10 chips don't talk to each other's internal registers directly —
registers are private to each chip. Instead, chips coordinate by writing
to and reading from a **shared device's LogicType**, the same way any
IC10 talks to any sensor or light. Two ways to do this:

1. **Repurpose an existing device's LogicType as a signal flag** — e.g.,
   one chip writes a Light's `Setting` to a specific value not otherwise
   meaningful for lighting, a second chip reads that same Light's
   `Setting` to know what the first chip decided. Confirmed as a real
   pattern used by the community for exactly this kind of inter-circuit
   signaling.
2. **Logic Transmitter / Logic Receiver pair** — purpose-built for this:
   a Transmitter broadcasts a `Mode` value on a chosen channel, a Receiver
   elsewhere picks it up, no direct wire needed between them. Each
   transmitter only carries one value per channel — multiple signals need
   multiple pairs or channels.

**Deliberate choice for this design: option 1, a Light, specifically for
its dual purpose.** Unlike a Transmitter/Receiver pair — which is purely
machine-to-machine, invisible to a player standing at the airlock — a
Light physically placed at the portal does double duty: the same write
that tells a second IC10 "this portal's power state is uncertain" also
gives the **player** a visible, in-world signal at the point where it
matters most. A player walking up to an airlock doesn't need to check a
console reading to know something's wrong — the light at the door itself
tells them. This is the right tool specifically because the requirement
isn't just inter-chip signaling, it's also player-facing warning at the
exact location the warning is relevant to, and a Light is the one option
that serves both jobs from a single write.

**Requirement (added):** the Light's placement matters as much as its
LogicType value — it needs to be visible from the portal itself (not
tucked in a rack with other indicator lights), since its second job is
informing the player standing there, not just the paired IC10.

**Why this matters for the 128-line limit:** if the full state machine
(normal cycling + Charge monitoring + staged fail-safe response) doesn't
fit in one IC10's 128 lines, split it — e.g., one chip owns door/vent
cycling and reads the shared light's Setting for "are we in fail-safe
mode," a second chip owns Power Controller monitoring and writes that
Setting (the same write that lights up the warning for the player).
Neither chip needs to know the other's internal logic, only the shared
LogicType they both touch.

## The core problem this script solves

Confirmed from prior research (see main guide Project H3): once an airlock
circuit powers on and locks its doors, that lock **persists through power
loss** — a locked door stays locked forever, Crowbar included, regardless
of current power state. A naive automated airlock with no awareness of its
own power source failing is a trap risk.

The fix isn't just "have backup power" (batteries eventually drain too) —
it's a script that **proactively degrades and unlocks before power is
fully gone**, so the worst case is an unpowered-but-unlocked door (still
Crowbar-operable) rather than a locked door with a dead battery behind it.

## Power Controller physical placement — requirement

**All dedicated Power Controllers for this airlock must be physically
located inside the airlock chamber itself**, not in the base interior
and not outside. This directly addresses the true worst case: total
battery drain during Critical, with a player caught inside.

The Power Controller's battery is confirmed swappable with any hand-tool
battery (small battery) — but that only helps if the player who's stuck
can actually reach it. Placing it inside the chamber means a player who
finds themselves there when everything's gone dead can crowbar the
Power Controller open (same tool, same access method already established
for the doors — see Community Wiki "Crowbar" page) and swap in a fresh
battery themselves, restoring power without needing anyone outside to
notice or help. This is the design's actual backstop underneath
everything else — Deep Idle Mode, the staged thresholds, and the Button
C override all reduce how *often* someone gets stuck, but this is what
guarantees they're never depending on rescue from outside to get
themselves out.

**Checkpoint:** confirm the Power Controller kit's footprint fits inside
your chamber's dimensions alongside the two Portals, the signal Light,
and Button C, without the chamber becoming so cramped it's awkward to
use day-to-day.

## Deep idle mode: doors off, wake on Button

**Confirmed worth the added complexity.** Two facts make this a real
power saving, not a marginal one:

- **Buttons/Switches cost nothing to monitor even fully unpowered.**
  Confirmed via Community Wiki "Logic Switch" page: losing power doesn't
  affect a Button's functionality, only its indicator light stops
  working. An IC10 can read a Button's `Activate`/`Setting` state
  regardless of whether that Button has power at all.
- **Doors draw real continuous standby power while active**, not just
  during actuation. Confirmed for Blast Doors specifically: 25W per tick,
  continuously, in the same class as a Wall Light (25W) or an IC10 chip
  (25W) — not a trivial background cost. Standard Composite/Glass Door
  wattage specifically wasn't confirmed in available sources; check your
  in-game tooltip for the exact figure, but it's a powered device in the
  same category, not free.

**CONDITIONAL BUILD REQUIREMENT — confirm before treating as final:**
if in-game testing confirms a Portal's power **cannot** be cut via a
direct IC10 LogicType write alone (no `s Portal <field> 0`-style command
that actually de-energizes it while leaving data/logic connected), then
**each Portal requires its own Transformer (or equivalent switching
device) in its power line, wired separately from the circuit feeding the
IC10 chips, Buttons, and Light.** This was surfaced while writing the
actual prototype code (see `ic10_airlock_prototype_code.md`), not
designed in from the start — without it, cutting a Portal's power for
Deep Idle would also cut power to whatever's supposed to be watching for
the wake trigger, since the monitoring loop can't survive on the same
circuit it's trying to power down. The controller chip would toggle the
Transformer, not the Portal directly.

**Checkpoint:** in-game, attempt a direct power LogicType write to a
Portal while its data connection stays live. If the door successfully
de-energizes (loses lock-persistence risk, becomes Crowbar-eligible)
while the IC10 itself keeps running and can still read Button/Light
state, the Transformer is unnecessary — pure logic suffices. If the
Portal's power can't be cut independently of its controlling circuit,
add "Transformer per Portal" to the build list as confirmed-required
hardware, not just a contingency.

**Requirement:** in State 2 (Low Power), between cycles, cut power to
the Portal itself rather than just dimming the warning light. Since an
unpowered door is *also* the target safe state from State 3 (unpowered +
unlocked, Crowbar-operable), this isn't a separate risk to manage — it's
the same safe state, reached earlier and now used as the *normal* idle
condition instead of an emergency-only one. Three Buttons — each costing
nothing to keep monitoring — are what wake the door back up:

- **Button E (Exterior)** — outside the airlock, for someone approaching
  from outside requesting a cycle
- **Button I (Interior)** — inside the base, for someone approaching from
  inside requesting a cycle
- **Button C (Chamber)** — mounted *inside the airlock chamber itself*
  (the buffer between the two portals), for the case where a player is
  already standing in the chamber when Charge crosses into State 2 and
  the doors power down around them. Without this third button, that
  player would have no way to request a cycle from inside a now-unpowered
  chamber except waiting for someone else to press E or I from outside —
  Button C exists specifically so they're never stuck waiting on someone
  else.

1. Door(s) sit unpowered + unlocked between uses (the confirmed-safe state)
2. IC10 continuously reads all three Buttons' state, at effectively
   zero power cost regardless of the doors' own power state
3. On any Button press, IC10 powers the relevant door(s), cycles for
   passage, then drops back to unpowered+unlocked once clear
4. The warning Light (see "Multiple IC10 chips" above) still fires the
   same dual-purpose signal at the State 2 threshold — this doesn't
   replace that, it's an additional saving layered on top

**Net effect:** State 2 goes from "same power draw, dimmer light" to
"door draws ~0W between uses, only spending power during an actual
cycle" — a real reduction, not just a visual warning, for the cost of
one more condition in the script (check Button, then act) rather than
a passive always-on door.

Cycle latency is worth verifying before locking this in as default — see
"In-Game Verification Checklist" below, item 4.

## Staged power-failure response — requirements

Three states, driven by monitoring the dedicated Power Controller's
`Charge` value (see main guide H3 for why this airlock has its own
isolated Power Controller rather than running off the main grid):

### State 1 — Normal (Charge > 90%)
- Full functionality: airlock cycles normally, lights at full brightness,
  doors lock during active cycling as designed
- Requirement: script continuously reads Power Controller `Charge` every
  loop iteration to detect the transition into State 2

### State 2 — Low Power Warning (Charge ≤ 90%, > 10%)
- **Requirement:** the portal's dedicated signal Light (see "Multiple IC10 chips" above) changes state to a dual-purpose warning — the same write both (a) informs any second IC10 reading that Light's Setting that this portal's power is uncertain, and (b) visually warns a player standing at the airlock that something's degraded, before they're relying on it mid-cycle
- **Requirement (see "Deep idle mode" above):** door power cuts between cycles rather than staying continuously powered — door sits unpowered+unlocked at rest, powers up on any of the three Buttons (E/I/C) being pressed, drops back to unpowered+unlocked once clear. This is now the state's primary power saving, not just the warning light.
- Open questions (LogicType name for Charge, absolute-vs-relative threshold) consolidated in "In-Game Verification Checklist" below — items 2 and 3

### State 3 — Critical (Charge ≤ 10%)
- **Requirement:** any currently-open door in the airlock closes
- **Requirement:** the airlock chamber's atmosphere is evacuated (pumped
  out to the connected pipe network / storage) before doors are
  potentially left stranded mid-cycle — prevents an explosive-decompression
  scenario if power dies mid-pressurization
- **Requirement — the critical safety behavior:** doors are explicitly
  **unlocked** at this stage, not left in their locked state. Since the
  IC10 owns the door directly (no circuitboard in the loop — see
  Architecture note above), writing its Lock LogicType to 0 is a direct,
  uncontested action, not a fight against another controller's state.
  This is the whole point of the script: if the dedicated Power
  Controller's battery fully drains after this point, the doors are
  unpowered AND unlocked, which is the one state confirmed
  Crowbar-operable.

## Requirements summary (testable)

1. Script reads dedicated Power Controller `Charge` (or equivalent) every
   loop cycle
2. Transition to State 2 triggers non-essential light dimming/shutoff,
   reversible if Charge recovers above 90%
3. Transition to State 3 triggers, in order: close any open doors → pump
   chamber atmosphere to storage → unlock doors
4. State 3's unlock action leaves the door genuinely Crowbar-operable
   once fully unpowered — confirmed reliable specifically because the
   IC10 owns the door directly (no circuitboard sharing control, see
   Architecture note), so the write isn't contesting another controller
5. Script stays under the 128-line / 90-char-per-line hard limit per
   chip — if the full state machine doesn't fit in one chip, split
   across multiple IC10s coordinating via a shared device LogicType or a
   Logic Transmitter/Receiver pair (see "Multiple IC10 chips" above),
   not by falling back to a hardcoded circuitboard for part of the logic
6. Behavior is testable in isolation: simulate Charge dropping through
   each threshold and confirm the corresponding action fires exactly once
   per transition, not repeatedly every loop while sitting in that state

## Not yet specified (next steps once ready to write actual code)

- Exact LogicType names — see "In-Game Verification Checklist" above,
  item 5, for the specific fields still needed
- Whether the full state machine fits in one IC10 or needs splitting
  across multiple chips (see "Multiple IC10 chips" section above) — depends
  on how much normal cycling logic (door sequencing, vent pressure
  targets) plus the fail-safe state machine actually costs in lines once
  written
- Recovery behavior detail beyond the current lean (manual/console
  acknowledgment) — exact UI/trigger for that acknowledgment isn't
  designed yet

## In-Game Verification Checklist

Everything below is flagged elsewhere in this doc individually — collected
here in one place so nothing gets missed before writing real code:

1. **Standard Door wattage** — only Blast Door's 25W/tick is confirmed.
   Check a Composite/Glass Door's actual idle and cycling draw via its
   in-game tooltip or Stationpedia entry.
2. **Exact LogicType name for Power Controller charge** — commonly
   referenced as `Charge` in community scripts; confirm against the
   in-game Logic Reader "VAR" list for your version before committing to
   a variable name.
3. **Whether 90%/10% thresholds should be absolute or relative** —
   depends on which battery is in the dedicated Power Controller; check
   whether a `Ratio`/percentage-style LogicType exists, or whether the
   script needs to hardcode the known battery's max capacity.
4. **Cycle latency from Deep Idle Mode** — how long a door takes to
   become responsive when powered on-demand from fully off, versus an
   always-powered door's instant response. Test before committing Deep
   Idle Mode as State 2's default rather than an optional deeper mode.
5. **Exact LogicType names for Lock/Open and vent/pipe evacuation
   controls** — needed for State 3's close→evacuate→unlock sequence;
   confirm against the Logic Reader VAR list, not assumed from memory.
6. **Hysteresis gap size** — the 3-point gap used above (90%/93%,
   10%/13%) is a starting guess; observe actual Charge fluctuation
   tick-to-tick in-game and adjust if it's too tight (flapping) or too
   loose (sluggish recovery).
7. **Chamber footprint** — confirm the Power Controller kit fits inside
   the chamber alongside both Portals, the signal Light, and Button C
   without making the space awkward to actually use.
8. **Gas Sensor LogicType names** — pressure, temperature, and gas
   composition ratios needed for the Propped-Open match check; confirm
   exact field names against the Logic Reader VAR list, same caveat as
   the other LogicTypes above.
9. **Actual Powered/Large Powered Vent wattage** — confirmed as "slightly
   higher" than standard Active Vent's 100W and confirmed 2×/4× pressure
   throughput, but exact watt figure wasn't found in available sources.
   Needed to size the dedicated battery correctly if using one of these
   instead of a standard Active Vent (see the vent/battery warning above).
10. **IC10 line-length limit — sources conflict.** One reference says
    128 lines × 90 characters; another (more actively maintained) says
    128 lines × 52 characters. These can't both be current. Check your
    actual in-game editor's line-length cutoff before writing real code
    against either figure — the prototype code was kept conservative
    (short lines) specifically to be safe under whichever is correct.

## Current Door Logic — Walkthrough

Putting the pieces above together, here's how the airlock actually
behaves end to end, state by state.

**Hardware in place:** one IC10 (in an IC Housing) wired directly to
both Portals and the Active Vent(s) — no airlock circuitboard in the
loop. A dedicated, isolated Power Controller (its own battery, off the
main grid, physically inside the chamber for battery-swap access) feeds
this IC10 and the doors. One signal Light mounted at the portal. Three
Buttons: E (exterior), I (interior/base side), C (inside the chamber
itself). Two Gas Sensors — one exterior-facing, one interior-facing —
feeding pressure/temperature/composition data continuously, used for
the Propped-Open exception described below.

**Every loop iteration, regardless of state:** the script reads the
dedicated Power Controller's `Charge` first. That single value decides
which of the three states below governs everything else this loop.

**State 1 — Normal (Charge > 90%):** the airlock behaves like a
standard automated airlock. Doors stay powered and lock during an active
cycle, per the normal safety behavior for pressure changes. Any of the
three Buttons can request a cycle; the script sequences vent
evacuation/pressurization and door open/close in the usual order. The
Light stays in its normal (non-warning) state. The two Gas Sensors run
continuously in the background here too — if they ever confirm a
genuine match on both sides, the airlock can prop both doors open
instead of cycling for no reason, reverting the instant either sensor
detects the match has broken.

**Transition into State 2 (Charge drops to ≤90%):** the Light switches
to its warning state — the single write that both signals a second IC10
(if one exists) that this portal's power is uncertain, and visibly warns
any player standing at the portal. From this point on, doors no longer
sit powered between uses: after any cycle completes, the door drops to
unpowered+unlocked rather than staying live. The script now spends most
of its time in an idle loop doing exactly one thing — reading E, I, and
C — until one of them fires. A press on any of the three powers up the
relevant door, runs the cycle, then returns it to unpowered+unlocked
once the player's through. Button C exists for the specific case where
someone's caught standing in the chamber itself when this transition
happens — they're not depending on someone else pressing E or I from
outside to get them moving again.

**Transition into State 3 (Charge drops to ≤10%):** the script stops
treating the airlock as available for normal cycling. Any door currently
open closes. The chamber's atmosphere gets pumped out to the connected
pipe network/storage rather than left mid-cycle. Then, critically, both
doors are explicitly unlocked — a direct, uncontested write since the
IC10 owns the door outright. From here, if the dedicated Power
Controller's battery finishes draining completely, the doors are left
unpowered *and* unlocked — the one state confirmed Crowbar-operable — so
a total power failure still leaves a manual way through, not a sealed
trap. In this state the three Buttons stop being the primary way through
(there's no power left to run a scripted cycle) — the Crowbar becomes
the fallback method, matching the same unpowered+unlocked state the
script deliberately created.

**Recovery (Charge climbs back up):** deliberately not automatic. The
script doesn't silently snap back to State 1 the moment Charge
recovers — that risks a player trusting the airlock's automation again
based on stale assumptions. Current lean is a manual/console
acknowledgment step before returning to State 1, though this is still
open (see "Not yet specified" below).

## Complete Discrete State Enumeration

The walkthrough above tells the story in narrative order. This section
is the coverage check — every dimension that varies, and every reachable
combination, so nothing gets missed. The three "states" discussed so far
(Normal/Low Power/Critical) are only one axis. The full space has four.

### The four independent dimensions

1. **Power Tier** — Normal / Low Power / Critical (driven by Power
   Controller `Charge`, as established)
2. **Cycle Phase** — what the airlock mechanism is actually doing right now
3. **Portal Configuration** — the combined open/closed state of both
   portals together (not each portal in isolation — see invariant below)
4. **Occupancy** — whether a player is currently standing in the chamber

Dimensions 1 and 4 apply across the board. Dimension 2's *available*
phases depend on which Power Tier you're in — Low Power and Critical
don't offer the same phases Normal does. Dimension 3 is constrained by
one hard invariant that limits which combinations are ever reachable.

### Invariant (revised — one designed exception)

**Both portals open simultaneously is forbidden, with exactly one
designed exception: a verified match state.** The original purpose
holds — this prevents a straight-through vent between two differing
environments — but if dedicated gas sensors on *both* sides confirm
matching pressure, temperature, and breathable gas composition
simultaneously, there's nothing hazardous left to prevent crossing, and
propping both doors open avoids the overhead of cycling for no reason.
**This requires new hardware:** a Gas Sensor on the exterior side and a
Gas Sensor on the interior side, both feeding the IC10 continuously —
not just to decide whether to enter this state, but to keep monitoring
*while* propped open, since a mismatch developing on either side (someone
changes one room's atmosphere) needs to trigger an immediate return to
normal closed operation. Every other state list below still enforces
the original both-open-forbidden rule; this is the one carved-out case.

### Cycle Phase, per Power Tier

**Normal:**
| Phase | Portal Config | Description |
|---|---|---|
| Idle-Exterior | Both closed, chamber matched to exterior | Resting state, ready to receive from outside |
| Idle-Interior | Both closed, chamber matched to interior | Resting state, ready to receive from inside |
| Evacuating | Both closed, venting chamber toward exterior/vacuum | Confirmed real phase — "depressurization" in the wiki's airlock guide |
| Pressurizing | Both closed, filling chamber from interior source | Confirmed real phase |
| **Stalled** | Both closed, phase halted | **Confirmed real state, not hypothetical** — the wiki's Advanced Airlock guide documents pressurization stalling if there isn't enough gas to reach the target pressure, with a "Cancel Pressurize" button to skip it. Any complete script needs to handle this, not assume phases always complete. |
| Exterior-Open | Exterior open, interior closed | Transit in progress, outside-facing |
| Interior-Open | Interior open, exterior closed | Transit in progress, inside-facing |
| **Propped-Open** | **Both open** | **The one exception to the invariant.** Entry: both Gas Sensors confirm matched pressure/temp/breathable composition. Exit: either sensor detects a mismatch developing — immediately close and return to Idle. |

**Low Power:** the phase list collapses — most of Normal's phases only
make sense with a continuously-powered door. Low Power replaces them
with:
| Phase | Portal Config | Description |
|---|---|---|
| Deep-Idle | Both closed, unpowered, unlocked | The new default resting state (see "Deep idle mode") |
| Waking | Both closed, powering up | Button (E/I/C) pressed, door spinning up before a cycle can start |
| Cycling | Same Evacuating/Pressurizing/Exterior-Open/Interior-Open phases as Normal | Runs once woken, identical mechanics to Normal, just power-gated by the Waking phase first |
| Returning-to-Deep-Idle | Both closed, powering down | Cycle complete, dropping back to the unpowered+unlocked rest state |

**Not yet decided:** whether Propped-Open should extend into Low Power.
There's a plausible efficiency argument — once open, an unpowered,
unlocked door might hold position without ongoing draw, the same way
Deep-Idle holds closed for free, which could make a continuously-matched
connection (e.g., two rooms in the same base with identical atmosphere)
cheaper to leave propped than to keep Deep-Idle/Waking cycling on. Not
designed yet — flagged as a future optimization, not a current
requirement.

**Critical:** the phase list is minimal — this tier isn't meant to
support normal transit anymore:
| Phase | Portal Config | Description |
|---|---|---|
| Emergency-Evacuating | Both closed, forced venting of chamber atmosphere | The State 3 action — close any open door, dump chamber atmosphere, regardless of what phase it interrupts |
| Final-Unlocked | Both closed, unpowered, unlocked | End state — Crowbar-operable, waiting for either manual intervention or Charge recovery |

### Occupancy as a cross-cutting dimension

Occupancy (Empty / Occupied) isn't a phase — it applies underneath
whichever Power Tier and Cycle Phase are active.

- **Button C's whole purpose is an Occupied-during-transition case** — a
  player in the chamber when Power Tier drops to Low Power needs their
  own wake trigger. Already covered.
- **RESOLVED — Occupied during Emergency-Evacuating.** A full suit-check
  was considered and rejected as too complex — no reliable way to read
  helmet-seal state was confirmed, and building one adds a whole new
  dependency for a rare edge case. **Decision: Button C does double duty
  as the override.** Its meaning depends on Power Tier: in Low Power, a
  press means "wake and cycle me through." In Critical, C being actively
  held/pressed means "don't evacuate — I'm in here." The script checks
  C's state immediately before executing Emergency-Evacuating; if held,
  skip the evacuation this loop and re-check next iteration; if not
  held, proceed. Same physical button, same location, interpreted
  differently by tier — no new hardware, no suit telemetry. **Traded-off
  honestly:** this puts the responsibility on the player to notice and
  hold C in time — it's not a guarantee, it's a simple manual interlock
  in exchange for not needing suit-state sensing. Worth deciding if
  that's an acceptable trade before finalizing.

### Button conflict resolution — RESOLVED

Conflicting E/I presses only matters in the one case where the chamber
is genuinely idle with both doors closed (Idle-Exterior/Idle-Interior in
Normal, Deep-Idle in Low Power) — if a cycle's already running, a press
from the opposite side isn't a real conflict, it's just a queued request
against a busy system.

**Resolution: first press wins, second press is ignored — not because
it's discarded, but because it's redundant.** Whichever button fires
first sets the cycle direction. Once that cycle completes, the chamber
naturally ends up matched to the *other* side by construction — an
exterior entry request finishes with the chamber sitting open toward the
interior, which is exactly the state the interior requester needed
anyway. No queue, no priority table, no second action required — the
natural end state of the first request already satisfies the second one.

### Hysteresis — added per the "good catch" above

Concrete bands, so Charge sitting right at a boundary doesn't cause
rapid state flapping (Light flickering, doors repeatedly
waking/sleeping):

| Transition | Enter threshold | Return threshold |
|---|---|---|
| Normal → Low Power | Charge ≤ 90% | — |
| Low Power → Normal | — | Charge > 93% (not 90% — gap prevents flapping) |
| Low Power → Critical | Charge ≤ 10% | — |
| Critical → Low Power | — | Charge > 13% (not 10% — same reasoning) |

Exact gap size (3 points used here) is a starting guess, not confirmed
against any in-game testing — adjust based on how much Charge actually
fluctuates tick-to-tick once this is running for real.

### Mid-cycle Power Tier drop — RESOLVED, by avoiding the specific cause

**Not a practical concern in normal configurations.** A standard Active
Vent draws a confirmed 100W. A full cycle's worth of that draw is well
within what a properly-sized dedicated battery can sustain — Charge
dropping enough to cross a whole Power Tier boundary *during* a single
cycle essentially doesn't happen with standard components.

**⚠️ Warning — avoid this specific combination:** a **Powered Vent or
Large Powered Vent paired with a Small Battery** in the dedicated Power
Controller. Powered Vents pump 2× the pressure per tick of a standard
Active Vent (Large Powered Vent: 4×), with confirmed "slightly higher
energy consumption" — and critically, **Powered Vents have no internal
pressure limiter**, so they'll keep drawing hard for as long as they're
told to run, unlike a standard Active Vent's more self-limiting
behavior. Pairing that draw profile with a Small Battery's limited
capacity is exactly the combination that could deplete Charge enough
mid-cycle to cross a Power Tier boundary while a door's open. Standard
Active Vent + any reasonably-sized battery: not a real risk. Powered/
Large Powered Vent + Small Battery: avoid, or size the battery up to
match.

**Net effect:** this removes the need for the complex mid-cycle-interrupt
logic that would otherwise be required — constrain the component
choices instead of adding runtime handling for a case those choices
create. If a build genuinely needs a Powered Vent's throughput, size the
dedicated Power Controller's battery accordingly rather than trying to
script around an undersized one.

### Summary table — every reachable (Power Tier × Occupancy) combination

| Power Tier | Empty | Occupied |
|---|---|---|
| Normal | Full range of Normal phases available | Same, no restriction currently designed |
| Low Power | Deep-Idle default, wakes via E or I | Deep-Idle default, wakes via E, I, **or C** (C only matters here) |
| Critical | Emergency-Evacuating → Final-Unlocked | Emergency-Evacuating gated by the Button-C override (see "RESOLVED" above) — held C skips evacuation this loop rather than proceeding identically to the Empty case |

**Reference:** IC10 language facts confirmed via Community Wiki "Integrated
Circuit (IC10)" and "IC10" pages, and XGamingServer's IC10 programming
guide. Circuitboard hardcoded-vs-programmable distinction and lock
persistence mechanic confirmed via Community Wiki "Circuitboard (Advanced
Airlock)" page (cross-referenced in main guide Project H3). Multi-chip
coordination via shared device state confirmed via Steam Community
discussion "logic transmitters with ic chip tutorials?" and the IC10
LogicType model described in the Community Wiki "IC10" page and
XGamingServer's guide. Stalled pressurization/depressurization phases
and the "Cancel Pressurize" button confirmed via Community Wiki "Guide
(Airlock) Atmosphere to Atmosphere" page. Active Vent (100W) and Powered
Vent/Large Powered Vent (2×/4× pressure throughput, "slightly higher"
consumption, no internal pressure limiter) power draw confirmed via
Community Wiki "Active Vent" and "Powered Vent" pages.
