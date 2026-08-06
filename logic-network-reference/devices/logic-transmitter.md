# Logic Transmitter

**Real class**: `Assets.Scripts.Objects.Electrical.LogicTransmitter`
(decompiled via `ilspycmd`, 2026-08-06 — this project's own IC10
airlock design depends on this device directly, see
`airlock-ic10-scripts/watcher.ic10`/`cycle.ic10`). Extends `LogicInputBase`.

**Not in `ground-truth-database.md`'s automated extraction** — its
`CanLogicRead`/`GetLogicValue`/`CanLogicWrite`/`SetLogicValue`
overrides use `if`/early-return logic, not a `switch`, which that
extraction script doesn't parse. Hand-decompiled here instead (see
`README.md`'s "Known open question" note, now resolved).

## `Mode` — Active (1) vs. Passive (0)

`IsActiveTransmitter => Mode == 1`. Confirms the project's existing
IC10-side understanding (`s Transmitter Mode 1` / `s Receiver Mode 0`)
exactly. Only Active-mode units get added to a global registry,
`Transmitters.AllTransmitters` — see "Pairing," below, for why this
matters.

## `Setting` — a real property, not just a LogicType case

`LogicTransmitter` overrides a `Setting` **property** (not just the
`LogicType.Setting` case) — this is the actual field
`s Transmitter Setting <value>` / `l r0 Transmitter Setting` write and
read:

```csharp
public override double Setting
{
    get => IsActiveTransmitter ? base.Setting : (CurrentDevice?.GetLogicValue(LogicType.Setting) ?? 0.0);
    set { if (IsActiveTransmitter) base.Setting = value; else CurrentDevice?.SetLogicValue(LogicType.Setting, value); }
}
```

**Active mode**: `Setting` is a genuine local field — reads/writes
affect only this unit's own stored value, exactly what a Watcher chip
writing its packed Tier+buttons number expects.

**Passive mode**: `Setting` isn't stored locally at all — every read
forwards live to whatever `CurrentDevice` currently reports, and every
write forwards live to it too. There's no local caching/buffering on
the Passive side beyond what `CurrentDevice` itself holds.

## Pairing — what `CurrentDevice` actually is

The project's existing docs describe pairing as "a physical dial on
the Passive unit, adjusted until it shows the Active unit's name" —
confirmed accurate, now with the mechanism behind it: `CurrentDevice`
(typed `ITransmitable`) is set by player interaction cycling through
`Logicable.GetNextReadOrWritable(this, CurrentDevice,
Transmitters.AllTransmitters, interaction.AltKey)` — a next/previous
index walk through the global `Transmitters.AllTransmitters` list,
`AltKey` presumably reversing direction. Since only Active-mode
Transmitters ever get added to that list (`SwitchMode`'s `Active`
case), a Passive unit's dial can only ever land on another Active
Transmitter — never another Passive one, and never anything that isn't
a Logic Transmitter at all. This matches "dial shows a name, cycle
until it's the right one" as the real in-game experience of an
index-based selection under the hood, not a contradiction of it.

The pairing itself is persisted (`CurrentConnectedId` in save data,
resolved back via `Thing.Find<ITransmitable>(_savedId)` on load) and
survives power loss/reload — matches "a one-time manual pairing step,"
not something that needs redoing.

## New finding: a Passive unit needs its own power to relay anything

**Not previously confirmed anywhere in this project — worth noting
explicitly.** Every forwarding call in the Passive branch
(`CanLogicRead`, `GetLogicValue`, `GetLogicValue(LogicSlotType, int)`,
`SetLogicValue`) is gated on `!Powered` returning early
(`false`/`0.0`/no-op) *before* ever touching `CurrentDevice` — a
Passive Logic Transmitter with no power of its own reads as
completely blank/unresponsive, **regardless of whether its paired
Active unit is broadcasting fine**. It is not a passive wireless
antenna that works unpowered; both ends need power to actually
function on their end of the link. The Active side's own `Setting`
read/write, by contrast, has no such `Powered` check in this override
(though the Active unit obviously still needs power to run whatever
script is writing to it in the first place, so this asymmetry doesn't
change anything practical).

**Why this doesn't reveal a bug in this project's own design**: the
Cycle chip (which houses the "Receiver" — a Passive Transmitter) is
only ever powered when Watcher's zone gate is on, and Cycle has no
reason to read `Setting` while unpowered anyway (it isn't running).
The design already happens to match this constraint; this finding
confirms an assumption that was previously just "probably fine," not a
problem to fix.

## Beyond `Setting` — a Passive unit mirrors everything, not just one field

Worth knowing even though this project only currently uses `Setting`:
the Passive-mode forwarding in `CanLogicRead`/`GetLogicValue`/
`CanLogicWrite`/`SetLogicValue` applies to **any** `LogicType`, not
specifically `Setting` — a Passive Transmitter genuinely mirrors its
paired Active unit's entire logic surface (its in-game tooltip
literally says "Mirroring `<DisplayName>`"). A future design could read
other LogicTypes through a Receiver the same way, not just the packed
`Setting` number this project's IC10 side relies on.

## `IsOperable` / `Error`

A Passive unit with no `CurrentDevice` set (never paired, or its
target got deleted) reports `Error = 1` automatically
(`OnServer.Interact(base.InteractError, 1)` inside `IsOperable`'s
getter) and `IsOperable` returns `false`. An Active unit is always
operable regardless of pairing (it doesn't need one). Worth a
troubleshooting-guide callout: an Error state on a Receiver most likely
means the pairing was never done or got broken, not a power/wiring
issue.
