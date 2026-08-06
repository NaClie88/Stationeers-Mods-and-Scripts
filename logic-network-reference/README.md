# Logic Network Reference — decompiled ground truth for LogicType read/write

## Why this exists

This project has been burned more than once by trusting community-sourced
Stationeers documentation (wiki pages, forum scripts, cross-referenced
enum lists) about what a device's LogicTypes actually do — see
`ic10-airlock/ic10_airlock_code_notes.md` for two real examples: the
Light `Setting` field that turned out not to exist at all, the
"Logic Receiver"/numbered-channel Transmitter mechanism that also
doesn't exist, and (the one that prompted this branch) a Power
Controller's `Charge` LogicType turning out to mean something
different from what a "real working script" implied. Community sources
aren't wrong on purpose — Stationeers' own in-game Logic tooltips and
the wiki are often incomplete or imprecise per-device — but this
project has repeatedly guessed wrong from them, specifically.

**The fix: read it from the actual compiled game code instead of
guessing from secondhand sources.** `Assembly-CSharp.dll`
(`<Stationeers install>/rocketstation_Data/Managed/Assembly-CSharp.dll`)
contains every device's real `GetLogicValue`/`SetLogicValue`/
`CanLogicRead`/`CanLogicWrite` implementation, decompilable with
`ilspycmd` (see [`airlock-card-mod/README.md`](../airlock-card-mod/README.md)
Milestone 1.5 for how that tool got installed and how to use it on
this machine). Ground truth, not inference.

## What this becomes

The eventual goal (project owner, 2026-08-06): a thorough,
per-LogicType explanation for every networked item in the game,
precise enough to update the Community Wiki itself, not just this
project's own scripts. This branch is that reference, built
incrementally.

## Structure

- **[`device-index.md`](device-index.md)** — the broad framework: every
  vanilla device's name and the LogicType *names* it exposes, for all
  499 devices in one table. **Community-sourced, unverified** — a
  starting skeleton for "what should I go check," not a trustworthy
  answer on its own (see that file's own header for why, and a
  concrete example of this exact source getting a device wrong). Start
  here to find what a device is called and roughly what it might
  expose; move to `ground-truth-database.md`/`devices/*.md` or
  decompile it yourself before relying on anything it says.
- **[`ground-truth-database.md`](ground-truth-database.md)** — the
  broad-but-verified middle layer: **every one of the 120 classes in
  the whole game that overrides `GetLogicValue`/`SetLogicValue`/
  `CanLogicRead`/`CanLogicWrite`** beyond `base-behavior.md`'s shared
  set, generated programmatically by scanning the full decompiled
  source (not hand-written, not community-sourced — real decompiled
  expressions, short fragments only, kept faithful to what the code
  actually does). Cross-checked against the hand-written
  `devices/power-controller.md` entry and found byte-for-byte
  identical, so the automated extraction is trustworthy, not just
  fast. If a class isn't in this file, it only exposes
  `base-behavior.md`'s shared set — confirmed, not a gap (verified for
  `Door` specifically). See that file's own header for known rough
  edges (occasional cosmetic leftovers, a few methods found but not
  fully parsed).
- **[`base-behavior.md`](base-behavior.md)** — read this first. Most
  devices in the game share one common implementation
  (`DynamicThing.CanLogicRead`/`GetLogicValue`/`CanLogicWrite`/
  `SetLogicValue`) for the "generic" LogicTypes (`On`, `Open`, `Lock`,
  `Mode`, `Color`, `Activate`, `Power`, `Error`, atmosphere reads, the
  gas `RatioX` family, `PrefabHash`, `ReferenceId`). A device only
  needs its own entry in `devices/` for what it adds or changes beyond
  this shared base — most of the "duplicate logic read/write across
  similar items" the project owner noted comes directly from this one
  shared implementation, not per-device duplication in the game's own
  code.
- **`devices/*.md`** — one file per device class (or closely related
  family), documenting only what that class overrides or adds beyond
  `base-behavior.md`. Named after the real C# class, with the in-game
  device name(s) noted at the top since they don't always match
  (`AreaPowerControl` is both "Power Controller" and "Area Power
  Controller" in-game — see that file for how this was confirmed).

## Methodology — how to add a device

**Two layers, not one.** `device-index.md` (broad, unverified,
community-sourced — see its own header for provenance/license notes)
tells you a device *probably* exposes LogicType X by name. `devices/*.md`
(narrow, decompiled, verified) tells you what X actually *means* for
that specific device — confirmed, not guessed. Adding a new
`devices/*.md` entry:

1. Find the real class name: `ilspycmd --list c Assembly-CSharp.dll |
   grep -i "<search term>"` (see `airlock-card-mod/PATCH_PLAN.md` for
   worked examples of this from the airlock mod's own investigation).
2. Decompile it: `ilspycmd --type "<Full.Namespace.ClassName>"
   Assembly-CSharp.dll`.
3. Look for overrides of `CanLogicRead`, `GetLogicValue`,
   `CanLogicWrite`, `SetLogicValue` (and their `LogicSlotType`
   siblings, for devices with inventory slots the network can read
   into, e.g. `LogicSlotType.Occupied`/`Quantity` on a printer or
   furnace). If none of these four methods are overridden, the device
   only exposes `base-behavior.md`'s shared set — worth a one-line
   note in its own file rather than skipping it silently, so it's
   clear the check was actually done.
4. For anything gated behind a `Has*State`-style capability flag
   (`HasColorState`, `HasOpenState`, etc.), note whether that flag is
   a simple field or has its own logic (some devices override it
   conditionally).
5. **Never commit decompiled source itself to the repo** — it's
   proprietary Stationeers game code. Only the extracted
   facts/explanations go into `devices/*.md`, written from what was
   read, not pasted wholesale. This matches how the airlock mod branch
   already handles this (see `airlock-card-mod/README.md`).
6. Cross-check against `SOURCES.md` and this project's other docs —
   if a finding here contradicts something documented elsewhere
   (IC10 scripts, the mod's `PATCH_PLAN.md`), flag it explicitly in
   both places rather than silently fixing one and leaving the other
   stale, the same way the Power Controller `Charge` finding was
   cross-posted to `ic10_airlock_code_notes.md` and `SOURCES.md`.

## Devices covered so far

- [`power-controller.md`](devices/power-controller.md) — `AreaPowerControl`
  ("Power Controller" / "Area Power Controller" — same device)
- [`door.md`](devices/door.md) — `Door`
- [`motherboards.md`](devices/motherboards.md) — why Circuitboards/
  Motherboards (the Advanced Airlock Circuitboard, and every other
  circuit card) **don't** use this LogicType system at all — a
  different, separate mechanism entirely.

`ground-truth-database.md` now covers another 117 classes beyond these
three hand-written entries (120 total, including these), programmatically —
worth browsing there before starting a new hand-written `devices/*.md`
entry, since the raw data may already answer the question.

## Known open question — Logic Transmitter's `Setting` field

`LogicTransmitter` (confirmed, working, in-game — this project's IC10
side depends on it directly, see `ic10-airlock/watcher.ic10`) **does**
override `CanLogicRead`/`CanLogicWrite`, but `ground-truth-database.md`'s
extraction found zero case-arms inside either — meaning those
overrides use `if`/boolean logic rather than a `switch`, the same
limitation flagged for `AreaPowerControl.CanLogicRead` in
`devices/power-controller.md`. Its base class, `LogicInputBase`,
doesn't override any of the four methods at all — so wherever
`Setting`'s actual read/write logic lives, this pass didn't find it.
Given this project's track record of wrong assumptions about exactly
this device (see "Why this exists" above — the Logic Receiver/numbered-
channel correction), this is worth a real follow-up pass: decompile
`LogicTransmitter.cs` and `LogicInputBase.cs` by hand (`ilspycmd --type`)
and trace where `Setting` is actually handled, the same way
`devices/power-controller.md`'s `CanLogicRead` note did for Power
Controller.
