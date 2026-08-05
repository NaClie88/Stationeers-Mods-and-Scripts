# IC10 Fail-Safe Airlock — First-Time Setup Guide

A build-order checklist for turning `ic10_airlock_prototype_code.md`
into a working airlock. Read `ic10_failsafe_airlock_requirements.md`
first if you haven't — this doc assumes you already know *why* the
design looks like this and just need to know what to physically do.

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
  Large Powered Vent unless your dedicated battery is sized well above
  Small — see the requirements doc's power-draw warning (Large Powered
  Vent confirmed 500W vs. Active Vent's 100W).

- **Power Controller — dedicated battery** — qty **1**
  **Must be physically inside the chamber**, not the base interior or
  outside — this is the actual backstop that lets a trapped player
  self-rescue by swapping the battery. Feeds Watcher continuously and,
  via the zone gate above, the Cycle chip and doors.

- **Light** — qty **1**
  Dual-purpose warning signal + inter-chip flag. Mount visibly at the
  portal, not tucked in a rack. Wired to **both** IC Housings.

- **Logic Switch ("Button")** — qty **3**
  E (exterior), I (interior/base side), C (inside the chamber itself) —
  all read by Watcher only, none wired to Cycle.

- **Logic Transmitter** — qty **1**
  On Watcher. Relays live button state to Cycle across the two
  independently-powered circuits.

- **Logic Receiver** — qty **1**
  On Cycle. Tuned to the same channel as the Transmitter above.

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
  itself (Portals + dedicated Power Controller + Light + Button C +
  the new chamber Gas Sensor), **plus at least 1 more grid of
  spillover** for hardware that doesn't fit inside the chamber proper —
  specifically the Vent's pressure tanks for cycle air, the zone-gate
  Power Controller, and the two IC Housings.
- **Light: mounted at the portal, visible from outside it**, with wires
  run to both IC Housings. Its second job is warning a player standing
  there, not just signaling between chips.
- **Button C: inside the chamber**, not at either portal — it's the
  wake/override for someone already caught inside.
- **Button E / Button I: outside the chamber**, one per side, wherever
  makes sense for approach traffic.
- **Logic Transmitter/Receiver: no placement constraint beyond being
  powered on their respective circuits** — Transmitter on Watcher's
  always-on side, Receiver on Cycle's gated side. Tune both to the same
  channel via their own console/build menu before relying on them (a
  one-time setup step, not something the script does).

## 3. Wiring — Watcher (always powered)

IC Housing pins, 4 of 6 used:

| Pin | Device |
|---|---|
| `d0` | dedicated Power Controller (the one inside the chamber) |
| `d1` | the shared Light |
| `d2` | the zone-gate Power Controller |
| `d3` | Logic Transmitter |

Nothing else connects to this housing — the three Buttons are read by
name over the network (see step 6), not pin-wired. Load `Watcher` from
the prototype doc. **This chip is never power-gated** — it feeds from
the same always-on circuit as the dedicated Power Controller itself.

## 4. Wiring — Cycle (powered only when the zone gate is on)

IC Housing pins, **all 6 used**:

| Pin | Device |
|---|---|
| `d0` | the shared Light (same physical Light as Watcher — wire it to both housings) |
| `d1` | exterior Portal |
| `d2` | interior Portal |
| `d3` | Active Vent |
| `d4` | Logic Receiver |
| `d5` | the chamber Gas Sensor |

Load `Cycle` from the prototype doc. **This entire housing, along with
both Portals and the Vent, sits on the zone-gate Power Controller's
switched output** — when Watcher cuts the gate, this whole circuit
(including the Cycle chip itself) loses power together. That's
deliberate: Cycle doesn't need its own Deep Idle logic anymore, because
it simply doesn't run at all while idle.

## 5. Wiring — Gas Sensor chip (optional)

Only build this if you installed the two exterior/interior-facing Gas
Sensors (not the chamber one from step 4, which belongs to Cycle).

| Pin | Device |
|---|---|
| `d0` | exterior-facing Gas Sensor |
| `d1` | interior-facing Gas Sensor |

Load the Gas Sensor chip from the prototype doc. If you skip this chip
entirely, Cycle still works — it just never enters the Propped-Open
state. This chip can live on either circuit (always-on or gated) since
Propped-Open only matters during Normal tier anyway when the zone is
already powered continuously — the always-on circuit is the simpler
choice if you're unsure.

## 6. Naming the three Buttons (required — Watcher won't see them otherwise)

Watcher reads all three Buttons via named batch (`lbn`), not pins,
because owning the dedicated Power Controller, Light, zone gate, and
Transmitter already accounts for most of its wiring, and named
addressing was already validated as reliable for this. Each Button
needs a unique name assigned in-game:

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

## 7. Constants to check before first power-on

Constant name and location are on the bold line; what it means and any
caveats follow underneath.

- **`On`** (zone-gate write) — Watcher, assumed field name
  The LogicType Watcher writes to enable/disable the zone-gate Power
  Controller's output. **Not independently confirmed** — every
  LogicType on other powered devices in this project turned out to be
  `On`, so it's a strong default, but check your own Power Controller's
  Stationpedia entry or Logic Reader "VAR" list before trusting it.

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

- **Transmitter/Receiver channel** — Watcher + Cycle, device-level
  setting, not in code
  Set on both devices via their own console/build menu, must match.
  Not an IC10 `define` — a one-time hardware configuration step.

## 8. First-time power-on order

Test incrementally, not everything at once:

1. **Power Watcher alone first.** Confirm the Light's `Setting` changes
   as you vary the dedicated Power Controller's charge. Confirm the
   zone-gate Power Controller's output actually toggles when Watcher
   writes to it — this is the one genuinely unconfirmed LogicType in
   the whole build (see step 7), so verify it here before wiring
   anything downstream of it.
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

Carried over from the prototype doc, not fixed yet:

- **No stall-timeout.** If a Pressurize/Evacuate phase can't reach
  target (not enough gas, target unreachable), Cycle will sit
  re-checking pressure forever rather than giving up. The requirements
  doc's "Stalled" phase and the game's own "Cancel Pressurize" button
  aren't handled in script.
- **Propped-Open mid-mismatch exit ordering isn't specified** — if
  you're using the Gas Sensor chip and a mismatch develops while both
  doors are propped open, which door closes first isn't decided in the
  code.
- **The zone-gate LogicType and `BtnHash` are both unconfirmed** — see
  step 7. Verify both early; they're load-bearing for the whole build.

## 10. Troubleshooting

- **Buttons don't do anything:** check the Labeller names exactly match
  `AirlockBtnE`/`AirlockBtnI`/`AirlockBtnC` (case-sensitive), then check
  `BtnHash` against your actual button's Stationpedia type hash.
- **Zone gate never toggles / Cycle never wakes:** the assumed `On`
  field on the zone-gate Power Controller may not be right — this is
  the single most likely point of failure in the whole build precisely
  because it's the one thing research couldn't confirm. Check it with a
  Logic Reader before assuming anything else is broken.
- **Cycle powers on but never receives a wake reason / doors don't
  respond to buttons even though the zone gate is clearly on:** check
  the Logic Transmitter and Receiver are tuned to the same channel.
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
