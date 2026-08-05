# State Table — `FailsafeController`

Every state `ApplyTierEffects()` can actually produce, laid out for
review. Traced directly from `src/FailsafeController.cs` as it stands
today — this is what the code *does*, not a design proposal. Two rows
are flagged **⚠ REVIEW** where the code has a clear, consistent
behavior but it's not obvious that behavior is what you actually want —
those are genuine open questions, not bugs I'm claiming exist.

## Tier: Normal

Downstream power is always on (line 245) — Normal tier is never Deep
Idle, ported directly from `watcher.ic10`'s `forceHold` on Tier 0.

| `PropAtmosphereMatched` | Result |
|---|---|
| `false` | Normal vanilla cycling (button-driven pressurize/evacuate against target). |
| `true` | `HoldBothDoorsOpen()` called every tick — both doors commanded open continuously, bypassing normal cycling for as long as the match holds. |

## Tier: Low

Downstream power depends on `HasWakeButtons` first, then (if true) on
`wakeRequested`/`wakeHoldRemaining`.

| `HasWakeButtons` | `wakeRequested` this tick | `wakeHoldRemaining` | Downstream power | Notes |
|---|---|---|---|---|
| `false` | — | — | **On, continuously** | Same as Normal, power-wise. Deep Idle doesn't run at all — see your last question's answer. |
| `true` | `true` (a button/Console-click/presence event) | — | On, `wakeHoldRemaining` reset to 20 | Vanilla's normal button-driven cycling can proceed. |
| `true` | `false` | `> 0` | On (coasting on the hold timer), decrements by 1 | Nothing new requested, but still within the wake-hold window from the last event. |
| `true` | `false` | `0` | **Off** — Deep Idle | The actual power-saving state. Doors/Vent unpowered; buttons still readable (confirmed unpowered-safe), nothing else is. |

**⚠ REVIEW — `PropAtmosphereMatched` is never checked in Low tier at
all**, in *any* of the four rows above — including the top row, where
downstream power is continuously on, functionally identical to Normal.
Ported faithfully from `cycle.ic10` (`beq r0 0 checkProp` — literally
only Tier 0), where the original design note was "Propped-Open only
matters during Normal tier anyway when the zone is already powered
continuously" — but that assumption doesn't fully hold anymore, since
the no-buttons Low-tier row *is* continuously powered too. **Question
for you:** should Propped-Open also apply whenever downstream power is
being held on continuously, regardless of which tier that's happening
in — or should it stay strictly Normal-tier-only, matching the letter
of the original IC10 port even though the reasoning behind that choice
has partly eroded? Either is a coherent, implementable answer; the code
currently does the latter by inheritance, not by a fresh decision.

## Tier: Critical

Downstream power is always forced on (line 226) — has to be, to run the
evacuation regardless of whether anyone's present to press anything.

| `ButtonCHeld` | Result |
|---|---|
| `false` | `ForceEvacuateAndUnlock()` runs: close both doors → evacuate to `TargetExt` → unlock both doors once chamber sensor confirms. **If the doors were propped open coming into this tier, this explicitly closes them as its first action** — a real Critical event safely overrides Propped-Open, no gap there. |
| `true` | `ForceEvacuateAndUnlock()` is skipped entirely — nothing acts on the doors this tick, they're left exactly as they physically were. |

**⚠ REVIEW — the `ButtonCHeld == true` row means "freeze in place,"
including if the doors happen to already be propped open.** If Tier
drops straight from Normal (propped open, both doors open) into
Critical while Button C is being held, the doors stay open and
unlocked for as long as C is held — nothing re-closes them, but nothing
re-affirms the open-hold either, it's just inert. Matches the
documented override intent ("someone caught inside gets to cancel the
lockdown attempt") in the sense that the evacuation genuinely doesn't
run — but it's worth confirming this is the behavior you want in the
specific case where the chamber was propped open (arguably already
"safe," matched atmosphere on both sides) versus the more typical case
the override was designed for (someone mid-transit, chamber not
matched). **Question for you:** should holding C during Critical, while
Propped-Open's match condition is still true, actively keep re-calling
`HoldBothDoorsOpen()` (an explicit "stay propped" decision), or is
"just don't force-close them" sufficient?

## Transition notes

- **Tier can only move one step per `UpdateTier()` call** (Normal↔Low,
  Low↔Critical) — ported faithfully from `watcher.ic10`'s branching,
  which never jumps Normal→Critical directly even on a sudden full
  power loss. Practically: at least one tick is always spent in Low
  before reaching Critical, even if charge drops to 0% in a single
  tick. Given how fast ticks run, this almost certainly isn't a
  real-world safety gap, just worth knowing it's there.
- **What happens to a physically-propped-open door the instant Tier
  leaves Normal (before Critical's explicit close, if it even gets
  there)?** `HoldBothDoorsOpen()` simply stops being called — nothing
  in this controller tells the door to close either. Whether the door
  then auto-closes on vanilla's own normal timer, or just hangs open
  until Critical explicitly closes it (or a player acts), depends on
  how vanilla's own door-open call behaves — **unconfirmed, Milestone
  1.5 territory**, not something decidable from this sandbox.

## Not affected by any of the above

- `ButtonCHeld`'s role in Critical and `PropAtmosphereMatched`'s role
  in Normal are fully independent of each other and of `HasWakeButtons`
  — no combination of "buttons missing" + "gas sensors missing" +
  "presence sensor missing" produces a different Critical-tier
  evacuation outcome. The safety-critical path stays constant no matter
  what optional hardware is or isn't present.
