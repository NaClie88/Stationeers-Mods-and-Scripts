# Patch Plan — Wiring `FailsafeController` into the Real Vanilla Class

`src/FailsafeController.cs` is done and game-independent. This doc is
the checklist for Milestone 1.5: what to find in `Assembly-CSharp.dll`
(dnSpy/ILSpy) so a thin Harmony `IAirlockHost` adapter can wire it up.
Each `IAirlockHost` member below needs one real answer.

**Real class names, CONFIRMED (2026-08-05, decompiled directly via
`ilspycmd` — full source pulled, not just a class list):**
`Assets.Scripts.Objects.Motherboards.AdvancedAirlockControl` extends
`AirlockControlBase` extends `Assets.Scripts.Objects.Items.Circuitboard`
extends `Motherboard`. The older non-Advanced airlock is a sibling,
`Assets.Scripts.Objects.Motherboards.AirlockControl`, sharing the same
`AirlockControlBase`. State lives in an `AdvancedAirlockState` enum
(`Disabled`/`PressurizingInternal`/`PressurizedInternal`/
`DepressurizingInternal`/`PressurizingExternal`/`PressurizedExternal`/
`DepressurizingExternal`) exposed as `AirlockControlState`.

## What to find, per `IAirlockHost` member

- **`DedicatedBatteryChargeRatio`** — **CONFIRMED: no vanilla
  equivalent, needs a new field.** `AdvancedAirlockControl`,
  `AirlockControlBase`, and `AirlockControl` were all decompiled in
  full — zero references to battery, charge, or any Power Controller
  type anywhere in the three classes. The circuit only tracks Doors,
  Gas Sensors, `IPoweredVent`s, `WallLight`s (as warning lights), and
  Speakers; nothing about its own power source. This needs its own new
  field, a reference to a specific Power Controller, set some other
  way (a new Console setting, most likely) — not a hook into anything
  that already exists.
- **`ButtonEHeld`/`ButtonIHeld`/`ButtonCHeld`** — **CONFIRMED: no
  vanilla equivalent, and the likely-looking hook is a dead end.**
  Vanilla's button input is entirely Console UI `Button` components
  wired to C# click handlers — `ButtonCycleAirlock()`,
  `ButtonPressureInternal()`, `ButtonPressureExternal()`,
  `ButtonEmergencyOverride()` — not hashed device reads the way this
  project's IC10 side works. **`ButtonEmergencyOverride()` looked like
  the obvious vanilla analog for Button C by name, but decompiling both
  overrides shows it's a genuine no-op in both classes** —
  `AdvancedAirlockControl` doesn't override it at all (falls through to
  the empty base), and `AirlockControl`'s own override is just
  `base.ButtonEmergencyOverride();` with nothing else. It's a vestigial
  hook the shipped game doesn't wire to anything — don't build on its
  name implying real behavior. This makes Milestone 0.5's finding more
  load-bearing, not less: reusing vanilla's Skip (below) really is the
  only working override path already in the game, so **stay on the
  "`ButtonCHeld` is a fallback, not the primary plan" position** — see
  `README.md` Milestone 0.5 and `GAP_ANALYSIS.md`'s "Reusing vanilla's
  Skip instead of custom Button C hardware." `ButtonEHeld`/`ButtonIHeld`
  (chamber-interior wake buttons) have no vanilla concept at all either
  — confirmed new wiring regardless, same as originally flagged.
- **HOW vanilla's own Skip/Cancel actually works, CONFIRMED** — useful
  context for `ForceEvacuate()` below and for closing Milestone 0.5's
  remaining open question. `ButtonCycleAirlock()` (bound to the
  Console's main Cycle/Cancel button) calls
  `Motherboard.UseComputer((int)ButtonCommands.SetFlag /* = 3 */, ...,
  newStateInt, sendToAll: true)`, which round-trips into
  `SetFlag(int page)` → sets `AirlockControlState`. The
  `AirlockControlState` property setter has a `switch` that starts a
  **new** `Pressurizing()`/`Depressurizing()` `UniTask` for whatever
  state was just assigned. There's no explicit cancellation of the old
  task — it's a plain async loop (`while (... && AirlockControlState ==
  transitState ...) { await UniTask.Delay(100, ...); }`) that simply
  stops looping on its next 100 ms poll once the state no longer
  matches, since the setter already changed it out from under it. So
  "Skip" is really "request the next state and let the old task notice
  and exit" — not a cancellation API. This is the real mechanism a
  Harmony patch would be reusing if `ForceEvacuate()` calls into the
  same state-assignment path rather than reimplementing vent control.
- **`HasWakeButtons`** — the actual gate for whether Deep Idle runs at
  all (see `GAP_ANALYSIS.md`'s "Graceful degradation" section). Just a
  presence check: does this circuit have a physical E or I button
  wired at all — confirmed above that this is entirely new wiring, so
  this becomes "is the new reference/hash null" on whatever gets built
  for `ButtonEHeld`/`ButtonIHeld`.
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
- **`SetDownstreamPower(bool)`** — **CONFIRMED: no vanilla
  equivalent**, same decompile pass as `DedicatedBatteryChargeRatio`
  above — zero Power Controller/APC references anywhere in the three
  airlock classes. Needs a reference to the traditional Area Power
  Controller feeding the doors, Vents, and chamber Gas Sensor, set up
  as a new field. **Must be wired from the APC's power-source side**
  (project owner, 2026-08-05: an APC only exposes its logic there, not
  on its downstream/output side) — don't look for a control hook on
  the network the APC creates downstream of itself. **Check first
  whether "Area Power Controller" and "Power Controller" are the same
  device** (a wiki redirect suggests they might be) — if so, this is
  the exact same device and `On`-field question already flagged
  unconfirmed for `ic10-airlock/watcher.ic10`'s own zone gate, not new
  unknowns.
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

**CONFIRMED (2026-08-05, decompiled `Assets.Scripts.Objects.Thing`
directly).** There is one single, shared attachment point, and it
answers both sub-questions at once:

1. **One shared method — yes.** `IsOpen` isn't a Door-specific field,
   it's a `virtual bool` property declared on the root `Thing` class
   itself (every door, and anything else with an open state, inherits
   it). The **setter** is where a physical state change actually
   happens: `set { if (HasOpenState) { if (BaseAnimator) SetIntegerSafe(...)
   else InteractOpen.State = ...; _isOpen = value; } }`. Every code
   path this project cares about — `AdvancedAirlockControl` calling
   `OnServer.Interact(door.InteractOpen, 1)` for its own automated
   cycling, the Console UI, and (near-certainly, same shared
   `Interactable`/`OnServer.Interact` dispatch pattern used everywhere
   else in this codebase for Vents/Lights/Speakers) the door's own
   native physical button — all funnel through this one setter. A
   Harmony `Postfix` on `Thing.IsOpen`'s setter, typed `Door __instance`,
   `bool value`, fires for every door-open event regardless of what
   triggered it.
2. **Which door — yes, trivially.** `__instance` in that Postfix *is*
   the specific `Door` object. `AdvancedAirlockControl` already holds
   `ExteriorAirlock`/`InteriorAirlock` references, so the adapter just
   compares `__instance == controller.ExteriorAirlock` vs.
   `== controller.InteriorAirlock` to resolve `DoorSide` — no
   inference needed.

**One real wrinkle to design around:** the setter runs on *every*
assignment, including redundant same-value writes (e.g. something
setting `IsOpen = true` on a door that's already open) — so
`OnDoorOpened` shouldn't fire blindly on every Postfix call. Either
check `value && !wasOpenBefore` (a `Prefix` capturing the old value,
compared in the `Postfix`), or let `FailsafeController.OnDoorOpened`
itself be idempotent/cheap enough that redundant calls are harmless —
worth deciding explicitly in Milestone 2 rather than assuming either.

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

1. **The class name — CONFIRMED**:
   `Assets.Scripts.Objects.Motherboards.AdvancedAirlockControl` (see
   the top of this doc for the full inheritance chain).
2. **The per-tick update method — CONFIRMED, with a wrinkle worth
   attending to.** `AirlockControlBase` (the shared base class)
   overrides two per-tick hooks, and they are **not** interchangeable:
   - `OnThreadUpdate()` — recomputes `_pressure` from the wired Gas
     Sensors every call, no visibility check. This one keeps running
     even when nobody's looking at the Console.
   - `UpdateEachFrame()` — only touches UI (slider/text/color lerps),
     and **returns immediately if `IsOccluded ||
     !PressureText.gameObject.activeInHierarchy`** — i.e. it stops
     running whenever the Console isn't actually on-screen.

   **`OnThreadUpdate()` is the correct Postfix target, not
   `UpdateEachFrame()`.** A failsafe controller that only ran its Tier
   checks while a player happened to be looking at the specific
   Console would silently stop monitoring the instant they walked
   away or the object got occluded — exactly the kind of failure this
   whole project exists to prevent. `OnThreadUpdate()` has no such
   gate. **Still unconfirmed:** the actual call frequency/thread of
   `OnThreadUpdate()` — it's declared further up the hierarchy (not on
   `Circuitboard` itself; tracing it needs `Motherboard`/`Thing`'s own
   base, not yet decompiled) — needed before `TicksPerCheck` below can
   be set correctly. A `Postfix` (not a `Prefix` or full replacement)
   remains the right shape, since vanilla's normal cycling should keep
   running underneath per `GAP_ANALYSIS.md`.

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

private static void Postfix(AdvancedAirlockControl __instance)
{
    if (++ticksSinceLastCheck < TicksPerCheck) return;
    ticksSinceLastCheck = 0;

    var controller = GetOrCreateController(__instance);
    controller.UpdateTier();
    controller.ApplyTierEffects();
}
```

**`TicksPerCheck` needs Stationeers' actual `OnThreadUpdate()` call
rate to set correctly — traced as far as static decompilation can go,
and it turns out to be a dead end there, not just "not found yet."**
`OnThreadUpdate()` runs on a dedicated background thread owned by
`OcclusionManager : ThreadedManager`. `ThreadedManager`'s loop is
`while (running) { ThreadedWork(); Thread.Sleep(TickSpeed); }`, where
`TickSpeed` is a **public field defaulting to `1` (ms) in code, but
serialized per-instance in Unity scene/prefab data** — the kind of
value the Unity Editor sets on a component instance, which lives
outside `Assembly-CSharp.dll` entirely and isn't visible to a C#
decompiler. (`OcclusionManager` also declares `SERVER_TICK_MS = 100`/
`CLIENT_TICK_MS = 1000`/`ALL_TICK_MS = 1000` constants that looked
promising by name, but grepping the full decompiled source turns up
zero usages anywhere — dead/vestigial constants, not the real answer;
worth flagging so a future pass doesn't reuse them by mistake.)
**Recommended path once Milestone 2's patch exists**: log a timestamp
delta on the first several real Postfix calls in-game and measure the
actual interval directly, rather than continuing to chase this
statically — it's a five-minute empirical check against something
static analysis genuinely cannot answer. Once known, pick
`TicksPerCheck` so `TicksPerCheck / tick_rate` ≈ the target delay (a
quarter second, per the request above).

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

## Written (2026-08-05) — and a real Harmony gotcha hit and fixed in-game

`airlock-card-mod/AirlockCardMod/Patches/AdvancedAirlockFailsafePatch.cs`
exists now. First version patched `typeof(AdvancedAirlockControl)`
directly, matching the naive reading of "the class name is
`AdvancedAirlockControl`" — it built clean but threw in-game:

```
HarmonyLib.HarmonyException: Patching exception in method null --->
System.ArgumentException: Undefined target method for patch method
static void AirlockCardMod.Patches.AdvancedAirlockFailsafePatch::Postfix(...)
```

**Root cause, confirmed:** `AdvancedAirlockControl` never overrides
`OnThreadUpdate()` itself — it inherits `AirlockControlBase`'s
override untouched (this was already noted earlier in this doc, just
not connected to what it means for the `[HarmonyPatch]` attribute
specifically). Harmony's `AccessTools.Method(typeof(X), "Name")`
only finds a method if `X` (or something `X` overrides itself)
actually has a compiled IL body for that name — an inherited-but-not-
overridden virtual method has no such body on the more specific type,
so the lookup returns nothing to patch.

**Fix:** patch `typeof(AirlockControlBase)` instead (the type that
actually declares the method), and filter to `AdvancedAirlockControl`
manually inside the `Postfix` body:

```csharp
[HarmonyPatch(typeof(AirlockControlBase), nameof(AirlockControlBase.OnThreadUpdate))]
internal static class AdvancedAirlockFailsafePatch
{
    private static void Postfix(AirlockControlBase __instance)
    {
        if (!(__instance is AdvancedAirlockControl advanced)) return;
        // ... GetOrCreateController(advanced), UpdateTier(), ApplyTierEffects()
    }
}
```

**General rule worth remembering for every future patch on this
project, not just this one method**: `[HarmonyPatch(typeof(X), ...)]`
needs `X` to be the type that *actually declares* the compiled method
body — for any virtual method that `AdvancedAirlockControl` inherits
without overriding, that means patching the declaring base type and
doing the `is AdvancedAirlockControl` type check by hand, exactly like
this fix. Doesn't matter for methods `AdvancedAirlockControl` *does*
override itself (e.g. `SetFlag`, `ButtonCycleAirlock`,
`RefreshScreen`) — only for inherited-as-is ones like this.

Fix built clean and reinstalled (2026-08-05); **not yet re-verified
in-game** — waiting on a retest to confirm Harmony patches apply
without exception this time. See `README.md`'s Milestone 2 section
for the log lines that would confirm it.
