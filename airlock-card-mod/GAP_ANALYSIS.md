# Gap Analysis — Vanilla Advanced Airlock Circuitboard vs. This Design

Point of this doc: confirm what vanilla already does well enough that
we don't reimplement it, and isolate exactly what's actually new. This
is what makes "patch the existing card" viable instead of building
from scratch.

Vanilla facts below are sourced from the Community Wiki (search
snippets — the live pages block automated fetches for this project,
same as everywhere else in this repo; verify directly once you're at
your PC and can check Stationpedia/the actual console UI). See
"Sources" at the bottom.

## What vanilla's Advanced Airlock Circuitboard already does

- Automates cycling between two environments ("interior"/"exterior"),
  same concept as this project's `Cycle` chip.
- Pressurize/Evacuate against a configured target pressure
  ("Internal #Pa"/"External #Pa" settings) — directly equivalent to
  this project's `TargetInt`/`TargetExt` constants and the
  `pressurize`/`evacuate` branches in `cycle.ic10`.
- **Stall handling, and a Cancel button** — if a phase can't reach
  target, it stalls, and a "Cancel Pressurize" button on the console
  lets a player skip it. **This is something our own docs flagged as a
  known gap** (`ic10_airlock_setup_guide.md` section 9: "No
  stall-timeout... the game's own 'Cancel Pressurize' button aren't
  handled in script") — vanilla already has this covered. One less
  thing to build.
- **Lock persists through power loss** — the mechanical property this
  whole project's fail-safe design exists to respond to in the first
  place (see `ic10_failsafe_airlock_requirements.md`).
- Console Slaves — multiple consoles can sync to one "master," for
  multi-entrance setups. This project's IC10 design has no equivalent
  (single Console assumed) — not a gap to close, just a vanilla
  capability to make sure a patch doesn't accidentally break.

## What this design adds that vanilla doesn't have

This is the actual scope of new work — everything else above, we
inherit for free by patching instead of replacing.

1. **Tier-based power monitoring with hysteresis** (Normal/Low/Critical,
   driven by a dedicated Power Controller's `Charge`/`Maximum`).
   Nothing found in vanilla's documented behavior resembles this — the
   "lock persists through power loss" property is passive (a
   mechanical fact about the door), not an active monitored response
   with staged behavior. This is `watcher.ic10`'s entire job, and it's
   wholly new.
2. **The chamber-interior override** — someone caught inside during a
   forced Critical-tier evacuation needs a way to skip it. This is the
   specific, motivating problem this whole project was built to solve
   (see `SOURCES.md`: the Steam Community "Unlock (not open) airlock
   door when the power is cut" discussion, confirming vanilla has no
   *automatic fail-safe* to override in the first place). **Downgraded
   from "wholly new" (2026-08-05):** the vanilla Console UI already has
   a Skip/"Cancel Pressurize" button (item above) for cancelling a
   stalled phase — see "Reusing vanilla's Skip instead of custom Button
   C hardware" below for why this may make a dedicated physical button
   unnecessary, not just a design nicety.
3. **Propped-Open state** — both doors held open when a Gas Sensor
   pair confirms matched atmosphere across the airlock, avoiding
   needless cycling. Not found in vanilla's documented behavior.
   Wholly new, and already optional/isolated in this project's own
   design (a separate `gas_sensor.ic10` chip) — cleanly separable as
   an add-on here too.
4. **The warning LED as a Tier indicator** — vanilla's console
   presumably has its own status UI, but not a player-facing physical
   light cycling green/yellow/red with Tier. Minor, but new.
5. **Downstream power gating ("Deep Idle")** — cutting power to the
   doors and Vent between uses to save the dedicated battery's charge,
   waking on a button press or forcing back on at Critical tier with
   zero button press. Nothing in vanilla's documented behavior
   resembles this either — vanilla presumably just keeps its own
   circuit powered continuously. Wholly new, and it's the one item on
   this list where the *architecture* matters as much as the logic —
   see "Power architecture" below.
6. **Auto-cycling via an optional Presence/Motion Sensor** — open on
   approach instead of requiring a button press. Not in vanilla, and
   deliberately not part of the IC10 build's core wake path either
   (see "Graceful degradation" below for why) — a genuinely new
   optional extra, not a port of anything.
7. **Opt-in power-down while propped open (`AllowPowerDownWhilePropped`)**
   — not in the IC10 build at all, and not vanilla either: lets Deep
   Idle engage even during a genuine atmosphere match, provided the
   three Gas Sensors keep monitoring from the always-on circuit and a
   mismatch forces an immediate wake. Off by default. Genuinely new,
   see "Graceful degradation" below.
8. **Configurable Tier thresholds and `WakeHoldTicks`** (project owner,
   2026-08-05) — the hysteresis boundaries (90/93/10/13%) and the
   Deep-Idle hold duration were fixed constants in the IC10 build (and
   still are, as defaults, here) but are now settable per-build via
   `FailsafeController`'s public properties rather than requiring a
   recompile to retune. No new device or wiring — a Console-settings
   question for Milestone 2, see `PATCH_PLAN.md`.
9. **Temperature safety check gating the Critical-tier unlock**
   (project owner, 2026-08-05) — a real gap this project hadn't
   caught: matching pressure alone (the existing chamber Gas Sensor
   check) doesn't protect against unlocking into an extreme-temperature
   environment. `SafeToUnlockTemperature` gates `UnlockDoors()`
   specifically — evacuating (relieving chamber pressure) stays
   unconditional, since that's safe regardless of temperature. See
   "Once these are known" in `PATCH_PLAN.md` for the `ForceEvacuate()`/
   `UnlockDoors()` split this required.
10. **Maintenance mode** (project owner, 2026-08-05) — a Console
    toggle to fully suspend this fail-safe layer without removing any
    hardware, for construction/expansion work where you want to hold a
    door open indefinitely without the fail-safe layer fighting that.
    Vanilla's own cycling keeps running underneath, completely
    unaffected either way.

## Reusing vanilla's Skip instead of custom Button C hardware

**Corrected (2026-08-05) — the Console is inside the chamber by
default, this isn't a workaround.** An earlier version of this section
assumed vanilla's Console sits *outside* the chamber (matching the
original IC10 layout, which only put Button C inside specifically to
give a trapped player any access at all) and proposed slaving a second
Console inside to compensate. That assumption was wrong. Project owner
confirms the traditional/conventional physical layout for this whole
build is:

- **Inside the chamber (the traditional core set):** Console, 2×
  Active Vent, (chamber) Gas Sensor, 2× Doors, the airlock chip/circuit,
  and one Area Power Controller.
- **Optional extras, not part of the traditional set:** the warning
  LED, a Presence Sensor, and the Logic Buttons (E/I/C) — all
  additive, enabling functionality the traditional build doesn't have
  on its own.

Since the Console is already inside by default, anyone in the chamber
during a Critical event already has direct access to its UI — no
Console Slave, no extra hardware, nothing to build. This makes the
Skip-button plan simpler than originally written, not more complex:

1. **`ForceEvacuate()` should call *into* vanilla's own
   evacuate-toward-target logic**, not reimplement vent sequencing from
   scratch (already the plan in `PATCH_PLAN.md`) — if that method
   already carries its own Skip/Cancel affordance, the override comes
   along for free.
2. **Reachability is already solved by the traditional layout.** No
   second condition to satisfy here anymore.

**Fully testable in-game right now, zero code required** — doesn't
wait on Milestone 1.5's decompiler pass:

1. Build a vanilla `Circuitboard (Advanced Airlock)` setup (no patch,
   no mod) with the traditional layout above.
2. Deliberately stall a Pressurize or Evacuate phase (e.g. no gas
   available to reach target).
3. From inside the chamber, at the (already-present) Console, click
   Skip and confirm it actually cancels the phase.

If that holds up, custom Button C hardware from the original IC10
design is likely unnecessary for this build entirely — not just
downgraded to a fallback. `ButtonCHeld` can stay on `IAirlockHost` for
anyone who wants a physical button anyway, but the setup guide for this
build wouldn't need to recommend building one.

## Power architecture

**Two Power Controllers, not one — this build adds a second one beyond
the traditional layout.** The traditional set (see "Reusing vanilla's
Skip" above) has exactly *one* Area Power Controller, feeding
everything inside the chamber — Console, both Vents, the chamber Gas
Sensor, the chip, implicitly the doors too. That's sufficient for
vanilla, which has no Tier monitoring to keep alive through a power
loss in the first place. This design's whole fail-safe premise
requires something vanilla doesn't need: the Console (running the
patched logic) has to keep running *through* a loss of that main
circuit, in order to detect the loss and respond to it. That's only
possible if the Console is fed from somewhere else.

So, confirmed by project owner (2026-08-05), the design needs two
separate Power Controllers, mapping onto three roles — same shape as
the IC10 build's Watcher/Gate split, just without a second chip:

- **A second, dedicated Power Controller — new, not part of the
  traditional set — feeding only the Console (running the patched
  logic).** Must stay on a circuit that's *never* switched off, same
  requirement as Watcher never being power-gated in the IC10 build. If
  the Console itself lost power, nothing could decide when to turn the
  downstream circuit back on. This is the same "isolated, physically
  swappable dedicated battery" concept the original IC10 design already
  required for the identical reason — not a new pattern, just applied
  again here.
- **Buttons** — power-agnostic. Confirmed elsewhere in this project
  (`SOURCES.md`, Logic Switch entry) to function fully unpowered, only
  their indicator light needs power — so it doesn't matter whether
  they're wired to the always-on side or the switched side.
- **Everything else (doors, Vents, chamber Gas Sensor) — stays on the
  traditional Area Power Controller**, which the card now switches
  on/off, reproducing the IC10 build's zone-gate exactly.
  `SetDownstreamPower(bool)` on `FailsafeController` is this switch.
  Effectively, the traditional single-APC layout becomes the switched
  "downstream" circuit, and the new second Power Controller becomes the
  always-on one.

**Wiring detail that affects Milestone 1.5, project owner (2026-08-05):
an APC only exposes its logic to its power-SOURCE side, not its
downstream/output side.** The network downstream of an APC is isolated
(matches the earlier research finding: "used to segregate power into
their own subnetworks") and doesn't carry the APC's own control
interface. Concretely: the card's data connection for reading/writing
the traditional Area Power Controller's `On` field has to come from the
source side — which, given the architecture above, is naturally where
the card and the new dedicated Power Controller already sit. This
isn't a new wiring requirement so much as a confirmation that the
already-planned architecture is wired the only way that could work —
worth stating explicitly so Milestone 1.5 doesn't waste time looking
for a control hook on the downstream side.

**This also means the card should detect whether an APC is present at
all, not assume one** (project owner, 2026-08-05) — see
`HasDownstreamController` on `IAirlockHost` in
`src/FailsafeController.cs`, and "Graceful degradation" below for what
happens if none is found.

**Naming question, working assumption as of 2026-08-05:** a search
turned up the Community Wiki's "Area Power Controller" page redirecting
to its "Power Controller" page — suggestive that "APC" and "Power
Controller" are the same in-game device under two names. Project owner
confirms this matches their own understanding, so treating it as likely
true from here on — still worth a Stationpedia glance during Milestone
1.5 to fully close it out, but not blocking. If it holds, this
architecture isn't new territory at all: it's the same device (and the
same already-flagged-unconfirmed `On` LogicType) as the zone gate in
`ic10-airlock/watcher.ic10`, just wired the same way again.

## Cross-network visibility for the downstream side

Project owner's concern (2026-08-05): Deep Idle logic still needs some
way to "see" the downstream network — either a Logic Transmitter pair
(vanilla) or, if Re-Volt is installed, its Data Diode (see
`revolt-mod` branch's `ic10-airlock/mods/revolt/PARTS_DELTA.md` for
that investigation). Worth being precise about *why* this was true for
the original IC10 design before assuming it's true here in the same
way, because the two situations aren't identical.

**Why `watcher.ic10`/`cycle.ic10` needed a bridge:** not really about
crossing a power boundary — two separate IC10 chips can't share
register values under any circumstances, full stop. They needed *some*
shared device with a readable/writable field to relay through no
matter how the networks were wired, which is why a Transmitter pair
(or, hypothetically, the Re-Volt Data Diode) was necessary there.

**Why this mod-card situation is genuinely different, and not yet
resolved either way:** the patched logic isn't a second IC10 chip —
it's a Harmony patch attached to the *same* vanilla circuit instance
that (presumably) already holds whatever internal references it uses
to reach its own Doors, Vents, and chamber Gas Sensor. If those are
plain C# object references rather than in-game logic-network reads
(likely, for compiled game code — a MonoBehaviour-style class typically
holds direct references to its wired structures, not device-network
lookups), the patch may already have access to them for free through
that shared instance, no bridge required for that part at all.

**What definitely still needs resolving either way:**

- Detecting and switching the downstream APC (`HasDownstreamController`
  / `SetDownstreamPower`) — addressed above ("Power architecture"),
  reachable from the source side without a bridge, assuming the Card
  and the APC's source-side connection point end up on the same
  network as the new dedicated Power Controller.
- Anything downstream the vanilla instance *doesn't* already hold a
  reference to and that our new logic needs directly — not yet
  identified as a concrete case, but can't be ruled out until
  Milestone 1.5's decompiler pass shows what the vanilla class
  actually looks like internally.

**Added to the Milestone 1.5 checklist** (`PATCH_PLAN.md`): does the
patched card have C#-level access to the vanilla instance's own
door/Vent/chamber-sensor references (via the shared instance, or via
reflection into private fields), or does it need to go through the
in-game logic network to reach them? That answer decides whether any
bridge is needed at all, not just which one.

**If a bridge does turn out to be needed**, both fallbacks are already
designed and documented:

- **Vanilla:** a Logic Transmitter (Active) + Logic Transmitter
  (Passive) pair, same pattern as `watcher.ic10`/`cycle.ic10` — proven,
  but costs the two devices and the manual dial-pairing step.
- **Re-Volt installed:** its Data Diode — see `revolt-mod`'s
  `PARTS_DELTA.md` for the full case (network-bridging per its own
  commit description, no manual pairing, fewer parts). Since this
  branch is vanilla-first and doesn't assume Re-Volt (see `README.md`),
  this would be the same kind of optional overlay the `ic10-airlock/`
  build already has — a `mods/revolt/` variant on top of the vanilla
  baseline, not a replacement for it.

## Graceful degradation

Every optional input on `IAirlockHost` (`src/FailsafeController.cs`)
was checked against "what happens if the end user never wires this."
None of them should be able to crash or strand the airlock — worth
recording exactly what each one falls back to, since one of these went
through two design passes to get right (see "No hardware Buttons"
below — the corrected version, not the version this project shipped
first).

- **No hardware Buttons (E/I) wired at all → Deep Idle doesn't run,
  full stop.** `HasWakeButtons` gates the entire Low-tier power-cutting
  behavior. This wasn't the first design: the first pass tried to keep
  Deep Idle running for everyone by adding `VanillaCycleRequested` (a
  Console-click signal) as a fallback wake source. That has a real
  hole — whether vanilla's own click handling *survives* the delay
  between "click arrives while downstream power is off" and "downstream
  power comes back on a tick later" is unconfirmed and can't be
  confirmed without decompiling the game. A one-shot click that gets
  silently dropped during that gap would be worse than no power saving
  at all. The corrected design doesn't take that risk: without buttons,
  Low tier just holds downstream power on continuously (identical to
  Normal), and the unconfirmed click-survives-a-delay question never
  comes up because nothing ever gets powered off in the first place.
  `VanillaCycleRequested` is kept, but demoted to a secondary wake
  source that only matters once `HasWakeButtons` is already true — see
  its doc comment on `IAirlockHost`.
- **`ButtonCHeld` specifically, if Button C isn't wired.** Defaults
  false — Critical tier never gets overridden, the forced evacuation
  always runs, which is the correct safe default (nobody's holding the
  override, so nothing should skip it). Independent of `HasWakeButtons`
  above, which only concerns E/I.
- **No external Gas Sensor pair (Propped-Open).** `PropAtmosphereMatched`
  defaults false — doors simply never enter Propped-Open, normal
  cycling proceeds exactly as it would otherwise. Already the same
  behavior as skipping `gas_sensor.ic10` in the IC10 build.
- **No Presence/Motion Sensor.** `PresenceDetected` defaults false — no
  auto-cycling, Console UI and/or hardware buttons work exactly as
  before, nothing else changes.
- **`AllowPowerDownWhilePropped` left off (the default).** No behavior
  change at all from everything else already described — Propped-Open
  keeps forcing power on for as long as the match holds, same as
  before this setting existed. Turning it on is an explicit,
  deliberate choice with its own placement requirement (see
  `STATE_TABLE.md`'s "New" callout on the Low tier) — never something
  a host silently opts into.
- **No dedicated Power Controller at all.** `DedicatedBatteryChargeRatio`
  should default to 100 (always Normal), not 0 — a host with nothing
  to monitor should behave like vanilla with no fail-safe layer, not
  like vanilla stuck falsely believing it's always in a power crisis.
- **No APC/Power Controller found on the downstream side at all
  (2026-08-05, project owner) → Deep Idle doesn't run, same as no
  buttons.** `HasDownstreamController` gates Low tier exactly the way
  `HasWakeButtons` does — both have to be true for Deep Idle to
  actually engage, either one missing holds downstream power on
  continuously instead. Different reason than the buttons case (there's
  nothing to switch, rather than nothing safe to wake from), same
  fallback. This also protects `SetDownstreamPower` from being called
  against a device that isn't there — see "Power architecture" above
  for why an APC can only be found/controlled from its source side, not
  by scanning downstream from the doors.
- **No temperature check wired.** `SafeToUnlockTemperature` defaults
  `true` — a host that doesn't implement a temperature check just gets
  the original unconditional-unlock behavior back, same as before this
  capability existed. This is the one degradation case worth pausing
  on: it means a build with no temperature sensor genuinely has no
  protection here, not a false sense of one. Worth actually wiring for
  any world with a real temperature hazard, not just leaving as a
  hypothetical.
- **Maintenance mode left off (the default).** No behavior change —
  identical to every state already described above.

## Presence sensor placement (auto-cycling)

Confirmed usable, but not the default, and not part of the
safety-critical wake path — same reasoning this project already
reached once, for the IC10 build's own "Optional afterthought: APC
motion-sensor automation" section
(`ic10-airlock/ic10_airlock_setup_guide.md`): Buttons are confirmed to
cost nothing to monitor even fully unpowered (`SOURCES.md`), but a
Motion/Presence Sensor's own idle power draw was never confirmed the
same way. That's still true here. A presence sensor **must** sit on
the always-on side (same circuit as the Console), not the switched
downstream circuit — if it's fed from behind the APC, it can never
detect anyone approaching while that circuit is depowered, defeating
its own purpose. The tradeoff for the convenience is its own
(unconfirmed-magnitude) continuous draw on the always-on circuit,
which is exactly why this project didn't make it part of the core
design and keeps it strictly optional here too.

## What this means for the patch

Everything in "what vanilla already does" stays untouched — the patch
should **add** behavior around the existing cycling logic, not replace
it. Concretely: extra checks that run *before* or *around* the
existing pressurize/evacuate/lock logic (a Tier check that can force a
lock/evacuate sequence in Critical, an override read for Button C, an
optional Propped-Open bypass), not a rewritten cycle state machine.
The exact attachment points (which method to `Prefix`/`Postfix`) are
still Milestone 1.5's job — this doc fixes *what* needs to attach,
`src/FailsafeController.cs` in this same folder is the *how*
(game-independent so it's ready the moment real hooks are known).

## Sources

- Community Wiki, "Circuitboard (Advanced Airlock)" — cycling,
  interior/exterior settings, Console Slaves, stall behavior (search
  snippet).
- Community Wiki, "Circuitboard (Airlock)" — basic-vs-advanced
  distinction (search snippet).
- Community Wiki, "Guide (Airlock) Atmosphere to Atmosphere" — Stalled
  phase and the "Cancel Pressurize" button, confirmed by name (search
  snippet). Already cited in this project's `SOURCES.md` for the IC10
  side, re-cited here for the mod-card side.
- `ic10-airlock/ic10_failsafe_airlock_requirements.md`,
  `ic10_airlock_setup_guide.md`, `ic10_airlock_code_notes.md` — this
  project's own design and its already-documented known gaps.
