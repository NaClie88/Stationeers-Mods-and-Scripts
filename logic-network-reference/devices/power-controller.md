# Power Controller / Area Power Controller

**Real class**: `Assets.Scripts.Objects.Electrical.AreaPowerControl`
(decompiled via `ilspycmd`, 2026-08-05/06). **In-game names: "Power
Controller" and "Area Power Controller" are the same device** —
confirmed by there being exactly one class in the entire decompiled
`Assembly-CSharp.dll` matching either name. Extends
`ElectricalInputOutput`.

See [`base-behavior.md`](../base-behavior.md) for the shared
`On`/`Open`/`Lock`/`Mode`/etc. set every device has — not repeated
here. This device has its own `BatterySlot` (index 0) holding a
`BatteryCell` item, and adds these LogicTypes on top of the shared
base:

## Read: `GetLogicValue` additions

| LogicType | Value | Notes |
|---|---|---|
| `Charge` | `AvailablePower` = `InputNetwork.PotentialLoad + Battery.PowerStored` (0 if no battery inserted) | **Not the battery's own charge alone** — includes whatever the input network is currently able to supply. See "The Charge/Ratio gotcha" below. |
| `Maximum` | `Battery.PowerMaximum` (0 if no battery inserted) | The inserted `BatteryCell`'s max capacity. |
| `Ratio` | `Battery.PowerStored / Battery.PowerMaximum` (0 if no battery inserted) | **This is the clean 0-1 battery-charge fraction** — the one most scripts actually want when they say "battery charge percentage." |
| `PowerPotential` | `PotentialLoad` (inherited electrical-network property) | How much power could flow, network-wide. |
| `PowerActual` | `CurrentLoad` (inherited electrical-network property) | How much power is actually flowing right now. |

`CanLogicRead` confirms `Charge`, and everything in the ordinal range
`Maximum`(23) through `Mode`(3)-offset — i.e. `Maximum` and `Ratio`
(24) both — as legally readable. Verified against the real `LogicType`
enum ordinals, not assumed from the switch statement alone.

## The Charge/Ratio gotcha — why this page exists

`Charge` sounds like "how charged is the battery," and most
community-sourced scripts (this project included, in
`ic10-airlock/watcher.ic10` — see the correction in
`ic10_airlock_code_notes.md`'s Watcher section) read it and divide by
`Maximum` expecting a clean percentage. **That's wrong whenever the
Power Controller has any live input power flowing** (a connected
solar panel, grid power, anything) — `Charge` adds that on top of the
battery's own stored energy, inflating the "percentage" above the
battery's true state of charge. **`Ratio` is the LogicType that
actually gives the clean 0-1 battery-charge fraction directly** — no
division against `Maximum` needed, and it's confirmed to exist and be
legally readable on this exact device (earlier community-sourced
research for this project, cited in `SOURCES.md`, couldn't confirm
`Ratio` was tied to Power Controller specifically — direct
decompilation resolves that ambiguity).

**If updating the Community Wiki from this reference**: the wiki's
Power Controller page (as of this project's research) documents
`Charge`/`Maximum` but doesn't call out this distinction clearly —
worth adding an explicit note that `Charge` includes live input power,
and `Ratio` is the field for a clean charge percentage.

## Write

No additional writable LogicTypes beyond the shared base — `Charge`,
`Maximum`, `Ratio`, `PowerPotential`, `PowerActual` are all read-only
(not present in the device's `SetLogicValue`, and not part of the
shared base's writable six either).

## Output-gating LogicType — resolved 2026-08-06

`On` gates the Power Controller's own downstream output/cutoff
switching. Decompilation alone narrowed this but couldn't close it:
`AreaPowerControl` has no `SetLogicValue`/`CanLogicRead`/
`CanLogicWrite` override at all (see "Write" above), so `On`'s write
path is unmodified `base-behavior.md` (`HasOnOffState` →
`OnServer.Interact(InteractOnOff, state)`) — meaning `On` was already
the only candidate, since nothing else touches the write path. What a
LogicType-accessor scan can't answer is whether toggling that state
*functionally* cuts downstream power rather than something narrower —
that's internal simulation behavior, not part of this reference's
scope. Closed by project owner's direct design knowledge instead: the
Power Controller's explicit in-game purpose is a battery buffer/charge
circuit for everything downstream of it *and* a power cutoff switch —
confirmed directly, no further decompilation or in-game test needed.

## Not yet checked

- `LogicSlotType` reads into `BatterySlot` (index 0) — would return
  info about the inserted `BatteryCell` itself (occupied/hash/
  quantity/etc. per `base-behavior.md`'s `LogicSlotType` section), not
  yet cross-referenced against `BatteryCell`'s own fields.
