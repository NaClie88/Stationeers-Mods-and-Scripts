# Base behavior — shared by (almost) every device

**Source**: `Assets.Scripts.Objects.DynamicThing` (decompiled via
`ilspycmd`, `Assembly-CSharp.dll`, 2026-08-06). `DynamicThing` extends
`Thing` — the root of nearly everything placeable/networked in the
game — and is itself the base for `Structure`, `Device`, `Item`, and
everything under them. Every device this reference documents inherits
this unless noted otherwise. **This is why so many devices expose
identical-looking LogicTypes** — it's one shared implementation, not
independently-duplicated per-device code.

## Read: `CanLogicRead(LogicType)` / `GetLogicValue(LogicType)`

| LogicType | Readable when... | Value returned |
|---|---|---|
| `ReferenceId` | always | the object's own reference ID |
| `PrefabHash` | always | the object's own prefab hash |
| `Color` | `HasColorState` | `ColorState` |
| `Activate` | `HasActivateState` | `Activate` |
| `Power` | `HasPowerState` | `Powered ? 1 : 0` |
| `Open` | `HasOpenState` | `IsOpen ? 1 : 0` |
| `Mode` | `HasModeState` | `Mode` |
| `Error` | `HasErrorState` | `Error` |
| `Lock` | `HasLockState` | `IsLocked ? 1 : 0` |
| `On` | `HasOnOffState` | `OnOff ? 1 : 0` |
| `Pressure` | `HasReadableAtmosphere` | internal atmosphere's gas+liquid pressure |
| `Temperature` | `HasReadableAtmosphere` | internal atmosphere's temperature |
| `Combustion` | `HasReadableAtmosphere` | `1` if internal atmosphere is inflamed, else `0` |
| `TotalMoles` | `HasReadableAtmosphere` | internal atmosphere's total moles |
| `RatioOxygen`, `RatioCarbonDioxide`, `RatioNitrogen`, `RatioPollutant`, `RatioMethane`, `RatioWater`, `RatioNitrousOxide`, `RatioHydrogen`, `RatioPollutedWater`, `RatioHydrazine`, `RatioHelium`, `RatioSilanol`, `RatioHydrochloricAcid`, `RatioOzone`, and the `RatioLiquidX`/`RatioSteam` variants of most of these | `HasReadableAtmosphere` | per-gas ratio of the internal atmosphere (`GasRatio(logicType)`) |
| `Reagents` | `HasReadableReagentMixture` | total reagents in the reagent mixture |
| anything else | — | not readable (`CanLogicRead` false, `GetLogicValue` falls through to `0.0`) |

**Note on the `RatioX` family**: this is the *general-purpose gas
composition* reading (how much of the internal atmosphere is oxygen,
CO2, etc.) — do not confuse this with a device-specific "charge ratio"
or "fill ratio" concept some devices add themselves (e.g. Power
Controller's `Ratio`, see `devices/power-controller.md` — that's a
*different* LogicType entirely, not part of this shared list, added by
that specific device's own override).

## Write: `CanLogicWrite(LogicType)` / `SetLogicValue(LogicType, double)`

Only six LogicTypes are writable at this shared base level, gated by
the same `Has*State` flags as their read-side counterparts above:

| LogicType | Writable when... | What writing does |
|---|---|---|
| `Color` | `HasColorState` | `OnServer.Interact(InteractColor, clampedColorIndex)` |
| `Activate` | `HasActivateState` | `OnServer.Interact(InteractActivate, state)` |
| `Open` | `HasOpenState` | `OnServer.Interact(InteractOpen, state)` |
| `Mode` | `HasModeState` | `OnServer.Interact(InteractMode, clampedState)` |
| `Lock` | `HasLockState` | `OnServer.Interact(InteractLock, state)` |
| `On` | `HasOnOffState` | `OnServer.Interact(InteractOnOff, state)` |

(`state` here is the written value clamped to `0`/`1` via
`Mathf.Clamp`, i.e. these are all effectively booleans at this level
even though the LogicType system passes a `double`.)

**Every one of these writes funnels through `OnServer.Interact` —
this is the single generic write mechanism nearly the whole game
uses**, not just for these six LogicTypes but for essentially any
player- or script-driven state change (see
`airlock-card-mod/PATCH_PLAN.md`'s "Where `OnDoorOpened` attaches" for
the full trace this reference is built on: `OnServer.Interact` →
`Interactable.Interact` → `Interactable.State`'s setter →
`Thing.OnInteractableStateChanged`, which is what actually drives a
device's Animator/visual state). This matters for anyone patching this
game with Harmony, not just for IC10 scripting — a Postfix on
`Thing.OnInteractableStateChanged`, filtered by `Interactable.Action`,
catches *any* of these six state changes on *any* device, from *any*
trigger (script write, player click, another mod), in one place.

## `LogicSlotType` — reading into an inventory slot from outside

Separate, sibling methods (`CanLogicRead(LogicSlotType, int slotId)` /
`GetLogicValue(LogicSlotType, int slotId)`) handle "what's sitting in
slot N of this device" reads — e.g. a printer's output slot, a
battery charger's inserted cell. Only present for devices where
`HasAnySlots` is true and `slotId` is in range. Fields include
`Occupied`, `OccupantHash`, `Quantity`, `Damage`, `Class`, `PrefabHash`,
`ReferenceId` (of whatever's in that slot, not the parent device
itself). Not fully cataloged here yet — worth its own pass once a
device with interesting slot-based reads is documented (Power
Controller's inserted `BatteryCell` is one candidate, see
`devices/power-controller.md`).

## What device-specific pages actually need to document

Given the above, a `devices/*.md` entry should **not** re-list `On`,
`Open`, `Lock`, `Mode`, `Color`, `Activate`, `Power`, `Error`, or the
atmosphere/gas readings unless that specific device does something
unusual with them (e.g. overrides the write behavior, or the
`Has*State` flag has its own non-trivial logic). It should focus on:
what that device *adds* beyond this shared set, and any place its
behavior *deviates* from what's documented here.
