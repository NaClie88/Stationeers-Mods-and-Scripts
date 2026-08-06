# Console UI Mod — Plan

Goal: a generic Circuitboard/Motherboard, in the spirit of the vanilla
Computer/Programming Motherboard's own code-editing screen, except the
code you write maps out a button/page UI and exposes/reads logic
values to and from the network. Gives *any* IC10 build a real Console
screen — not scoped to one project's airlock the way `airlock-card-mod`
is.

## Why this needs its own mod at all

Confirmed via `logic-network-reference` (`devices/motherboards.md`):
Console/Circuitboard UI runs through a completely separate
`ButtonCommands`/`MotherboardCommand` dispatch system —
`Motherboard`/`Circuitboard` have zero overrides of
`GetLogicValue`/`SetLogicValue`/`CanLogicRead`/`CanLogicWrite`
anywhere. An IC10 chip has no channel into that system at all, by
construction — not a gap this project can script its way around, a
hard mechanical wall confirmed by direct decompilation. The only way
around it is a mod that puts something on the *other* side of that
wall: a new Circuitboard, built in C#, that a raw IC10 build's own
logic network can actually reach.

## Status: core interaction model resolved, rendering/scope still open

This is a separate, standalone mod from `airlock-card-mod` — no shared
code, though the same disciplines apply (decompile before trusting a
LogicType assumption, keep the design layer game-independent and
testable the way `FailsafeController` is, don't build bloat past what's
asked for).

**Resolved (2026-08-06):**

- **Behavior & player input back into IC10 — one answer covers both.**
  Each configured slot on the card behaves like a device the IC10
  ecosystem already knows how to talk to: a button slot exposes
  something like `Activate`/`On`, the way a real Logic Button does; a
  readout slot accepts a numeric write the way any LogicType field
  does. A script wires to a slot exactly like it already wires to a
  physical button or LED — by pin if hardwired to the card, by
  batch/hash if wireless — no new protocol vocabulary, and reading a
  virtual button is identical to reading a real one.
- **Text — the script never transmits it.** IC10 has no string type or
  text literal (`HASH()` is one-way), so the fix isn't encoding
  workarounds like character-code streaming — that just pushes ASCII
  conversion onto the player, which is exactly the friction to avoid.
  Instead, three tiers:
  1. **Structure & behavior** — script-driven, numeric (the point
     above). Which slot does what, reads/writes which LogicType.
  2. **Static label text** — typed directly by the player, in-game, on
     the card itself, the same interaction vanilla's Sign/Note/
     Labeller already use. No ASCII, no external tool, no template
     file.
  3. **Runtime-varying text** (a status readout that needs to show
     different strings depending on script state, e.g. Tier) — the
     player pre-types the *set* of possible strings for that slot once
     via the same native UI; the script only ever writes a **number**
     at runtime to select which pre-typed option is currently shown.
     Still fully script-controlled where it matters (what's visible
     changes live) without the script ever handling characters.
  Numeric readouts (pressure, percentages, a raw Tier number) don't
  need any of this — the mod renders a number natively.

**Still open:**
- **The rendering layer.** Given a slot's structure/behavior and (for
  text) a set of pre-typed strings, how does the Console housing
  (`ThingStructureConsole` — confirmed to expose almost nothing over
  LogicTypes itself, see `device-index.md`) turn that into an actual
  on-screen UI? Likely means hooking the same
  `ButtonCommands`/`MotherboardCommand` path vanilla's own circuit
  cards use, per `motherboards.md`'s trace.
- **Scope of "any IC10 build."** Is this single-page/fixed-slot-count
  to start, with paging/nesting as a stretch goal? Keeping the first
  version small matters more here than for the airlock mod, since
  there's no existing "vanilla does this already" reference design to
  replicate — this one's being invented from scratch.

## Requirements

- BepInEx, same hard requirement as `airlock-card-mod` — see that
  project's `README.md` for why the Unity/StationeersMods path doesn't
  avoid this either.

## Not started yet

Milestones, hardware/UI mockups, and a `GAP_ANALYSIS.md`-style design
doc all come after the open questions above get worked through with
the project owner — same process `airlock-card-mod` went through
before any code got written.
