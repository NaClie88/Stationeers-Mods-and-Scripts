# Patch Plan — Wiring `FailsafeController` into the Real Vanilla Class

`src/FailsafeController.cs` is done and game-independent. This doc is
the checklist for Milestone 1.5: what to find in `Assembly-CSharp.dll`
(dnSpy/ILSpy) so a thin Harmony `IAirlockHost` adapter can wire it up.
Each `IAirlockHost` member below needs one real answer.

## What to find, per `IAirlockHost` member

- **`DedicatedBatteryChargeRatio`** — find how the vanilla circuit (or
  the Console it's inserted into) already reads any wired
  Power Controller's `Charge`/`Maximum` — the LogicType names
  themselves are already confirmed for IC10 use
  (`ic10-airlock/watcher.ic10` lines 32-33, sourced in `SOURCES.md`),
  the open question is purely the C# property/field name on whatever
  class exposes it, if the vanilla circuit exposes a Power Controller
  reference at all. **If it doesn't** (vanilla's cycling logic may not
  care about power source at all, only whether it currently has
  power), this needs its own new field on the patched class — a
  reference to a specific Power Controller, set some other way (a new
  Console setting? Hardcoded to "whatever's on the same network"?).
  Flag whichever is true when you find it.
- **`ButtonEHeld`/`ButtonIHeld`/`ButtonCHeld`** — find how the vanilla
  circuit reads button/switch input at all (its Console UI buttons vs.
  physical Logic Switches wired to the airlock). This project's IC10
  side uses `lbn` with a hashed name per button (`AirlockBtnE`/`I`/`C`,
  see `watcher.ic10`) — the vanilla class may use something else
  entirely, e.g. UI button click callbacks rather than any hashed
  device read. `ButtonEHeld`/`ButtonIHeld` feed the downstream-power
  wake logic — if vanilla has no concept of a chamber-interior button
  at all, new wiring is needed for those. **`ButtonCHeld` specifically
  is now a fallback, not the primary plan** — see Milestone 0.5 in
  `README.md` and `GAP_ANALYSIS.md`'s "Reusing vanilla's Skip instead
  of custom Button C hardware": the Console already sits inside the
  chamber in the traditional layout, so a trapped player already has
  Skip access to vanilla's own stall-cancel mechanism with nothing
  extra to build, and `ForceEvacuate()`/`UnlockDoors()` may not need a
  separate override check at all. Don't invest heavily in finding a
  hashed-button hook for Button C until Milestone 0.5 comes back
  negative.
- **`HasWakeButtons`** — the actual gate for whether Deep Idle runs at
  all (see `GAP_ANALYSIS.md`'s "Graceful degradation" section). Just a
  presence check: does this circuit have a physical E or I button
  wired at all, however Milestone 1.5 finds button-wiring is detected.
  Straightforward once `ButtonEHeld`/`ButtonIHeld` above are resolved —
  this is likely "is the reference/hash null" on whatever those turn
  out to be built from.
- **`VanillaCycleRequested`** — optional, nice-to-have, not blocking.
  Whatever vanilla's real cycle-trigger path is (Console UI click
  handler most likely), surfacing it as a boolean the patch can read
  every tick lets someone who wired buttons also wake the circuit from
  the Console. Correctness doesn't depend on finding this — Deep Idle
  already works off buttons alone via `HasWakeButtons` above. Skip this
  one if it's not a clean/obvious hook.
- **`PresenceDetected`** — optional. No vanilla equivalent, needs its
  own new field referencing a Presence/Motion Sensor the same way
  `PropAtmosphereMatched` needs Gas Sensor references. **Must be wired
  to the always-on side**, not the switched downstream circuit — see
  `GAP_ANALYSIS.md`'s "Presence sensor placement" section for why (the
  sensor has to detect someone approaching *before* downstream power
  comes back on, so it can't itself be behind the thing it's supposed
  to wake).
- **`PropAtmosphereMatched`** — almost certainly has no vanilla
  equivalent (see `GAP_ANALYSIS.md`) — this will likely need its own
  new field/method entirely, fed by two Gas Sensor references the way
  `gas_sensor.ic10` does it, not a hook into anything existing.
- **`AllowPowerDownWhilePropped`** — not a device read at all, a setup
  toggle (project owner, 2026-08-05). No decompiler work needed —
  needs an actual settings surface for the end user to opt in (a new
  Console setting field, most likely, following whatever pattern
  vanilla's own Interior/Exterior #Pa settings already use). Off by
  default; correctness of `FailsafeController` doesn't depend on this
  existing at all for Milestone 2 — safe to stub as always-`false`
  until a settings UI is worth building.
- **`ForceEvacuate()` / `UnlockDoors()`** — split from a single
  `ForceEvacuateAndUnlock()` (2026-08-05) so `SafeToUnlockTemperature`
  (below) can gate just the unlock step. Find whatever method vanilla's
  own Critical-adjacent logic (if any exists) or its normal
  evacuate-cycle method looks like, so `ForceEvacuate()` can call
  *into* it rather than reimplementing vent control, matching
  `cycle.ic10`'s `tierCrit` block (lines 108-119): close both doors →
  run Vent evacuate to `TargetExt`. **Prefer calling vanilla's own
  evacuate-toward-target method over a custom implementation** if one
  exists — per the Skip-button reasoning above, that's very likely the
  same method whose stall already carries vanilla's own Skip/Cancel
  affordance, meaning the trapped-player override comes along for free
  rather than needing a hand-rolled check. **The split creates a real
  question to resolve here:** if vanilla's own evacuate method unlocks
  as part of the same call (rather than as a separate step), calling it
  from `ForceEvacuate()` doesn't naturally let `UnlockDoors()` withhold
  anything — the adapter would need to immediately re-lock right after
  vanilla's call if `SafeToUnlockTemperature` is false, rather than
  vanilla's unlock genuinely being skippable. Confirm which shape
  vanilla's method actually has before assuming either.
- **`SafeToUnlockTemperature`** — needs a temperature reference for
  whatever's on the far side of the evacuation target (the "Exterior"
  side settings already define, most likely) and a safe-range
  definition. No existing vanilla concept to hook into — this is new,
  same as `PropAtmosphereMatched`'s Gas Sensor references.
- **`HoldBothDoorsOpen()`** — likely a direct call into whatever method
  vanilla uses to open a door (called twice, once per door), skipping
  the normal auto-close timer. Confirm whether vanilla's door-open call
  has an optional "don't auto-close" parameter, or whether this needs
  a small patch of its own to suppress the auto-close while Propped-Open
  is active.
- **`CloseDoor(DoorSide)`** (2026-08-05, project owner exit-ordering
  feature) — a direct call into whatever method vanilla uses to close
  a door, but for exactly ONE of the two, not both. `DoorSide` maps
  onto whichever internal identifiers vanilla uses for
  Exterior/Interior (the same settings its own "interior"/"exterior"
  atmosphere config already names, most likely).
- **`ExteriorPresenceDetected`/`InteriorPresenceDetected`** — optional,
  needs two new Presence/Motion Sensor references, same pattern as
  `PropAtmosphereMatched`'s Gas Sensor pair. No vanilla equivalent.
- **`SetWarningIndicator(Tier)`** — vanilla's Console likely has its
  own status display; decide whether this drives that, or a separate
  physical LED the way the IC10 build does (`watcher.ic10`'s
  `ColorGreen`/`ColorYellow`/`ColorRed`, itself still flagged
  unconfirmed in that build too). Lowest-stakes item on this list —
  fine to stub this out last.
- **`SetDownstreamPower(bool)`** — almost certainly no vanilla
  equivalent (see `GAP_ANALYSIS.md`'s "Power architecture" section) —
  needs a reference to the traditional Area Power Controller feeding
  the doors, Vents, and chamber Gas Sensor, set up as a new field, the
  same way the dedicated battery reference above likely needs to be.
  **Must be wired from the APC's power-source side** (project owner,
  2026-08-05: an APC only exposes its logic there, not on its
  downstream/output side) — don't look for a control hook on the
  network the APC creates downstream of itself. **Check first whether
  "Area Power Controller" and "Power Controller" are the same device**
  (a wiki redirect suggests they might be) — if so, this is the exact
  same device and `On`-field question already flagged unconfirmed for
  `ic10-airlock/watcher.ic10`'s own zone gate, not new unknowns.
- **`HasDownstreamController`** — presence check paired with the above:
  does a controllable APC actually exist on the source-side network at
  all. Same shape as `HasWakeButtons`'s presence check — likely "is the
  reference/hash null" once `SetDownstreamPower`'s real hook is found.
  Without this, `SetDownstreamPower` could get called against a device
  that isn't there; `FailsafeController` already handles that
  gracefully (Deep Idle just doesn't run), but the adapter still needs
  to detect absence rather than assume presence.
- **`MaintenanceModeEnabled`** — a Console setting toggle, no device to
  find. Same "no decompiler work needed, just a settings surface"
  category as `AllowPowerDownWhilePropped` — safe to stub as
  always-`false` until a settings UI is worth building.
- **Tier thresholds and `WakeHoldTicks`** — not `IAirlockHost` members
  at all, these live as public settable properties directly on
  `FailsafeController` (`NormalToLowThreshold`, `LowToNormalThreshold`,
  `LowToCriticalThreshold`, `CriticalToLowThreshold`, `WakeHoldTicks`).
  If a Console settings surface gets built for these, the adapter just
  writes to the `FailsafeController` instance directly when a setting
  changes — no new interface member needed, and defaults already match
  validated behavior if no settings UI exists yet.
- **`ExtendVentRelief(DoorSide)`** (2026-08-05, project owner —
  universal inline-tank relief, see `GAP_ANALYSIS.md` item 12) — needs
  a reference to that side's Active Vent (already needed for
  `ForceEvacuate()`/`HoldBothDoorsOpen()`) and a way to briefly extend
  its operation once the door opens. No live pressure read required —
  see the "Where `OnDoorOpened` attaches" section below for the more
  important open question this one depends on.

## Where `OnDoorOpened` attaches — bigger than a single Postfix

**This is a real expansion of Milestone 1.5's scope, not just another
checklist item.** Every other `IAirlockHost` member so far gets driven
by the single per-tick Postfix already planned (see "Where the Harmony
patch itself attaches" below) — `FailsafeController.ApplyTierEffects()`
runs once a tick regardless of what triggered anything. `OnDoorOpened`
is different: it has to fire on **every** door-open event, including
ones this design never initiates at all — a player pressing the native
button that comes on a powered door, or clicking Console UI, both of
which run entirely through vanilla's own untouched code (project owner,
2026-08-05: this design is meant to improve the whole airlock, not just
wrap around the parts it directly controls — see the correction at the
top of `GAP_ANALYSIS.md`).

That means Milestone 1.5 needs to find a **second** attachment point
beyond the per-tick update method: whatever vanilla method actually
opens a door (or fires when one opens), so a Harmony patch can call
`FailsafeController.OnDoorOpened(side)` from there too. Two sub-questions
worth resolving together, since the answer to one probably answers the
other:

1. Is there one shared "open this door" method both the native button
   and the Console UI already funnel through, or do they take genuinely
   separate code paths that would each need their own patch?
2. Does that method (or whatever's closest to it) already know *which*
   door — Exterior or Interior — so `DoorSide` can be passed through
   correctly, or does the adapter need to infer that some other way?

## Cross-network visibility — the question that decides if a bridge is needed at all

**Before chasing a Transmitter pair or a Re-Volt Data Diode, answer
this first:** does the patched card have C#-level access to the
vanilla instance's own Door/Vent/chamber-Gas-Sensor references —
either because the Harmony patch runs against the same instance and
those fields are reachable directly or via reflection — or does
reaching them require going through the in-game logic network the way
an IC10 script would? See `GAP_ANALYSIS.md`'s "Cross-network
visibility for the downstream side" for the full reasoning: unlike the
original two-IC10-chip design (which needed a bridge no matter what,
since separate chips can't share registers under any circumstances),
this is a genuinely open question here, not an assumed requirement.
If C#-level access already works, `ForceEvacuate()`, `UnlockDoors()`,
and `HoldBothDoorsOpen()` need no bridge at all. If it doesn't, fall
back to a Logic Transmitter pair (vanilla) or, for a Re-Volt-enhanced
variant, the Data Diode already investigated on the `revolt-mod`
branch.

## Where the Harmony patch itself attaches

Two things needed from Milestone 1.5 beyond the above:

1. **The class name** for the Advanced Airlock Circuitboard's runtime
   behavior (guessed as something like `InternalCircuitAdvancedAirlock`
   in `README.md`, unconfirmed).
2. **The per-tick update method** on that class — wherever its own
   cycling state machine runs each tick, so a Harmony `Postfix` patch
   can call `FailsafeController.UpdateTier()` +
   `.ApplyTierEffects()` right after vanilla's own logic runs, every
   tick, without needing to touch vanilla's method at all. A `Postfix`
   (not a `Prefix` or full replacement) is the right shape *if*
   vanilla's normal cycling should keep running underneath — which
   `GAP_ANALYSIS.md` says it should, for everything except the new
   Critical-tier override.

## Throttling how often the patch actually runs

Project owner's request: don't run `UpdateTier()`/`ApplyTierEffects()`
on every single game tick if the game runs many ticks per second — a
quarter-second response delay is completely unnoticeable to a player,
and skipping most invocations is real, measurable savings if the
per-tick update method this patches into runs at high frequency.

**Important correction on the mechanism, though: this should be a
skip-counter, not a "wait statement."** A literal blocking wait
(`Thread.Sleep` or similar) inside a Harmony `Postfix` would run on
Unity's main thread — the same thread the whole game simulation and
rendering run on — and would stall the entire game for that duration,
every time it fired. That's the opposite of the intended effect: real
lag, not saved processing. The correct approach is non-blocking:
count invocations and only run the real logic every Nth one, skipping
the rest for free.

```csharp
private static int ticksSinceLastCheck = 0;
private const int TicksPerCheck = /* TBD, see below */;

private static void Postfix(/* real class */ __instance)
{
    if (++ticksSinceLastCheck < TicksPerCheck) return;
    ticksSinceLastCheck = 0;

    var controller = GetOrCreateController(__instance);
    controller.UpdateTier();
    controller.ApplyTierEffects();
}
```

**`TicksPerCheck` needs Stationeers' actual simulation tick rate to set
correctly** — not found/confirmed anywhere in this project yet. Once
known, pick `TicksPerCheck` so `TicksPerCheck / tick_rate` ≈ the target
delay (a quarter second, per the request above).

**This has a real knock-on effect that has to be handled, not just a
detail:** `FailsafeController`'s `WakeHoldTicks` constant (currently
20) represents 20 *invocations*, which the IC10 build's own comments
already flag as "an unvalidated starting guess" at IC10's native
per-tick cadence. If this patch only invokes `ApplyTierEffects()` every
`TicksPerCheck` game ticks instead of every one, 20 invocations now
spans `20 * TicksPerCheck` real game ticks — a much longer real-world
hold than originally intended, growing directly with whatever
`TicksPerCheck` ends up being. **Recalibrate `WakeHoldTicks` once both
numbers are known**, so the actual held-open duration stays the
in-game-verifiable duration the IC10 build already targets, not an
accidental multiple of it.

Tier-threshold responsiveness (the 90/93/10/13 charge percentages)
isn't a concern at a quarter-second check interval — charge doesn't
meaningfully change that fast, so throttling `UpdateTier()` the same
way as `ApplyTierEffects()` should be safe. Worth a sanity check once
this is actually running, not just assumed.

## Once these are known

Write a class like:

```csharp
[HarmonyPatch(typeof(/* real class */), "/* real update method */")]
internal static class AdvancedAirlockFailsafePatch
{
    // one FailsafeController per circuit instance -- a
    // ConditionalWeakTable keyed on the patched instance is the usual
    // Harmony pattern for "attach new per-instance state to an
    // existing class," but confirm this project's chosen approach
    // once real types are known. Combine with the throttling counter
    // above -- both belong in this same Postfix.
    private static void Postfix(/* real class */ __instance)
    {
        var controller = GetOrCreateController(__instance);
        controller.UpdateTier();
        controller.ApplyTierEffects();
    }
}
```

This file doesn't exist yet — deliberately. Writing it before the real
class/method names are known would mean guessing at a `HarmonyPatch`
attribute that either doesn't compile or silently patches the wrong
thing. `FailsafeController.cs` is the part safe to write now; this
shell is Milestone 2's actual first task.
