# State Table — `FailsafeController`

Every state `ApplyTierEffects()` can actually produce, traced directly
from `src/FailsafeController.cs` as it stands today (2026-08-07,
post-merge with the concurrent Station Battery redesign — see
`GAP_ANALYSIS.md`'s "Power architecture" for the device-role summary
and its "Design history" timeline for how this got here). Everything
below **replaces** the previous version of this doc, which described
the 2026-08-05 design (percentage-only Tier, no brownout override, no
Low-tier Idle/Active split, no button-driven door control) — see the
changelog at the bottom.

## `UpdateTier()` — how `CurrentTier` gets decided, every tick

| Current `CurrentTier` | `StationBatteryChargeRatio` | Result |
|---|---|---|
| `Normal` | `<= 90` | → `Low` |
| `Normal` | `> 90` | stays `Normal` |
| `Low` | `>= 93` | → `Normal` |
| `Low` | `<= 10` | → `Critical` |
| `Low` | `10 < x < 93` | stays `Low` |
| `Critical` | `> 13` | → `Low` |
| `Critical` | `<= 13` | stays `Critical` |

Hysteresis bands (90/93, 10/13) prevent a charge value sitting right at
a boundary from chattering between tiers every tick — same reasoning
as the original IC10 `watcher.ic10` port. One tier per call, same as
before (`Normal` can't jump straight to `Critical` in one tick).

**No secondary override anymore.** A Cable Analyser-driven
`BasePowerBrownout` (`Required > Potential` on the always-on backbone,
forcing an immediate escalation to `Critical`) existed briefly here and
was reverted (2026-08-08, project owner) — too aggressive once the
Station Battery gave a genuinely trustworthy early-warning reading on
its own: a brief demand/supply blip a healthy battery could easily
absorb was still slamming the airlock into a full evacuate-and-lock
event every time. The charge-based table above is the only thing that
decides `CurrentTier` now.

**Leaving `Low` tier in either direction resets its own sub-state**
(`lowPowerPhase` → `Idle`, `hasBeenOccupiedSinceWake` → `false`) — a
later `Low` entry always starts fresh, never resumes mid-`Active` as
if nothing happened.

## Maintenance mode — overrides everything below

Unchanged from the original design: `MaintenanceModeEnabled` is
checked immediately after `SetWarningIndicator`, before any tier logic
runs. `UpdateTier()` still runs (Tier keeps tracking, the LED/log
indicator keeps updating), but nothing else does.

## Button press semantics — shared across every tier below

**Level vs. edge, two different things read from the same buttons:**

- `wakeRequested` (drives Low tier's Idle→Active wake decision, and
  contributes to `WakeHoldTicks` staying topped up) is **level-based**:
  `host.ButtonEHeld || host.ButtonIHeld || host.ButtonCHeld ||
  host.VanillaCycleRequested || host.PresenceDetected`, read fresh
  every tick.
- `buttonEPressed`/`buttonIPressed` (drive the actual door-cycling
  actions below) are **edge-triggered**: true only on the tick a
  button's level transitions from not-held to held, computed by
  tracking the previous tick's level. Necessary because a
  `LogicButton`'s `Activate` pulse can last up to ~550ms
  (`LogicButton.WaitThenStop`), which can span more than one
  `ApplyTierEffects` tick at this project's ~250ms check interval — an
  action trigger using the raw level would fire twice for one physical
  press (e.g. pressing vanilla's cycle button twice would cancel what
  the first press started).

## Tier: Normal

Downstream power is always on — Normal tier is never Deep Idle, same
as before.

**Buttons now work here too** (2026-08-07 — previously Normal tier
ignored buttons entirely, Deep Idle's wake path was the only thing
that responded to them):

| `buttonEPressed` | `buttonIPressed` | Result |
|---|---|---|
| `true` | — | `host.RequestCycleToward(Exterior)` |
| — | `true` | `host.RequestCycleToward(Interior)` |
| `false` | `false` | no button action this tick |

`RequestCycleToward` (implemented on the host) is a thin pass-through
to vanilla's own `ButtonCycleAirlock()` — the exact method the Console
UI's own cycle button calls — **with no gate at all** (revised
2026-08-07: an earlier version blocked the call when already at the
requested side; removed, since pressing your own side's button while
already there is a real, wanted "courtesy send-back" action, not a
no-op — see `IAirlockHost.RequestCycleToward`'s doc comment). The
`side` argument doesn't change vanilla's behavior at all (it has no
directional concept, and with only two possible sides there's no
ambiguity for it to resolve) — it's informational/logging only. This
also means a repeated press mid-transition reaches
`ButtonCycleAirlock()` again, which vanilla's own switch handles as a
cancel/reverse of that specific step (confirmed via decompile) —
"press again to cancel," matching the real Console button 1:1.

`PropAtmosphereMatched` handling is unchanged from before:

| `PropAtmosphereMatched` | Result |
|---|---|
| `false` | Normal vanilla cycling — unless Propped-Open just broke this tick, see below. |
| `true` | `HoldBothDoorsOpen()` called every tick. |

**Exit ordering when Propped-Open breaks** (unchanged): the one tick
`PropAtmosphereMatched` goes true→false, `CloseNonPreferredDoor()`
runs — closes Exterior/keeps Interior by default, or favors whichever
door *wasn't* most recently used if the optional
`ExteriorPresenceDetected`/`InteriorPresenceDetected` pair is wired.

## Tier: Low

**Redesigned 2026-08-07.** No longer means "a crisis" (that's
`Critical`'s job again, restored) — back to its original meaning,
"battery genuinely getting low," pure downstream-power idle-saving.
Has its own two-phase sub-state machine, `lowPowerPhase`:

**Graceful degradation, checked first, same as before:** if
`!HasWakeButtons || !HasDownstreamController`, downstream power is
just held on continuously (same as Normal) and neither phase below
ever runs.

### Phase: `Idle`

| `wakeRequested` | Result |
|---|---|
| `false` | `SetDownstreamPower(false)` (subject to `WakeHoldTicks` coasting down from the last time it was true) |
| `true` | Power on, `WakeHoldTicks` reset. Also: `host.LockDoors()` (both doors — see below for why), then `if (buttonEPressed) host.OpenDoor(Exterior)` / `if (buttonIPressed) host.OpenDoor(Interior)`, then transition to `Active`. |

**No forced evacuation in this phase** (moved to `Critical`, see
below) — Idle just gates power, it doesn't touch the doors/vents on
its own at all.

**`LockDoors()` before opening, every wake**: vanilla's own
`IsOperable` requires both doors *locked* before its
Pressurizing/Depressurizing cycling will run at all (confirmed via
decompile). Re-locking gives a normal cycle its best chance once the
player is inside using the Console/buttons. **Not yet confirmed
in-game** whether this alone is sufficient in every case.

**`OpenDoor(side)`, the one place a "safe direct open" can happen**:

| Both doors already closed? | Result |
|---|---|
| Yes | Direct open — `OnServer.Interact(door.InteractOpen, 1)` on the requested side only. Safe because nothing else is open to mix gas with, and there's no live vanilla cycle to fight. |
| No | Defers to `RequestCycleToward(side)` instead — a raw open isn't safe if the chamber's current pressure doesn't already match the requested side (confirmed via a real in-game bug, 2026-08-07: opening onto a live, differently-pressurized chamber connects both sides and mixes gas). |

### Phase: `Active`

Power held on unconditionally — vanilla's own cycling needs it to
actually run the traversal; this design doesn't drive the cycle
itself once the door's open.

| `PresenceDetected` | `hasBeenOccupiedSinceWake` | Result |
|---|---|---|
| `true` | (any) | Sets `hasBeenOccupiedSinceWake = true`, resets `ReidleDelayTicks` countdown. |
| `false` | `false` | Never confirmed occupied yet (e.g. button just pressed, door still opening) — stay `Active`, keep waiting. |
| `false` | `true` | Someone genuinely entered and has now left — counts down `ReidleDelayTicks`; once it hits 0, → back to `Idle`. |

**FIXED, 2026-08-07 (was a real gap, confirmed broken live):** buttons
keep working the whole time the airlock is `Active`, not just for the
one press that woke it — `buttonEPressed`/`buttonIPressed` route to
`RequestCycleToward` here too, same vanilla-button pass-through Normal
tier uses. Before this fix, only the wake-triggering press ever
reached a door action; every later press (including a "cancel this
step" or "send it back to the other side" attempt) silently did
nothing.

## Tier: Critical

**Restored 2026-08-07** as its own tier again (it had been folded into
Low's Idle phase during the short-lived brownout-only redesign earlier
the same day; moved back out once percentage staging + the brownout
override could distinguish "a real crisis" from "just getting low"
again). Exactly the evacuate/lock/unlock behavior the original design
had:

Power forced on unconditionally, every tick. `HoldBothDoorsOpen()`
never called here — Propped-Open never persists into Critical.

| `ButtonCHeld` | `SafeToUnlockTemperature` | Result |
|---|---|---|
| `false` | `true` | `ForceEvacuate()` (close + lock both doors, run vent(s) toward vacuum) then `UnlockDoors()` — every tick. |
| `false` | `false` | `ForceEvacuate()` still runs; `UnlockDoors()` doesn't. Chamber sits evacuated and locked until temperature reads safe, rechecked every tick. |
| `true` | — | Both skipped entirely — someone caught inside gets to cancel the lockdown attempt this tick. Power stays on either way. |

**`UnlockDoors()` deliberately unlocks, not just closes** — so a fully
depowered chamber can still be crowbarred open by a tool-less player,
the intended manual fallback once this design's own safety margin runs
out (project owner, 2026-08-07).

## LED indicator

`SetWarningIndicator(tier)` writes `LogicType.Color` on the LED
(`Diode`, PrefabHash `1944485013`) in addition to logging — Green(2)
in `Normal`, Yellow(5) in `Low`, Red(4) in `Critical`. Same ordinals
already confirmed live from `GameManager.CustomColors` for the IC10
build's own indicator, reused here.

## Open questions / not yet resolved

- **The empty-inline-tank stall on a airlock's first-ever cycle**
  (vent physics can't reach target pressure if the tank it's drawing
  from/pushing to has never been charged) is a vanilla-level quirk,
  not something this design controls — flagged so it isn't mistaken
  for a button/cycle-routing bug during testing.
- **A possible async race in vanilla's own `Depressurizing`/
  `Pressurizing` coroutines** if `ButtonCycleAirlock()` gets called
  outside vanilla's own expected press cadence — suspected, not
  confirmed. A temp diagnostic log (`CYCLE-REQUEST` in
  `AdvancedAirlockControlHost.RequestCycleToward`) is in place to catch
  the real state next time a press appears to do nothing.
- **Whether a door needs continuous power just to stay physically
  open, or only to move** — still relevant to Low tier's no-buttons
  fallback row. Sourced figures suggest doors draw power continuously
  while operational, implying position needs power to hold too, but
  this is inference, not direct confirmation.

## Changelog

- **2026-08-08:** Removed `BasePowerBrownout` (the Cable Analyser
  secondary Critical override) entirely — reverted as too aggressive
  now that the Station Battery gives a trustworthy reading on its own.
  Also fixed a real in-game bug: Low tier's `Active` phase wasn't
  routing further button presses to any door action after the initial
  wake, breaking both "cancel the current step" and "send it back to
  the other side" once already awake.
- **2026-08-07 (this rewrite):** Full rewrite for the day's redesign —
  three-tier staging restored on a Station Battery, Low tier split into
  Idle/Active, buttons wired into every tier via `RequestCycleToward`/
  `OpenDoor`, `LED` indicator added, `AllowPowerDownWhilePropped` and
  the `wasIdlingWhileProppedOpen` machinery removed (dead code once Low
  tier stopped doing propped-open handling). See `GAP_ANALYSIS.md`'s
  "Design history" for the full three-generation timeline.
- **2026-08-05 and earlier:** See git history for this file — the
  previous version (Propped-Open tier scope, `HasDownstreamController`,
  Button C resolution, `AllowPowerDownWhilePropped`, temperature check,
  `OnDoorOpened`/`ExtendVentRelief`) is superseded by the rewrite above,
  not reproduced here.
