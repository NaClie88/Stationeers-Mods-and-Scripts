# State Table — `FailsafeController`

Every state `ApplyTierEffects()` can actually produce, laid out for
review. Traced directly from `src/FailsafeController.cs` as it stands
today. All open questions from earlier passes of this table are now
resolved — see the changelog at the bottom for what changed and when.

## Tier: Normal

Downstream power is always on — Normal tier is never Deep Idle, ported
directly from `watcher.ic10`'s `forceHold` on Tier 0.

| `PropAtmosphereMatched` | Result |
|---|---|
| `false` | Normal vanilla cycling (button-driven pressurize/evacuate against target). |
| `true` | `HoldBothDoorsOpen()` called every tick — both doors commanded open continuously, bypassing normal cycling for as long as the match holds. |

## Tier: Low

Downstream power depends on `HasWakeButtons` **and**
`HasDownstreamController` first (both required, added 2026-08-05 —
see changelog), then, only if both hold, on
`wakeRequested`/`wakeHoldRemaining` — and `wakeRequested` includes
`PropAtmosphereMatched`.

| `HasWakeButtons` | `HasDownstreamController` | `wakeRequested` this tick (buttons / Console-click / presence / matched atmosphere) | `wakeHoldRemaining` | Downstream power | Propped-Open? |
|---|---|---|---|---|---|
| `false` | — | — | — | **On, continuously** | Called if matched — power's already on regardless. |
| — | `false` | — | — | **On, continuously** | Called if matched — power's already on regardless. No APC found to switch, so there's nothing to idle even if buttons exist. |
| `true` | `true` | `true` | — | On, `wakeHoldRemaining` reset to 20 | Called if matched. If the *only* reason `wakeRequested` is true is a match (no button/click/presence), this is what's actually keeping the circuit awake — unless `AllowPowerDownWhilePropped` is enabled, see below. |
| `true` | `true` | `false` | `> 0` | On (coasting on the hold timer), decrements by 1 | Called if matched (power's still on from the countdown). |
| `true` | `true` | `false` | `0` | **Off** — Deep Idle | Not called by default — a steady match would normally have kept `wakeRequested` true and prevented reaching this row. **Reachable on purpose if `AllowPowerDownWhilePropped` is enabled** — see below. |

**Resolved (2026-08-05):** whether Propped-Open applies beyond Normal
tier now falls entirely out of where the Gas Sensors are physically
wired, not a Tier check:

- **Gas Sensors on the switched downstream circuit** (need power to
  function) → can only ever read a match while downstream power is
  already on, so this can never pull the circuit awake from a genuine
  Deep Idle on its own. Effectively stays powered-state-only, matching
  the original "only for normal operation" intent.
- **Gas Sensors on the always-on circuit** (confirmed unpowered-safe,
  same as Buttons) → can read a match even during what would otherwise
  be Deep Idle, so a genuine atmosphere match becomes its own wake
  reason and the doors stay propped open across Low tier too. This
  requires `HasWakeButtons` to be true for Deep Idle to be reachable at
  all in the first place — see the no-buttons row above, where power's
  unconditionally on regardless.

**New (2026-08-05): `AllowPowerDownWhilePropped` lets the last row
above be reached even while genuinely matched.** Setup-time choice
(`IAirlockHost.AllowPowerDownWhilePropped`), not a live sensor read —
off by default. When enabled, a *steady* match no longer forces power
on by itself, on the assumption that a door doesn't need continuous
power just to stay in position (still unconfirmed, see "Transition
notes" below) and all three Gas Sensors are wired to the always-on
circuit so monitoring continues regardless. Critically, this doesn't
turn off monitoring — a match breaking (`mismatchJustAppeared` in the
code) still forces an immediate wake even with this enabled, tracked
via a one-tick-lookback flag (`wasIdlingWhileProppedOpen`) so an
ordinary not-matched tick elsewhere in the game is never mistaken for
"the propped state just broke." Doors get closed/managed normally once
that wake happens, same as any other Low-tier wake.

## Tier: Critical

Downstream power is always forced on — has to be, to run the
evacuation regardless of whether anyone's present to press anything.
`HoldBothDoorsOpen()` is never called in this tier in any row below —
**confirmed (2026-08-05): Propped-Open never persists into Critical,
full stop.** That part of the question is fully closed.

| `ButtonCHeld` | Result |
|---|---|
| `false` | `ForceEvacuateAndUnlock()` runs: close both doors → evacuate to `TargetExt` → unlock both doors once chamber sensor confirms. If the doors were propped open coming into this tier, this explicitly closes them as its first action. |
| `true` | `ForceEvacuateAndUnlock()` is **skipped** — nothing acts on the doors this tick, they're left exactly as they physically were. |

**Resolved (2026-08-05).** `ButtonCHeld`'s row above stays exactly as
coded — unconditional skip when held, unchanged from the original IC10
design's tested behavior. Nothing about the propped-open answer ever
required changing it; that question was fully independent, as noted
above.

What actually resolves the ambiguity: this code path is now expected to
be the **fallback**, not the primary mechanism. Since the Console sits
inside the chamber by default (`GAP_ANALYSIS.md`, "Reusing vanilla's
Skip instead of custom Button C hardware"), a trapped player likely
never needs `ButtonCHeld` at all — they use vanilla's own Skip button
at the Console that's already there, and (pending Milestone 1.5)
`ForceEvacuateAndUnlock()` calling into vanilla's own evacuate method
means that Skip affordance comes along for free, no `ButtonCHeld` check
in the loop at that point. `ButtonCHeld` remains on `IAirlockHost`
purely for anyone who builds the optional physical button anyway, in
which case it should behave exactly like the original design — which
is what's already implemented. No code change was needed here; the
open question was really "which path is primary," not "what should the
skip behavior be," and that's now answered.

## Transition notes

- **Tier can only move one step per `UpdateTier()` call** (Normal↔Low,
  Low↔Critical) — ported faithfully from `watcher.ic10`'s branching,
  which never jumps Normal→Critical directly even on a sudden full
  power loss. Practically: at least one tick is always spent in Low
  before reaching Critical, even if charge drops to 0% in a single
  tick. Given how fast ticks run, this almost certainly isn't a
  real-world safety gap, just worth knowing it's there.
- **What happens to a physically-propped-open door the instant Tier
  reaches Critical?** `HoldBothDoorsOpen()` stops being called (never
  called in Critical, any row) — and if `ButtonCHeld` is false,
  `ForceEvacuateAndUnlock()` explicitly closes both doors as its first
  action, so this case is fully handled. Normal↔Low transitions no
  longer lose Propped-Open at all now that Low also checks it — the
  only real "stops being held" moment is the Critical transition.
- **Does a door need continuous power just to stay physically open, or
  only to move?** Still relevant for the no-buttons Low-tier row and
  the Deep Idle row — if a door can hold its position without power,
  "downstream power is off" might not immediately close a door that
  was already open. This project's own sourced figures suggest doors
  draw power continuously while operational (`SOURCES.md`'s Composite
  Door / Blast Door entries), which would mean position does need
  power to hold, not just to move — but this is inference, not a
  direct confirmation. **Unconfirmed, Milestone 1.5 / in-game
  territory.**

## Not affected by any of the above

- `ButtonCHeld`'s role in Critical is independent of `PropAtmosphereMatched`
  and `HasWakeButtons` — no combination of "buttons missing" / "gas
  sensors missing" / "presence sensor missing" produces a different
  Critical-tier evacuation outcome (when `ButtonCHeld` is false). The
  safety-critical default path stays constant no matter what optional
  hardware is or isn't present. `ButtonCHeld == true`'s exact effect is
  the one remaining open question, above.

## Changelog

- **2026-08-05:** Propped-Open's tier scope resolved — folded into the
  Low-tier wake condition rather than gated by a Tier check, so it now
  naturally extends into Low tier when (and only when) the end user's
  Gas Sensor wiring choice makes that possible. Never applies in
  Critical, unconditionally, confirmed.
- **2026-08-05:** Added `HasDownstreamController` — Deep Idle now
  requires both a confirmed-safe wake mechanism (`HasWakeButtons`) and
  something to actually switch (`HasDownstreamController`), gated
  symmetrically. Prompted by project owner noting an APC only exposes
  its logic on its power-source side, not downstream — meaning the
  card has to actually confirm one is present and controllable rather
  than assume it.
- **2026-08-05:** Button C's interaction with Critical resolved. No
  code change — `ButtonCHeld`'s unconditional skip stays exactly as
  coded, unchanged from the original IC10 design. What resolved the
  ambiguity was recognizing this path is now the *fallback*: with the
  Console inside the chamber by default, vanilla's own Skip button
  likely covers the trapped-player case without `ButtonCHeld` ever
  entering into it — see `GAP_ANALYSIS.md`'s "Reusing vanilla's Skip
  instead of custom Button C hardware."
- **2026-08-05:** Added `AllowPowerDownWhilePropped` — opt-in,
  off by default, lets Deep Idle engage even with a genuine atmosphere
  match, provided all three Gas Sensors are wired to the always-on
  circuit. A mismatch that breaks a prior propped-and-idling state
  still forces an immediate wake regardless of the setting, tracked via
  `wasIdlingWhileProppedOpen` — monitoring never actually turns off,
  only the "stay awake just because it's still matched" behavior does.
