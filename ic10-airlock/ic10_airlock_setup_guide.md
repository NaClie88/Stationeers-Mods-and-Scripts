# IC10 Fail-Safe Airlock — First-Time Setup Guide

A build-order checklist for turning `ic10_airlock_prototype_code.md`
into a working airlock. Read `ic10_failsafe_airlock_requirements.md`
first if you haven't — this doc assumes you already know *why* the
design looks like this and just need to know what to physically do.

Everything here reflects the 2026-08-04 state of the code (all three
chips complete, dry-run verified in an emulator — not yet verified
in-game, which is what this guide is for).

## 1. Hardware shopping list

| Device | Qty | Notes |
|---|---|---|
| IC Housing | 2 (+1 optional) | Chip A, Chip B required; Chip C optional |
| IC10 chip | 2 (+1 optional) | One per housing above |
| Portal (Airlock door) | 2 | Exterior + Interior — **not** a Circuitboard (Airlock) or Circuitboard (Advanced Airlock); this design replaces that hardcoded circuit entirely |
| Transformer (or equivalent switching device) | 2 | One per Portal, own power line, separate from the IC10/Button/Light circuit — required for Deep Idle Mode to work at all |
| Active Vent | 1 | Chamber evacuate/pressurize. **Do not substitute a Powered Vent or Large Powered Vent unless your dedicated battery is sized well above Small** — see the requirements doc's power-draw warning (Large Powered Vent confirmed 500W vs. Active Vent's 100W) |
| Power Controller (dedicated, own battery) | 1 | **Must be physically inside the chamber**, not the base interior or outside — this is the actual backstop that lets a trapped player self-rescue by swapping the battery. Budget footprint per section 2 below |
| Light | 1 | Dual-purpose warning signal + inter-chip flag. Mount visibly at the portal, not tucked in a rack |
| Logic Switch ("Button") | 3 | E (exterior), I (interior/base side), C (inside the chamber itself) |
| Gas Sensor | 2 (optional, for Chip C) | One exterior-facing, one interior-facing — only needed if you want the Propped-Open feature |

## 2. Placement and footprint

- **Power Controller: inside the chamber.** Non-negotiable per the
  requirements doc — this is what lets a player caught in a fully dead
  airlock crowbar it open and swap the battery themselves.
- **Chamber footprint: budget 1–2 grid volumes** for the chamber
  itself (Portals + Power Controller + Light + Button C), **plus at
  least 1 more grid of spillover** for hardware that doesn't fit inside
  the chamber proper — specifically the Vent's pressure tanks for cycle
  air.
- **Light: mounted at the portal, visible from outside it.** Its second
  job is warning a player standing there, not just signaling Chip B.
- **Button C: inside the chamber**, not at either portal — it's the
  wake/override for someone already caught inside.
- **Button E / Button I: outside the chamber**, one per side, wherever
  makes sense for approach traffic.

## 3. Wiring — Chip A (Power Monitor)

IC Housing pins, 2 of 6 used:

| Pin | Device |
|---|---|
| `d0` | dedicated Power Controller |
| `d1` | the shared Light |

Nothing else connects to this housing. Load `Chip A` from the
prototype doc.

## 4. Wiring — Chip B (Door / Vent / Button Controller)

IC Housing pins, **all 6 used** — this chip has no spare pins:

| Pin | Device |
|---|---|
| `d0` | the shared Light (same physical Light as Chip A — wire it to both housings) |
| `d1` | exterior Portal |
| `d2` | interior Portal |
| `d3` | exterior Portal's Transformer |
| `d4` | interior Portal's Transformer |
| `d5` | Active Vent |

**The three Buttons are not pin-wired at all** — they're read by name
over the network instead (see step 6 below for why and how). Don't
plug any Button into an unused pin on this housing; there isn't one to
spare in the first place, and the code doesn't expect it there.

Load `Chip B` from the prototype doc.

## 5. Wiring — Chip C (optional, Gas Sensor / Propped-Open Monitor)

Only build this if you installed the two Gas Sensors.

| Pin | Device |
|---|---|
| `d0` | exterior-facing Gas Sensor |
| `d1` | interior-facing Gas Sensor |

Load `Chip C` from the prototype doc. If you skip this chip entirely,
Chip B still works — it just never enters the Propped-Open state.

## 6. Naming the three Buttons (required — Chip B won't see them otherwise)

Chip B reads all three Buttons via named batch (`lbn`), not pins,
because owning the Light + 2 Portals + 2 Transformers + Vent already
fills all 6 available pins. This means **each Button needs a unique
name assigned in-game before Chip B can find it**:

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

All of these live at the top of Chip B (and one in Chip C) as `define`
lines. None are guesses about *whether* they're needed — they're
starting values flagged in the requirements doc as needing your own
in-game tuning:

| Constant | Chip | Current value | What it means |
|---|---|---|---|
| `TargetInt` | B | `100` | kPa the chamber pressurizes to before opening the interior door — matches your base's standard atmosphere. Adjust if your base doesn't run ~100kPa. |
| `TargetExt` | B | `2` | kPa the chamber evacuates to before opening the exterior door or unlocking in Critical — near-vacuum. |
| door dwell (`move r11 10`, three places in Chip B) | B | 10 ticks | How long a door stays open before auto-closing. Not occupancy-sensed — a fixed timer. Time an actual transit in-game and adjust; 10 is an unvalidated starting guess. |
| `PropFlagHash` | B and C | `-1234567` | Placeholder shared-flag hash. **Must be identical in both chips** (each chip defines its own copy independently — they don't share a symbol table) and should correspond to a real device type on your network. Left as a placeholder because it wasn't resolved during research — replace with a confirmed type-hash before Chip C's Propped-Open feature can be trusted. |

## 8. First-time power-on order

Test incrementally, not all three chips at once:

1. **Power Chip A alone first.** Confirm the Light's `Setting` changes
   as you vary the Power Controller's charge (or just watch it read `0`
   / Normal with a full battery). This validates the hysteresis logic
   in isolation before anything else depends on it.
2. **Power Chip B next**, with Chip A already running so it has a real
   Tier value to read from the Light. Test in this order:
   - Normal tier, Button E from outside → exterior Portal opens, closes
     itself after the dwell timer.
   - Normal tier, Button I from the opposite match state → confirm the
     Vent actually runs and the correct door opens only once target
     pressure is reached, not immediately.
   - Drain the dedicated Power Controller (or fake it by testing with a
     small battery) to confirm Deep Idle Mode actually cuts Transformer
     power between uses once Tier drops to Low.
   - Continue draining to Critical — confirm doors close, the Vent
     runs an evacuation, and only *then* do the doors unlock.
   - Hold Button C during a Critical evacuation attempt — confirm it's
     skipped that loop, matching the documented override behavior.
3. **Power Chip C last**, if built. Confirm the Propped-Open flag
   toggles when you manually match both Gas Sensors' readings, and that
   Chip B actually props both doors open when it does.

## 9. Known gaps — don't be surprised by these

Carried over from the prototype doc, not fixed yet:

- **No stall-timeout.** If a Pressurize/Evacuate phase can't reach
  target (not enough gas, target unreachable), Chip B will sit
  re-checking pressure forever rather than giving up. The requirements
  doc's "Stalled" phase and the game's own "Cancel Pressurize" button
  aren't handled in script.
- **Propped-Open mid-mismatch exit ordering isn't specified** — if
  you're using Chip C and a mismatch develops while both doors are
  propped open, which door closes first isn't decided in the code.
- **Chip B has no spare lines** (127 of 128 used). Any future change
  has to remove something before adding something else.

## 10. Troubleshooting

- **Buttons don't do anything:** check the Labeller names exactly match
  `AirlockBtnE`/`AirlockBtnI`/`AirlockBtnC` (case-sensitive), then check
  `BtnHash` against your actual button's Stationpedia type hash.
- **Doors never lock/unlock as expected:** confirm you didn't leave a
  Circuitboard (Airlock) or Circuitboard (Advanced Airlock) also wired
  to the same Portals — this design assumes the IC10 is the *only*
  controller on the door, per the Architecture note in the requirements
  doc.
- **Deep Idle never seems to save power / doors never fully de-power:**
  confirm the Transformers are wired in the Portals' own power line,
  separate from the circuit feeding the IC10s/Buttons/Light — if
  they're on the same circuit, cutting Portal power would also kill the
  chip watching for the wake trigger.
- **Chip C never triggers Propped-Open:** confirm `PropFlagHash` is the
  literal same value in both Chip B and Chip C's `define` lines.
