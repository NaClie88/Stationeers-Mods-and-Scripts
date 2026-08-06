# Door

**Real class**: `Assets.Scripts.Objects.Structures.Door` (decompiled
via `ilspycmd`, 2026-08-05/06, during the airlock mod's Milestone 2
work — see `airlock-card-mod/PATCH_PLAN.md` for the full investigation
this page summarizes).

See [`base-behavior.md`](../base-behavior.md) — `Door` does not
override `CanLogicRead`/`GetLogicValue`/`CanLogicWrite`/
`SetLogicValue` at all. Its `Open`/`Lock` LogicTypes are exactly the
shared base behavior: `IsOpen ? 1 : 0` / `IsLocked ? 1 : 0` for reads,
`OnServer.Interact(InteractOpen, state)` / `OnServer.Interact(InteractLock, state)`
for writes. Nothing device-specific to add on the LogicType surface
itself.

## Why this page exists anyway — the real mechanism behind "Open"

What Door *does* add is worth documenting because it's where this
project first learned that `Thing.IsOpen`'s own **property setter**
is not actually part of the real state-change path, even though
reading its source made that setter look load-bearing. The real chain,
confirmed by testing in-game (not just reading source):

```
OnServer.Interact(door, InteractableType.Open, state)
  -> Interactable.Interact(state, skipAnimation)
     -> Interactable.State = state   (property setter)
        -> Thing.OnInteractableStateChanged(interactable, newState, oldState)
           -> SetIntegerSafe(interactable.PropertyId, newState)   // drives the Animator
```

`Thing.IsOpen`'s own setter (`set { if (HasOpenState) { ...
SetIntegerSafe(...) or InteractOpen.State = ...; _isOpen = value; } }`)
is a **separate, effectively-dead code path** for animator-driven
doors in practice — nothing in the real interaction flow (native door
button, Console UI, or `AdvancedAirlockControl`'s own automated
cycling, all confirmed to go through `OnServer.Interact`) ever assigns
to it. `IsOpen`'s **getter**, on the other hand, is accurate — it
reads live from `BaseAnimator.GetInteger(Interactable.OpenState)` for
animator-driven Doors, which the chain above does correctly keep
updated.

**Practical implication for modding**: if you want to react to a door
(or anything else with `HasOpenState`) opening or closing via Harmony,
patch `Thing.OnInteractableStateChanged` and filter on
`interactable.Action == InteractableType.Open`, **not**
`Thing.IsOpen`'s setter — the setter patch compiles fine and produces
no error, it simply never fires. This generalizes beyond `Open`: the
same `OnInteractableStateChanged` path is the real mechanism behind
`On`, `Lock`, `Mode`, `Color`, and `Activate` too (see
`base-behavior.md`'s write section) — worth checking any future
"react to LogicType X changing" patch against this same pattern before
assuming a property setter is the right target.

## `HasOpenState` / animator vs. non-animator doors

`Thing.IsOpen`'s getter branches on whether the object has a
`BaseAnimator`: animator-driven objects read live from the Animator's
integer parameter; non-animator objects with `HasOpenState` instead
read `InteractOpen.State == 1` directly. Not yet confirmed whether any
in-game Door variant actually lacks a `BaseAnimator` (all doors tested
so far in this project have animated open/close), but worth checking
before assuming every Door behaves identically if a future patch needs
to be airtight.
