# IC10 Fail-Safe Airlock — First-Time Setup Guide

A build-order checklist for turning `watcher.ic10`, `cycle.ic10`, and
`gas_sensor.ic10` into a working airlock. Read
`ic10_failsafe_airlock_requirements.md` first if you haven't — this doc
assumes you already know *why* the design looks like this and just need
to know what to physically do. For the design rationale behind the
scripts themselves, see `ic10_airlock_code_notes.md`.

Everything here reflects the 2026-08-04 Watcher/Cycle architecture —
all three chips complete, dry-run verified in an emulator — not yet
verified in-game, which is what this guide is for. If you built an
earlier version of this airlock using Transformers, see "If you already
built the earlier Transformer version" near the end before rewiring.

## 1. Hardware shopping list

Item and quantity are on the bold line; the line under each one is
notes only, not part of the item name.

- **IC Housing** — qty **2** (+1 optional)
  Watcher and Cycle required; Gas Sensor chip optional.

- **IC10 chip** — qty **2** (+1 optional)
  One per housing above.

- **Portal (Airlock door)** — qty **2**
  Exterior + Interior. **Not** a Circuitboard (Airlock) or Circuitboard
  (Advanced Airlock) — this design replaces that hardcoded circuit
  entirely.

- **Power Controller — zone gate** — qty **1**
  Gates power to the entire Cycle zone (both Portals + Vent + the Cycle
  chip's own housing) as a single switchable unit. **Replaces the
  Transformer** called for in earlier drafts — a Transformer has no
  data port and can't be logic-controlled at all, confirmed via the
  Community Wiki.

- **Active Vent** — qty **1**
  Chamber evacuate/pressurize. **Do not substitute** a Powered Vent or
  Powered Vent Large for the standard Active Vent — both are meant for
  multi-grid airlocks and hangars respectively, not a single
  self-contained chamber like this one, and draw significantly more
  (Powered Vent 250W, Powered Vent Large 500W, vs. Active Vent's 100W —
  confirmed from the Community Wiki's own infobox). If you do use one
  anyway, size your dedicated battery well above Small — see the
  requirements doc's power-draw warning for why.

- **Power Controller — dedicated battery** — qty **1**
  **Must be physically inside the chamber**, not the base interior or
  outside — this is the actual backstop that lets a trapped player
  self-rescue by swapping the battery. Feeds Watcher continuously and,
  via the zone gate above, the Cycle chip and doors.

- **LED** (`StructureDiode`, **not** a plain Light) — qty **1**
  Player-facing warning signal, three colors (green/yellow/red per
  Tier) — wired to Watcher only, not Cycle; inter-chip Tier signaling
  goes over the Transmitter/Receiver instead. **Specifically an LED,
  not a Light** — confirmed in-game that neither Light variant has a
  `Color` field, only the LED does. Mount visibly at the portal, not
  tucked in a rack.

- **Logic Switch ("Button")** — qty **3**
  E (exterior), I (interior/base side), C (inside the chamber itself) —
  all read by Watcher only, none wired to Cycle.

- **Logic Transmitter** — qty **2**
  Yes, two — **there's no separate "Logic Receiver" device.** One goes
  on Watcher set to **Active** mode, one on Cycle set to **Passive**
  mode; the Passive one is manually tuned to the Active one's name via
  a dial on the device itself, in-game (see step 6 below) — not
  something the script sets. Relays Tier + live button state to Cycle
  across the two independently-powered circuits.

- **Gas Sensor — chamber** — qty **1**
  **New in this revision.** Mounted inside the chamber itself, read by
  Cycle for unambiguous pressure during a cycle. Replaces an earlier
  design shortcut that read pressure off the Vent's own field instead.

- **Gas Sensor — exterior/interior-facing** — qty **2** (optional, for
  the Gas Sensor chip)
  One exterior-facing, one interior-facing — only needed if you want
  the Propped-Open feature.

## 2. Placement and footprint

- **Dedicated Power Controller: inside the chamber.** Non-negotiable per
  the requirements doc — this is what lets a player caught in a fully
  dead airlock crowbar it open and swap the battery themselves.
- **Zone-gate Power Controller: wherever's convenient**, typically near
  the Cycle IC Housing — it's not the same device as the dedicated one
  above, and doesn't need to be inside the chamber itself.
- **Chamber footprint: budget 1–2 grid volumes** for the chamber
  itself (Portals + dedicated Power Controller + LED + Button C +
  the new chamber Gas Sensor), **plus at least 1 more grid of
  spillover** for hardware that doesn't fit inside the chamber proper —
  specifically the Vent's pressure tanks for cycle air, the zone-gate
  Power Controller, and the two IC Housings.
- **LED: mounted at the portal, visible from outside it**, wired to
  Watcher only — it's a player-facing warning now, not an inter-chip
  signal (that moved to the Transmitter/Receiver).
- **Button C: inside the chamber**, not at either portal — it's the
  wake/override for someone already caught inside.
- **Button E / Button I: outside the chamber**, one per side, wherever
  makes sense for approach traffic.
- **Both Logic Transmitters: no placement constraint beyond being
  powered on their respective circuits** — Active unit on Watcher's
  always-on side, Passive unit on Cycle's gated side. **Manually tune
  the Passive unit's dial to the Active unit's name in-game** before
  relying on them (see step 6) — a one-time physical pairing step, not
  something the script sets.

## 3. Wiring — Watcher (always powered)

IC Housing pins, 4 of 6 used. The code alias is what's typed in the
`alias` line at the top of the script — match it to the device you
just wired to that pin.

- **`d0`** — code alias `Battery`
  Dedicated Power Controller (the one inside the chamber).

- **`d1`** — code alias `LED`
  The warning LED (`StructureDiode`) — not a plain Light, see hardware
  list above for why.

- **`d2`** — code alias `Gate`
  The zone-gate Power Controller.

- **`d3`** — code alias `Transmitter`
  Logic Transmitter, set to **Active** mode (the script also sets this
  via `s Transmitter Mode 1` at startup, but the physical unit needs to
  exist and be powered for that to take).

Nothing else connects to this housing — the three Buttons are read by
name over the network (see step 6), not pin-wired. Load `watcher.ic10`.
**This chip is never power-gated** — it feeds from the same always-on
circuit as the dedicated Power Controller itself.

## 4. Wiring — Cycle (powered only when the zone gate is on)

IC Housing pins, 5 of 6 used (`d0` intentionally spare — see below).
Same idea as the Watcher table above — code alias on the left, what to
physically wire to that pin on the right.

- **`d1`** — code alias `DoorExt`
  Exterior Portal.

- **`d2`** — code alias `DoorInt`
  Interior Portal.

- **`d3`** — code alias `Vent`
  Active Vent.

- **`d4`** — code alias `Receiver`
  A second Logic Transmitter, set to **Passive** mode (script sets this
  via `s Receiver Mode 0` at startup) and physically tuned to Watcher's
  Active unit by name — see step 6. There's no separate "Logic
  Receiver" device; `Receiver` is just this project's alias name for
  it.

- **`d5`** — code alias `ChamberSensor`
  The chamber Gas Sensor.

**`d0` is not wired to anything.** An earlier revision had this chip
reading Tier off the shared Light's `Setting` field — that field
doesn't exist on any Light variant (confirmed in-game), so Tier now
arrives over `d4`'s Receiver instead, and Cycle never needs to touch
the Light at all.

Load `cycle.ic10`. **This entire housing, along with both Portals and
the Vent, sits on the zone-gate Power Controller's switched output** —
when Watcher cuts the gate, this whole circuit (including the Cycle
chip itself) loses power together. That's deliberate: Cycle doesn't
need its own Deep Idle logic anymore, because
it simply doesn't run at all while idle.

## 5. Wiring — Gas Sensor chip (optional)

Only build this if you installed the two exterior/interior-facing Gas
Sensors (not the chamber one from step 4, which belongs to Cycle).

- **`d0`** — code alias `SensExt`
  Exterior-facing Gas Sensor.

- **`d1`** — code alias `SensInt`
  Interior-facing Gas Sensor.

Load `gas_sensor.ic10`. If you skip this chip entirely, Cycle still
works — it just never enters the Propped-Open state. This chip can live
on either circuit (always-on or gated) since Propped-Open only matters
during Normal tier anyway when the zone is already powered
continuously — the always-on circuit is the simpler choice if you're
unsure.

## 6. Naming the Buttons and pairing the two Logic Transmitters (both required)

**Buttons.** Watcher reads all three Buttons via named batch (`lbn`),
not pins, because owning the dedicated Power Controller, LED, zone
gate, and Transmitter already accounts for most of its wiring, and
named addressing was already validated as reliable for this. Each
Button needs a unique name assigned in-game:

1. Get a Labeller.
2. Point it at the exterior Button, rename it exactly `AirlockBtnE`.
3. Point it at the interior Button, rename it exactly `AirlockBtnI`.
4. Point it at the chamber Button, rename it exactly `AirlockBtnC`.

Names must match the code exactly (case-sensitive) — the code computes
each name's hash via `HASH("AirlockBtnE")` etc. If you rename to
something else, either match your own name back into the code's
`define BtnEName HASH("...")` lines, or just use these exact names to
avoid touching the code at all.

**Also double-check `BtnHash` (`-1591419276`) against your own
Stationpedia entry for the Logic Switch / Button structure before
relying on it** — this value came from a single community source in
this project's research pass, not independently cross-confirmed. If
your buttons aren't recognized at all (not just misnamed), this hash
not matching your button's actual structure type is the first thing to
check — see Troubleshooting below.

**The two Logic Transmitters.** This is a physical, in-game pairing
step — the script sets each unit's `Mode` correctly on its own
(`s Transmitter Mode 1` on Watcher, `s Receiver Mode 0` on Cycle), but
it cannot make them find each other. There is no console channel
setting for this, and no separate "Logic Receiver" device — both are
the same "Logic Transmitter" structure, one Active and one Passive:

1. Build both, wire each into its housing (see wiring steps 3 and 4
   above), and power both chips on so the Active one is actually
   broadcasting.
2. On the **Passive** unit (Cycle's), find its tuning dial in its
   build-menu UI and select the Active unit's name from the list of
   currently-broadcasting Active transmitters it can see.
3. If the Active unit doesn't show up as an option, confirm it's
   actually powered and its `Mode` really is 1 — the wiki notes it must
   be on and active to appear in the passive unit's list at all.

Naming the Active unit itself via Labeller first (e.g. `AirlockLink`)
makes it easier to pick out of the list if you have other Transmitters
elsewhere in your base.

## 7. Constants to check before first power-on

Constant name and location are on the bold line; what it means and any
caveats follow underneath.

- **`On`** (zone-gate write) — Watcher, assumed field name
  The LogicType Watcher writes to enable/disable the zone-gate Power
  Controller's output. **Not independently confirmed** — every
  LogicType on other powered devices in this project turned out to be
  `On`, so it's a strong default, but check your own Power Controller's
  Stationpedia entry or Logic Reader "VAR" list before trusting it.

- **`ColorGreen`/`ColorYellow`/`ColorRed`** — Watcher, currently
  `2`/`5`/`4`
  Values written to the LED's `Color` field per Tier. **Not
  independently confirmed** — sourced from aggregated search results
  citing the Community Wiki's "Data Network Colors" page, which
  couldn't be fetched directly. Check that page yourself, or just watch
  what color actually shows up at each Tier during step 8 below and
  adjust the numbers if they're wrong — the branching logic that picks
  a color per Tier doesn't need to change either way.

- **`WakeHold`** — Watcher, currently `20` ticks
  How long the zone gate stays open after the last button press before
  idling again in Low tier. Time an actual transit in-game and adjust —
  20 is an unvalidated starting guess.

- **`TargetInt`** — Cycle, currently `100`
  kPa the chamber pressurizes to before opening the interior door —
  matches your base's standard atmosphere. Adjust if your base doesn't
  run ~100kPa.

- **`TargetExt`** — Cycle, currently `2`
  kPa the chamber evacuates to before opening the exterior door or
  unlocking in Critical — near-vacuum.

- **Door dwell** (`move r11 10`, three places in Cycle) — currently
  `10` ticks
  How long a door stays open before auto-closing. Not occupancy-sensed
  — a fixed timer, same caveat as `WakeHold`.

- **`PropFlagHash`** — Cycle and Gas Sensor chip, currently `-1234567`
  Placeholder shared-flag hash. **Must be identical in both chips**
  (each chip defines its own copy independently — they don't share a
  symbol table) and should correspond to a real device type on your
  network. Replace with a confirmed type-hash before Propped-Open can
  be trusted.

- **`BtnHash`** — Watcher, currently `-1591419276`
  See step 6 — single-sourced, verify against Stationpedia.

- **Transmitter pairing** — Watcher + Cycle, device-level, not in code
  The Passive unit's dial must be pointed at the Active unit's name,
  in-game — see step 6. Not an IC10 `define`, and not a numeric
  channel — an earlier draft of this guide described it as a channel
  setting, which was wrong; it's a name-based physical pairing.

## 8. First-time power-on order

Test incrementally, not everything at once:

1. **Power Watcher alone first.** Confirm the LED shows green, then
   turns yellow as you drop the dedicated Power Controller's charge
   into Low range, then red into Critical, and back to green on
   recovery to Normal. If it stays off or shows the wrong color, the
   `ColorGreen`/`ColorYellow`/`ColorRed` values are the first thing to
   check (see step 7 — flagged unconfirmed). Confirm the zone-gate
   Power Controller's output actually toggles when Watcher writes to it
   — this is the *other* genuinely unconfirmed LogicType in the whole
   build, so verify it here before wiring anything downstream of it.
2. **Power Cycle next**, fed only through the (now-confirmed) zone
   gate. Test in this order:
   - With Watcher in Normal tier and no button pressed, confirm the
     zone gate stays on continuously — this is *not* Deep Idle
     behavior, and shouldn't idle off.
   - Button E from outside while chamber is already exterior-matched →
     exterior Portal opens directly, no cycle, closes itself after the
     dwell timer.
   - Button I from the opposite match state → confirm the Vent actually
     runs and the correct door opens only once the chamber Gas Sensor
     reports target pressure, not immediately.
   - Drain the dedicated Power Controller (or fake it with a small
     battery) to confirm the zone gate actually cuts power between uses
     once Tier drops to Low — this is Deep Idle actually working, now
     one level up from the old per-Portal Transformer approach.
   - Continue draining to Critical — confirm the zone gate comes back
     on with **zero button press**, doors close, the Vent runs an
     evacuation against the chamber sensor's reading, and only *then*
     do the doors unlock.
   - Hold Button C during a Critical evacuation attempt — confirm it's
     skipped that loop, matching the documented override behavior.
3. **Power the Gas Sensor chip last**, if built. Confirm the Propped-Open
   flag toggles when you manually match both Gas Sensors' readings, and
   that Cycle actually props both doors open when it does.

## 9. Known gaps — don't be surprised by these

Carried over from `ic10_airlock_code_notes.md`, not fixed yet:

- **No stall-timeout.** If a Pressurize/Evacuate phase can't reach
  target (not enough gas, target unreachable), Cycle will sit
  re-checking pressure forever rather than giving up. The requirements
  doc's "Stalled" phase and the game's own "Cancel Pressurize" button
  aren't handled in script.
- **Propped-Open mid-mismatch exit ordering isn't specified** — if
  you're using the Gas Sensor chip and a mismatch develops while both
  doors are propped open, which door closes first isn't decided in the
  code.
- **The zone-gate LogicType, `BtnHash`, and the LED `Color` values are
  all unconfirmed** — see step 7. Verify early; they're load-bearing
  for the whole build.
- **The two Logic Transmitters need manual pairing** (step 6) — easy to
  forget since nothing in the script can detect or fix a missed
  pairing. If Cycle seems to never hear from Watcher, this is the first
  thing to check.

## 10. Troubleshooting

- **Buttons don't do anything:** check the Labeller names exactly match
  `AirlockBtnE`/`AirlockBtnI`/`AirlockBtnC` (case-sensitive), then check
  `BtnHash` against your actual button's Stationpedia type hash.
- **LED stays off, or shows a color that doesn't match the Tier you
  expect:** `ColorGreen`/`ColorYellow`/`ColorRed` are unconfirmed
  guesses (see step 7) — check the actual color against the Community
  Wiki's "Data Network Colors" page or just try different values until
  the right ones show up.
- **Zone gate never toggles / Cycle never wakes:** the assumed `On`
  field on the zone-gate Power Controller may not be right — this is
  the single most likely point of failure in the whole build precisely
  because it's the one thing research couldn't confirm. Check it with a
  Logic Reader before assuming anything else is broken.
- **Cycle powers on but doors don't respond to buttons even though the
  zone gate is clearly on:** check the Passive Transmitter's dial is
  actually tuned to the Active one's name (step 6) — this is a manual
  pairing step the script can't verify or fix for you. Confirm the
  Active unit is powered and its `Mode` reads 1 (Watcher sets this
  automatically, but only if `d3` is actually wired).
- **Doors never lock/unlock as expected:** confirm you didn't leave a
  Circuitboard (Airlock) or Circuitboard (Advanced Airlock) also wired
  to the same Portals — this design assumes the Cycle chip is the
  *only* controller on the door, per the Architecture note in the
  requirements doc.
- **Deep Idle never seems to save power / the zone never fully
  de-powers:** confirm the zone-gate Power Controller is genuinely
  gating the whole zone (both Portals, the Vent, and the Cycle IC
  Housing itself) rather than just one of them — if the Cycle chip's
  own housing is on a different circuit than the doors, it'll keep
  drawing its 25W even while the doors are dark, defeating half the
  point.
- **Gas Sensor chip never triggers Propped-Open:** confirm
  `PropFlagHash` is the literal same value in both the Cycle chip and
  the Gas Sensor chip's `define` lines.

## If you already built the earlier Transformer version

This design originally called for a Transformer per Portal instead of
a single Power Controller-gated zone. If you already built that: the
Transformer wiring won't work at all (it has no data port — any
`s XfmrExt On` write was silently going nowhere), so there's nothing to
preserve from that wiring. Swap both Transformers for the single
zone-gate Power Controller in step 4, and move the Cycle chip's own
housing onto that same switched circuit rather than the always-on one.

## Optional afterthought: APC motion-sensor automation

Not part of the core script, and not something you need to add. If you
want it later: a Motion Sensor (or Light Sensor) wired through a Logic
Writer can drive an APC's output directly, with no IC10 involved at
all — genuinely useful for things like ambient lighting near the
airlock that only needs to be on when someone's actually nearby.

**Why it's not built into this design:** the core wake path deliberately
stays button-based because Buttons are the one device confirmed to cost
nothing to monitor even fully unpowered. A Motion Sensor's own idle
power draw hasn't been confirmed the same way, so leaning on it for the
*safety-critical* wake path (the one that has to work even when
everything's nearly dead) would be trading a proven-free primitive for
an unproven one. If you want to experiment with motion-based automation
for something lower-stakes — a convenience light, not the airlock's own
wake logic — it's a reasonable thing to bolt on separately, entirely
outside the IC10 scripts in this project.
