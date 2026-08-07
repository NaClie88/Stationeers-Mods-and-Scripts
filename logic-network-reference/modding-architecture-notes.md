# Modding Architecture Notes — hard-won lessons for coding Stationeers mods

Not LogicType read/write data (see `base-behavior.md`,
`ground-truth-database.md`, `devices/*.md` for that) — this is the
broader engineering knowledge that came out of building
`airlock-card-mod`: game architecture facts, tooling gotchas, and
process discipline that would otherwise get re-learned the hard way on
every future mod. Written for whoever (human or Claude) picks up
modding work on this game next, including corrections to things this
project itself got wrong mid-session — the mistakes are as much the
point as the facts.

## 1. "Wired to a device" and "in that device's `LinkedDevices`" are not the same thing

`Circuitboard`/`Motherboard.LinkedDevices` (`public List<Device>`) is
**not** a general "everything reachable on my network" list. It's
populated through `AirlockControlBase.CanDeviceLink(Device device) =>
device is IAirlockDevice` (or whatever the equivalent gate is for a
different Circuitboard type) — a hard whitelist filter checked at
link time. `IAirlockDevice` is implemented only by `Console`,
`GasSensor`, `Speaker`, `ActiveVent`, `PoweredVent`, `Door`,
`WallLight` (confirmed by grepping the full decompiled source for
every implementer). A device of any other type — `AreaPowerControl`
included — **can never appear in `LinkedDevices`, no matter how it's
physically cabled.** This cost real time: a first attempt to find a
linked Power Controller scanned `LinkedDevices`, built clean, and
found nothing in-game against a correctly-wired real Power Controller
— looked like a wiring problem, wasn't.

**The real, unfiltered device list — the same one IC10 chips
themselves read from** — is `Motherboard.ParentComputer.DeviceList()`.
`ParentComputer` is a public field, type `IComputer` (the Console/
Computer structure the card is installed in), and `IComputer.DeviceList()`
returns every `ILogicable` reachable on that structure's own
`DataCableNetwork` — unfiltered by any per-Circuitboard whitelist.
This is structurally the same mechanism IC10's own `lb`/`lbn`
instructions use (`ProgrammableChip` calls the equivalent
`CircuitHousing.GetBatchOutput()` for a chip housing). **Use
`ParentComputer.DeviceList()`, not `LinkedDevices`, for any "what's on
my network" query from a mod.** `LinkedDevices` is specifically for
"what did this particular Circuitboard type accept a link to," a much
narrower and type-gated question.

## 2. Power and data are the same cable — confirmed, corrected mid-session

**Got this wrong once, corrected by the project owner, worth recording
precisely so it doesn't get re-guessed wrong again.** Stationeers uses
a single combined cable for both power and logic/data — there is no
separate "data-only" cable that can be routed independently of power
topology to bypass a power split. This was NOT obvious from the
decompiled evidence alone (`ElectricalInputOutput.InputNetwork`/
`OutputNetwork` and `IComputer.DataCableNetwork` are both typed
`CableNetwork`, which is *consistent* with either "same unified
network" or "coincidentally same C# type for two independent graphs"
— the decompiled types alone don't disambiguate this, and guessing
the wrong one is exactly what happened here initially).

**Practical consequence**: an `AreaPowerControl` splitting its
`InputNetwork` from its `OutputNetwork` splits **both power and data**
reachability across that boundary — a device wired to the power-out
side is not reachable from the power-in side's data network, full
stop, the same way a Transformer blocks everything (Transformers just
have zero data port *at all*, an even harder wall; an APC's data port
exists but only on whichever specific physical connector it's
attached to). **There is no way to run a "logic bridge" cable around
this** — the only in-game mechanism that crosses an always-on/switched
power boundary without requiring continuous power on both sides is a
wireless Logic Transmitter pair (see `devices/logic-transmitter.md`),
relaying exactly one value (`Setting`), which is exactly why this
project's own IC10 Watcher/Cycle design already uses one. **A mod
that needs to bridge the same kind of boundary should expect to need
the same kind of bridge** — don't assume a C# `DeviceList()` call can
reach across a power-switching device's own input/output split.

## 3. Some runtime facts are genuinely unrecoverable via static decompilation

Not "hard to find" — structurally absent from the compiled IL, no
matter how thorough the search. Confirmed examples: `PrefabHash`
values (assigned from Unity asset/prefab identity, not a compile-time
constant), `GameManager.CustomColors`' palette order (a
`[SerializeField] List<ColorSwatch>` populated in the Unity Editor),
`ThreadedManager.TickSpeed` (same — a public field with a code
default of `1`, but overridden per-instance in scene/prefab data).
**The tell**: the field/property is a plain value with no interesting
logic around it, and its *meaning* depends on data assigned outside
any `.cs` file — Editor-authored, not code-authored.

**The fix is a live BepInEx research dump, not more decompiling.** A
temporary Harmony patch, piggybacked on an attachment point already
proven to fire after the game is fully loaded (this project used
`AirlockControlBase.OnThreadUpdate`), that logs the real runtime value
once via `Debug.Log`, then gets deleted after capturing the answer.
See [[feedback_harmony_patch_diagnostic_technique]] (project's Claude
memory) for the full technique — it was originally developed for
verifying Harmony patch targets, and generalizes cleanly to this
different problem. `Assets.Scripts.Objects.Prefab.AllPrefabs`
(`List<Thing>`, every registered prefab) is the master registry worth
reusing for future `PrefabHash` questions specifically.

## 4. Power draw model — `Device.GetUsedPower` predicts some of this correctly, not all of it

**Real numbers, confirmed in-game by the project owner (2026-08-06),
some matching the static code trace and one genuinely not explained by
it — recorded honestly, gap included:**

| Device | Off | On | Active |
|---|---|---|---|
| Glass Door | 0W | 10W flat, regardless of open/closed/mid-cycle (confirmed no distinction at all) | — |
| LED Light | 0W | 25W | — |
| Cable Analyzer | — | 0W (see below — it's a pure passive monitor) | — |
| Gas Sensor | — | **0W observed** (wiki states ~1W) | — |
| Active Vent | 0W | — | **100W while pumping (see below — vent only exposes a binary On/Off logic state at all, no separate "pumping" flag)** |

Doors and the LED match what the static trace below predicts exactly
— a single flat draw the whole time they're on, no exceptions. **Gas
Sensor and Active Vent don't, or aren't fully explained by it** —
worth understanding as a real limit of this project's decompilation
reach, not a settled fact to build on uncritically.

**Cable Analyzer, corrected**: it always draws `0W` itself (a passive
monitor, not a load), but displays **three distinct readings** for
whatever network it's watching, project owner's own explanation —
worth understanding for any future mod touching power:
- **`Required`** — total power every device on the network is
  currently asking for (this is almost certainly what
  `Device.GetUsedPower`'s returned value feeds into, network-wide).
- **`Potential`** — what's actually available to supply right now.
- **`Actual`** — what's actually being delivered/consumed.

If `Required > Potential`, the network **browns out** (can't meet
demand, things don't get full power) as long as the shortfall stays
under the physical cable's own rating; if `Required` exceeds what the
**cable itself** can carry (a hardware rating, separate from
generation/battery capacity), it causes a **cable fault** instead —
two different failure modes for two different kinds of shortfall
(supply-side vs. wire-rating-side). **This likely explains the Active
Vent gap above**: the static `GetUsedPower` trace found a flat
`Required`-side calculation (matches "the vent always *asks* for its
full rated wattage whenever switched on"), but what the project owner
observed with a live meter was almost certainly `Actual` — and
`Actual` staying at 0 while idle-but-on, only jumping to 100W while
genuinely pumping, points at network-level demand/delivery balancing
elsewhere in the simulation (not inside `Device`/`ElectricalInputOutput`'s
own `GetUsedPower`, which is the "how much do I want" side, not the
"how much am I actually getting" side). **Confirmed (project owner):
Active Vent only exposes a binary `On`/`Off` logic state at all — no
separate "pumping" flag or `Activate` field to read** (matches
`device-index.md`'s LogicType list for it — no `Activate` there
either). So whatever decides "genuinely pumping right now" isn't even
a *readable logic value* on the vent itself, only an internal
simulation detail that happens to also gate `Actual` power draw —
**still not confirmed exactly where that logic lives**, flagged as the
more precise version of the still-open question, not fully resolved.

The base `Device` class (`Assets.Scripts.Objects.Pipes.Device`)
declares `public float UsedPower = 10f` and:

```csharp
public virtual float GetUsedPower(CableNetwork cableNetwork)
{
    if (PowerCable == null || PowerCable.CableNetwork != cableNetwork) return -1f;
    if (!OnOff || !IsStructureCompleted) return 0f;
    return UsedPower;
}
```

Neither `Door`, `ActiveVent`, nor `GasSensor` override this anywhere
in their full inheritance chains (`ActiveVent`'s traced all the way up
through `SmallDeviceOutput → DeviceOutput → DeviceAtmospherics →
Device`, `UsedPower` itself confirmed never dynamically reassigned
anywhere in that chain either — checked specifically after the vent
finding below, since `UsedPower` is a mutable field a subclass could
write to without needing to override the method at all). This predicts
a single flat draw whenever `OnOff == true` and powered, with no
built-in distinction between "actively working" and "idle" — and
that's exactly what Doors and the LED showed. **It does not explain
Active Vent's confirmed 0W‑off/100W‑pumping split** (as opposed to
0W‑off/100W‑whenever‑switched‑on‑regardless‑of‑pumping, which is what
the trace above predicts) — there must be a real mechanism gating
consumption on actual pump activity specifically, somewhere outside
every class this pass checked. **Not yet found — flagged as a genuine
open question, not swept under the "probably fine" rug.** Possible
places to look next if this matters for a future mod: network load-
balancing/`PowerTick` code (outside the `Device`/`ElectricalInputOutput`
hierarchy checked here), or a completely separate power-request path
that isn't `GetUsedPower` at all.

Gas Sensor's 0W (vs. the wiki's ~1W) is a smaller discrepancy, same
general lesson: don't trust either the wiki figure or a static
`UsedPower` field reading over an actual in-game measurement.

**Still true regardless of the above**: cutting the *upstream* switch
(an APC's own `On`) zeroes everything downstream's actual consumption
regardless of individual device state, since there's no supply to
draw from at all — this part doesn't depend on resolving the Active
Vent mechanism.

## 5. Verify every Harmony patch actually fires — a plausible-looking target can be wrong two different ways

Cross-referenced in full at
[[feedback_harmony_patch_diagnostic_technique]] — summarized here
because it's core modding discipline, not just an airlock-specific
note. Two real incidents this project hit:
- Patched `AdvancedAirlockControl.OnThreadUpdate` directly — compiled
  fine, threw `HarmonyException: Undefined target method` in-game,
  because that class inherits the override from `AirlockControlBase`
  without redeclaring it (Harmony needs the type that actually
  declares the compiled method body).
- Patched `Thing.IsOpen`'s property setter — compiled fine, decompiled
  source clearly showed it updating a door's Animator, but it
  **silently never fired**, no exception at all, because nothing in
  the real interaction flow actually calls that setter (the real path
  goes through `Thing.OnInteractableStateChanged` instead).

One failed loudly, one failed silently — neither would have been
caught by re-reading the decompiled source more carefully, since both
readings were accurate about what the method *does*, just wrong about
whether anything *calls* it. **Always add a one-time diagnostic log
and confirm it fires in-game before trusting a new patch target**,
even one that looks obviously right from decompiled source.

## 6. Toolchain notes

- `ilspycmd` (ILSpy's CLI decompiler, installed as a `dotnet` global
  tool) needs the **.NET 6 runtime** installed alongside whatever SDK
  is present — newer `ilspycmd` versions (9.x/10.x as of this
  writing) fail to install at all with a cryptic "`DotnetToolSettings.xml`
  was not found in the package" error on at least one tested SDK
  (9.0.316); **`8.2.0.7535` is the version confirmed working** on this
  setup. If hitting that install error, try pinning to this version
  before assuming something else is broken.
- For one-off lookups: `ilspycmd --list c Assembly-CSharp.dll | grep
  -i "<search term>"` to find a class, then `ilspycmd --type
  "<Full.Namespace.ClassName>" Assembly-CSharp.dll` to decompile it.
- For bulk passes across many classes at once (e.g. finding every
  class that overrides a specific set of methods): a full `ilspycmd -p
  -o <dir> Assembly-CSharp.dll` project decompile (a few seconds for
  the whole game) plus a small Node script scanning the output text
  works well and is fast — see `ground-truth-database.md`'s own
  generation process for a worked example.
- **Never commit decompiled source to the repo** — it's proprietary
  game code. Only short extracted facts and functional fragments (a
  single expression, a few lines) go into docs, written from what was
  read, not pasted wholesale. Delete decompiled `.cs` files from the
  scratchpad once their findings are extracted.
- `Assembly-CSharp.dll` lives at `<Stationeers
  install>/rocketstation_Data/Managed/Assembly-CSharp.dll`.
