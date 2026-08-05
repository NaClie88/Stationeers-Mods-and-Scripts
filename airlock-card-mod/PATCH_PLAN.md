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
  device read. `ButtonCHeld` is the one that matters most (the trapped
  -player override); `ButtonEHeld`/`ButtonIHeld` only feed the
  downstream-power wake logic (see below) — if vanilla has no concept
  of a chamber-interior button at all, that one specifically is new
  wiring, not a hook into existing behavior.
- **`VanillaCycleRequested`** — **not optional, has to be found.**
  Whatever vanilla's real cycle-trigger path is (Console UI click
  handler most likely), this needs to surface as a boolean the patch
  can read every tick. This is what keeps Deep Idle from stranding a
  Console-only player who never wires the optional hardware buttons —
  see `GAP_ANALYSIS.md`'s "Graceful degradation" section for the bug
  this fixes. If vanilla's trigger is a one-shot event/callback rather
  than a persistent flag, the adapter will need to latch it into a
  boolean that stays true for at least one tick after the event fires.
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
- **`ForceEvacuateAndUnlock()`** — the important one. Find whatever
  method vanilla's own Critical-adjacent logic (if any exists) or its
  normal evacuate-cycle method looks like, so this can call *into* it
  rather than reimplementing vent control. If vanilla's evacuate logic
  is one method that also happens to unlock doors at the end, this
  might be a single call; if door-lock and vent-evacuate are separate
  vanilla methods, this needs to sequence both, matching
  `cycle.ic10`'s `tierCrit` block (lines 108-119): close both doors →
  run Vent evacuate to `TargetExt` → unlock both doors once the
  chamber's Gas Sensor confirms it.
- **`HoldBothDoorsOpen()`** — likely a direct call into whatever method
  vanilla uses to open a door (called twice, once per door), skipping
  the normal auto-close timer. Confirm whether vanilla's door-open call
  has an optional "don't auto-close" parameter, or whether this needs
  a small patch of its own to suppress the auto-close while Propped-Open
  is active.
- **`SetWarningIndicator(Tier)`** — vanilla's Console likely has its
  own status display; decide whether this drives that, or a separate
  physical LED the way the IC10 build does (`watcher.ic10`'s
  `ColorGreen`/`ColorYellow`/`ColorRed`, itself still flagged
  unconfirmed in that build too). Lowest-stakes item on this list —
  fine to stub this out last.
- **`SetDownstreamPower(bool)`** — almost certainly no vanilla
  equivalent (see `GAP_ANALYSIS.md`'s "Power architecture" section) —
  needs a reference to a downstream APC/Power Controller feeding the
  doors and Vent, set up as a new field, the same way the dedicated
  battery reference above likely needs to be. **Check first whether
  "Area Power Controller" and "Power Controller" are the same device**
  (a wiki redirect suggests they might be) — if so, this is the exact
  same device and `On`-field question already flagged unconfirmed for
  `ic10-airlock/watcher.ic10`'s own zone gate, not new unknowns.

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
    // once real types are known.
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
