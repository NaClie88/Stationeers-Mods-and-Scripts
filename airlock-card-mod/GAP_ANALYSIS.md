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
2. **The chamber-interior Button C override** — someone caught inside
   during a forced Critical-tier evacuation can hold Button C to skip
   it. This is the specific, motivating problem this whole project was
   built to solve (see `SOURCES.md`: the Steam Community "Unlock (not
   open) airlock door when the power is cut" discussion, which
   confirms vanilla has no native answer to this at all). Wholly new.
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

## Power architecture

Confirmed by project owner (in-game device placement question,
2026-08-05): the power-saving design maps cleanly onto three roles,
same shape as the IC10 build's Watcher/Gate split, just without a
second chip:

- **Console (running the patched card's logic) + the dedicated Power
  Controller feeding it** — must stay on a circuit that's *never*
  switched off, same requirement as Watcher never being power-gated in
  the IC10 build. If the Console itself lost power, nothing could
  decide when to turn the downstream circuit back on.
- **Buttons** — power-agnostic. Confirmed elsewhere in this project
  (`SOURCES.md`, Logic Switch entry) to function fully unpowered, only
  their indicator light needs power — so it doesn't matter whether
  they're wired to the always-on side or the switched side.
- **Everything else (doors, Vent)** — behind a downstream APC/Power
  Controller the card switches on/off, reproducing the IC10 build's
  zone-gate exactly. `SetDownstreamPower(bool)` on `FailsafeController`
  is this switch.

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

## Graceful degradation

Every optional input on `IAirlockHost` (`src/FailsafeController.cs`)
was checked against "what happens if the end user never wires this."
None of them should be able to crash or strand the airlock — worth
recording exactly what each one falls back to, since some of these
took a real fix to get right (see the `VanillaCycleRequested` entry
below).

- **No hardware Buttons (E/I/C) wired at all.** `ButtonEHeld`/`IHeld`/
  `CHeld` all default false. `ButtonCHeld` false means Critical tier
  never gets overridden — the forced evacuation always runs, which is
  the correct safe default (nobody's holding the override, so nothing
  should skip it). The Low-tier wake condition doesn't depend solely on
  these — see the next point for why that matters.
- **No way to detect a wake request at all would be a real bug, not a
  degradation case.** An earlier version of this design tied the
  Low-tier wake condition to the hardware buttons alone — if none were
  wired, Deep Idle would cut downstream power and *never* restore it,
  even if a player used the Console's own UI to request a cycle,
  because the doors/Vent would stay depowered and unable to act on
  that request. Fixed by adding `VanillaCycleRequested`, which reflects
  vanilla's own request signal regardless of source (Console click,
  hardware button, anything) — this one isn't optional the way the
  others are, since a Console is required for the card to exist at all,
  so Milestone 1.5 has to find vanilla's real trigger hook no matter
  what else does or doesn't get wired.
- **No external Gas Sensor pair (Propped-Open).** `PropAtmosphereMatched`
  defaults false — doors simply never enter Propped-Open, normal
  cycling proceeds exactly as it would otherwise. Already the same
  behavior as skipping `gas_sensor.ic10` in the IC10 build.
- **No Presence/Motion Sensor.** `PresenceDetected` defaults false — no
  auto-cycling, Console UI and/or hardware buttons work exactly as
  before, nothing else changes.
- **No dedicated Power Controller at all.** `DedicatedBatteryChargeRatio`
  should default to 100 (always Normal), not 0 — a host with nothing
  to monitor should behave like vanilla with no fail-safe layer, not
  like vanilla stuck falsely believing it's always in a power crisis.

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
