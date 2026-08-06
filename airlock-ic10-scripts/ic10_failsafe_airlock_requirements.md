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
   one chip writes some device's field to a value not otherwise
   meaningful, a second chip reads that same field to know what the
   first chip decided. Confirmed as a real pattern used by the
   community for exactly this kind of inter-circuit signaling — **but
   see the correction below: a Light specifically doesn't work for
   this**, since it has no free-form field to repurpose.
2. **Logic Transmitter, Active/Passive pair** — purpose-built for this,
   but not as originally described here. **Corrected (2026-08-05, from
   a locally-saved copy of the Community Wiki's "Logic Transmitter"
   page, since the live page blocked every fetch attempt):** there is
   no separate "Logic Receiver" device and no numbered channel setting.
   It's a single device, **Logic Transmitter**, used in either
   **Active** or **Passive** `Mode` — a "receiver" is just a second
   Logic Transmitter set to Passive. Each unit exposes exactly one
   value field, `Setting` (not eight numbered channels), and pairing is
   a physical, in-game action: tune the Passive unit's dial to the
   Active unit's name. Multiple signals from one Active unit need
   packing into that single `Setting` value (see
   `ic10_airlock_code_notes.md` for how this design does it), not
   multiple channels.

**RESOLVED (2026-08-04, superseding the "deliberate choice" originally
written here): option 1, a Light, was the original plan specifically
for its dual purpose — but it doesn't work.** In-game Logic panel
screenshots confirmed neither a standard Light nor the "battery
backup" Light variant exposes a `Setting` field or anything else
free-form; the only writable fields are `On` (and `Lock`, `Mode` on
some variants) — nothing that can carry an arbitrary Tier value. The
actual mechanism used is **option 2, a Logic Transmitter pair (Active +
Passive)**, with Tier and live button state packed into the single
`Setting` value the Active unit broadcasts (see
`ic10_airlock_code_notes.md` for the full Watcher/Cycle implementation).
The player-facing half of the original idea survives, just through
different hardware: an **LED** (`StructureDiode`), confirmed to expose
a `Color` field none of the Light variants have, mounted at the portal
and driven directly by the same chip that computes Tier — still a
single source of truth, still visible in-world, just no longer
double-duty as the inter-chip signal too. Green/Normal, yellow/Low,
red/Critical.

**Requirement (unchanged):** the LED's placement matters as much as its
color — it needs to be visible from the portal itself (not tucked in a
rack with other indicator lights), since its job is informing the
player standing there.

**Why this matters for the 128-line limit:** if the full state machine
(normal cycling + Charge monitoring + staged fail-safe response) doesn't
fit in one IC10's 128 lines, split it — e.g., one chip owns door/vent
cycling and reads the Tier channel for "are we in fail-safe mode," a
second chip owns Power Controller monitoring and broadcasts that
channel (plus drives the LED for the player, a separate write to a
separate device). Neither chip needs to know the other's internal
logic, only the shared channel they both touch.

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
your chamber's dimensions alongside the two Portals, the warning LED,
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

**RESOLVED (2026-08-04) — not a Transformer, a Power Controller.** An
earlier draft of this requirement called for a Transformer per Portal.
That's confirmed wrong: Community Wiki, verbatim, *"Data will not flow
through a transformer"* — it's a passive wattage-cap device with no
data port at all, incapable of receiving an IC10 write in the first
place. The fix, confirmed as the community-standard workaround since
vanilla has no dedicated breaker component: a **Power Controller
(APC)**, which is data-networked (already established elsewhere in this
doc via Watcher's own Charge monitoring) and is what people
actually use to gate a circuit on/off via logic.

This surfaced alongside a bigger restructuring, not just a device swap
— see `ic10_airlock_code_notes.md`'s "Watcher/Cycle split" section
for the full reasoning. Short version: rather than each Portal getting
its own independent switching device, both Portals, the Vent, and the
door/vent-controller chip itself now share **one** switchable zone,
gated by a single Power Controller that the always-on Watcher chip
controls. The always-on/gateable split also resolved a second question
this doc hadn't asked yet: whether the *controller chips themselves*
(25W each, continuously, regardless of what they're doing) were
undermining Deep Idle's own power savings by all staying powered all
the time. Now only the Watcher chip does.

**Still open:** the exact LogicType that gates a Power Controller's own
output — assumed `On`, matching every other powered device confirmed
in this project, but not independently verified. A 10-second in-game
Logic Reader check settles it.

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
4. The warning LED (see "Multiple IC10 chips" above) still fires the
   same Tier-broadcast-plus-visual-warning signal at the State 2
   threshold — this doesn't replace that, it's an additional saving
   layered on top

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
- **Requirement:** the portal's warning LED (see "Multiple IC10 chips" above) changes to yellow — Watcher's Tier broadcast on the Transmitter channel handles telling Cycle a fail-safe response may be coming, and the LED write is a separate but simultaneous action that visually warns a player standing at the airlock that something's degraded, before they're relying on it mid-cycle
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
here in one place so nothing gets missed before writing real code. Status
as of the 2026-08-04 research pass — see `SOURCES.md` for the specific
URLs behind each **Resolved** item; items marked **Still open** genuinely
need your own in-game check, not more research, since they're
build-specific or blocked behind a Cloudflare wall no fetch tool got past.

1. **Standard Door wattage — Resolved (partial).** Composite Door
   confirmed at **10W/tick**. Glass Door specifically still unconfirmed —
   check its tooltip if your build uses one; likely the same order of
   magnitude as Composite, not confirmed identical.
2. **Exact LogicType name for Power Controller charge — Resolved, and it
   changed the Watcher chip.** `Charge` (Joules, absolute) and `Maximum`
   (Joules, battery capacity) are the confirmed pair — seen both in a
   community wiki-derived LogicType reference and in a real working
   Power Controller IC10 script. **No dedicated `Ratio`/`ChargeRatio`
   field was found on the Power Controller itself** (a plain `Ratio`
   LogicType does exist, but confirmed sources only show it read
   directly off a standalone Battery device, not through a Power
   Controller). The real script computes percentage manually:
   `div r0 charge max`. Watcher's `l r1 Battery Ratio` (an early draft's
   assumption — see `ic10_airlock_code_notes.md`) was changed to reading
   `Charge` and `Maximum` separately and dividing, not just a doc note.
   Still worth one in-game double check with a Logic Reader on your
   specific Power Controller in case your version does expose `Ratio`
   directly — but
   design against Charge/Maximum as the safer default.
3. **Absolute vs relative threshold — Resolved by item 2.** Since no
   confirmed direct ratio field exists on the Power Controller, the
   script computes a relative ratio itself (`Charge / Maximum × 100`)
   rather than hardcoding an absolute joule threshold tied to one
   specific battery. This makes the 90%/10% thresholds portable across
   any battery size without editing the script.
4. **Cycle latency from Deep Idle Mode — Target set, measurement still
   pending.** Design target: **under 0.25ms** from wake trigger (Button
   E/I/C press) to the Cycle zone (Power Controller gate + Portal) being
   responsive. This is a requirement to build and test against, not yet
   a measured result — still needs an in-game stopwatch check against
   your specific Power Controller + Portal combination before Deep Idle
   Mode is locked in as State 2's default.
5. **Exact LogicType names for Lock/Open and vent/pipe evacuation
   controls — Resolved.** `Lock` confirmed real: a plain bit, 0 =
   unlocked, 1 = locked, set the same way as any other LogicType write.
   `Open` confirmed for door state. For vent control, a real airlock
   script example confirms `On` (power the vent) and `Mode` (0 = outward
   /depressurize, 1 = inward/pressurize) as the pair driving
   evacuation/pressurization — adopted directly in the Cycle chip's
   evacuate/pressurize sequences rather than guessing new names.
6. **Hysteresis gap size — Resolved (starting value confirmed).** 3% of
   capacity is confirmed as a reasonable starting gap — matches the
   existing 90%/93% and 10%/13% bands already in the Watcher chip
   exactly, so no code change needed here. Still worth watching real
   Charge jitter under load once built and tightening/loosening if it
   flaps or lags.
7. **Chamber footprint — Resolved (planning figure).** Budget **1–2
   grid volumes** for the chamber itself, plus **at least 1 more grid of
   spillover** for the hardware that doesn't fit inside the chamber
   proper — pressure tanks for cycle air specifically. Note this sits
   alongside the earlier requirement that the Power Controller itself
   must be *inside* the chamber (for battery-swap access) — plan the
   1–2 grid interior with that constraint in mind, not just doors +
   buttons + light.
8. **Gas Sensor LogicType names — Resolved, and it surfaces a Gas
   Sensor chip bug.** Confirmed real fields: `Pressure`, `Temperature`,
   and per-gas ratios `RatioOxygen`, `RatioCarbonDioxide`,
   `RatioNitrogen`, `RatioPollutant`, `RatioMethane`,
   `RatioNitrousOxide`, `RatioHydrogen`, `RatioWater`,
   `RatioPollutedWater`, `RatioHydrazine`, `RatioLiquidAlcohol`,
   `RatioHelium`, `RatioSilanol`, `RatioHydrochloricAcid`, `RatioOzone`,
   `RatioLiquidOzone` — **there is no single generic `Ratio` field for
   "gas composition"** the way an early draft of the Gas Sensor chip
   read it (`l r4 SensExt Ratio`). That line was wrong as written —
   composition matching needs separate reads per relevant gas (Oxygen,
   Pollutant, Methane, and NOx per the tolerance list in the prototype
   doc), each compared against its own tolerance — since fixed there.
9. **Actual Powered Vent / Powered Vent Large wattage — Fully resolved,
   direct from the Community Wiki's own infobox** (the project owner
   saved a local copy of the page, bypassing the Cloudflare block that
   stopped every earlier attempt at this specific page). **Naming
   correction: it's "Powered Vent" and "Powered Vent Large" — not
   "Large Powered Vent."** Every earlier mention in this project had the
   word order backwards. Confirmed figures: **Active Vent 100W**,
   **Powered Vent 250W** (2× the pressure/tick of an Active Vent),
   **Powered Vent Large 500W** (4×) — the 250W figure was never
   previously confirmed anywhere and settles the "don't assume it's a
   linear half of 500W" caution below in this project's earlier draft;
   it isn't linear (250 vs 500, not 250 vs 500/2). Also newly confirmed
   from the same page: Powered Vents "scavenge" air from nearby grids
   once pressure drops below ~20kPa (Manhattan distance 4 grids for the
   small one, 6 for Large), and the in-game description explicitly
   frames Powered Vent as being for "multi-grid airlocks" and Powered
   Vent Large for "large scale airlock systems and pressurized
   hangars" — confirming, per the project owner's own game knowledge,
   that **neither is the right tool for a single self-contained chamber
   airlock like this one**; a standard Active Vent remains the
   recommended default.
10. **IC10 line-length limit — Fully resolved, both figures were
    correct at once.** The two numbers weren't actually in conflict —
    they measure different things. **52 characters is the in-game
    editor's typing limit** — a UI constraint on what you can enter by
    hand. **90 characters is the real, underlying execution/storage
    limit** — you can paste a longer line (up to 90 chars) directly into
    the editor and the game accepts and runs it correctly, even though
    typing that far by hand is blocked. This is exactly why community
    sources split down the middle: GitHub repos optimized for
    manually-typed code cite 52 for compatibility, while wiki/technical
    docs cite 90 as the true limit. **Practical takeaway:** the
    prototype code's conservative short lines remain a reasonable
    default (easiest to type/edit directly in-game), but there's no
    correctness reason to keep lines under 52 chars if you're
    copy-pasting rather than hand-typing — 90 is the real ceiling.

## Current Door Logic — Walkthrough

Putting the pieces above together, here's how the airlock actually
behaves end to end, state by state.

**Hardware in place:** two IC10s (Watcher and Cycle, each in its own IC
Housing, see `ic10_airlock_code_notes.md` for the split) — no airlock
circuitboard in the loop. Watcher stays powered continuously; Cycle is
powered only
when Watcher's Power Controller-based zone gate is on, and owns both
Portals and the Active Vent directly. A dedicated, isolated Power
Controller (its own battery, off the main grid, physically inside the
chamber for battery-swap access) feeds Watcher and, via the zone gate,
Cycle and the doors. One warning LED mounted at the portal, wired to
Watcher only — not a plain Light, since neither Light variant exposes
a field for the color-per-Tier signal this needs (confirmed in-game;
see `ic10_airlock_code_notes.md`). Three Buttons — E (exterior),
I (interior/base side), C (inside the chamber itself) — all read by
Watcher and relayed to Cycle over a pair of Logic Transmitters (one
Active, one Passive — see the correction above; not a separate
"Receiver" device), not wired directly to Cycle at all. A dedicated
Gas Sensor inside the chamber itself, read by Cycle,
for unambiguous pressure sensing during a cycle. Two more Gas Sensors —
one exterior-facing, one interior-facing — feeding the optional Gas
Sensor chip continuously, used for the Propped-Open exception described
below.

**Every loop iteration, regardless of state:** the script reads the
dedicated Power Controller's `Charge` first. That single value decides
which of the three states below governs everything else this loop.

**State 1 — Normal (Charge > 90%):** the airlock behaves like a
standard automated airlock. Doors stay powered and lock during an active
cycle, per the normal safety behavior for pressure changes. Any of the
three Buttons can request a cycle; the script sequences vent
evacuation/pressurization and door open/close in the usual order. The
LED stays green. The two Gas Sensors run
continuously in the background here too — if they ever confirm a
genuine match on both sides, the airlock can prop both doors open
instead of cycling for no reason, reverting the instant either sensor
detects the match has broken.

**Transition into State 2 (Charge drops to ≤90%):** the LED switches to
yellow — Watcher's Tier broadcast (packed into the Logic Transmitter's
`Setting` value, see the correction above) tells Cycle this portal's
power is uncertain, and the LED write in the same loop iteration
visibly warns any player standing at the portal. From this point on,
doors no longer
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
Powered Vent Large paired with a Small Battery** in the dedicated Power
Controller. (Naming note: the in-game names are "Powered Vent" and
"Powered Vent Large" — an earlier draft of this doc had the word order
backwards as "Large Powered Vent" throughout.) Powered Vent pumps 2× the
pressure per tick of a standard Active Vent (Powered Vent Large: 4×) —
and the draw isn't just "slightly higher" as earlier phrased, it's
confirmed directly from the Community Wiki's own infobox: **Powered
Vent = 250W, Powered Vent Large = 500W, vs. Active Vent's 100W** —
2.5× and 5× respectively, not the vague "slightly higher" this doc
used to say. Critically, **Powered Vents have no internal pressure
limiter**, so they'll keep drawing hard for as long as they're told to
run, unlike a standard Active Vent's more self-limiting behavior — the
same wiki page adds that a Powered Vent left running too long can
overpressure and burst its own input piping without careful monitoring.
Pairing either Powered Vent tier's continuous draw with a Small
Battery's limited capacity is exactly the combination that could
deplete Charge enough mid-cycle to cross a Power Tier boundary while a
door's open — this is now a concrete, sized risk, not a vague one.
Standard Active Vent + any reasonably-sized battery: not a real risk.
Powered Vent/Powered Vent Large + Small Battery: avoid, or size the
battery up to comfortably cover 250W/500W for a full cycle's duration.

**Also newly confirmed: neither Powered Vent tier is actually the right
tool for this build in the first place.** The Community Wiki quotes the
in-game Stationpedia description directly — Powered Vent is "for the
creation of multi-grid airlocks," Powered Vent Large is for "large
scale airlock systems and pressurized hangars." This project's airlock
is a single self-contained chamber, not a multi-grid system or a
hangar — a standard Active Vent remains the right default regardless of
the battery-sizing question above, which mostly matters if you're
reusing this design's Power-Tier logic for a bigger multi-grid build.

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
(Airlock) Atmosphere to Atmosphere" page. Active Vent (100W), Powered
Vent (250W, 2× pressure throughput), and Powered Vent Large (500W, 4×)
all confirmed directly from the Community Wiki's "Active Vent" and
"Powered Vent" pages' own infoboxes — the project owner saved a local
copy of the "Powered Vent" page (2026-08-04) that resolved both the
250W figure and the "Large Powered Vent" → "Powered Vent Large" naming
correction, after the live page repeatedly blocked automated fetches.
