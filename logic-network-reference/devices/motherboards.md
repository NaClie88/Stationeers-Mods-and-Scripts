# Circuitboards / Motherboards — a different system entirely

**Real classes**: `Assets.Scripts.Objects.Items.Motherboard`,
`Assets.Scripts.Objects.Items.Circuitboard` (extends `Motherboard`),
and everything under `Assets.Scripts.Objects.Motherboards.*`
(`AdvancedAirlockControl`, `AirlockControl`, `AirlockControlBase`,
`LogicMotherboard`, etc.) — decompiled via `ilspycmd`, 2026-08-05/06,
during the airlock mod's Milestone 1.5/2 work.

**Not covered by [`base-behavior.md`](../base-behavior.md) at all** —
confirmed by decompiling `Motherboard.cs` and `Circuitboard.cs` and
finding zero overrides of `CanLogicRead`, `GetLogicValue`,
`CanLogicWrite`, or `SetLogicValue` anywhere in either class. This
isn't an oversight to fill in later — it's a real structural fact
worth documenting explicitly, since it's easy to assume (incorrectly)
that a circuit card exposes IC10-readable LogicTypes the same way a
standalone device does.

## Why: circuit cards aren't networked devices themselves

A Circuitboard/Motherboard is an *item* that gets inserted into a
Computer/Console structure — it's the Computer that's the actual
networked `Device` (if it even is one; not yet independently confirmed
here). The circuit card's own behavior (the Advanced Airlock's
pressurize/depressurize state machine, its Cycle/Skip buttons, its
Internal/External pressure settings) is driven entirely through a
**separate command dispatch system**:

```
Motherboard.UseComputer(command, masterNetId, thisNetId, referenceInt, sendToAll)
  -> routes to MotherboardCommand(int command, Thing reference, int referenceInt, string text)
     -> individual command handlers (SetFlag, ButtonCycleAirlock, etc.)
```

`ButtonCommands` (the `command` parameter's enum,
`Assets.Scripts.Objects.Items.ButtonCommands`) is the circuit-card
equivalent of `LogicType` for this system — `Toggle`, `AddDevice`,
`RemoveDevice`, `SetFlag`, `OpenAll`, `CloseAll`, `Refresh`,
`AddSlave`, `RemoveSlave`, `AddSlaveDirect`, `PowerOn`, `PowerOff`,
`Mode0`, `Mode1`, `IndexUp`, `IndexDown`, `SetFilter`, `ClearFilter`,
`Special1`, `Special2`, `FlashCircuit`. This is triggered by Console
UI button clicks (`OnClick` handlers calling `Motherboard.UseComputer`
directly, see `AdvancedAirlockControl.ButtonCycleAirlock()` in
`airlock-card-mod/PATCH_PLAN.md`'s trace of vanilla's Skip mechanism),
not by IC10 `l`/`s` instructions against the card itself.

## Practical implication

If a script or a Harmony patch needs to read or influence a
Circuitboard's own state (Tier, cycle phase, pressure settings), the
`LogicType` system this reference otherwise documents is the wrong
tool — go through this project's own confirmed findings instead
(`airlock-card-mod/PATCH_PLAN.md` for the Advanced Airlock
Circuitboard specifically: `AirlockControlBase.OnThreadUpdate` for
per-tick state, `AdvancedAirlockControl.AirlockControlState` /
`SetFlag` for triggering a phase transition, `ButtonCommands.SetFlag`
as the opcode). Whether *other* circuit cards in the game (not just
the Advanced Airlock) follow this same `ButtonCommands`/
`MotherboardCommand` pattern is a reasonable assumption (same base
classes) but not independently re-verified per-card yet.
