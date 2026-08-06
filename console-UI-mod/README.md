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

## Status: just started, design not yet scoped

This is a separate, standalone mod from `airlock-card-mod` — no shared
code, though the same disciplines apply (decompile before trusting a
LogicType assumption, keep the design layer game-independent and
testable the way `FailsafeController` is, don't build bloat past what's
asked for). Nothing below is decided yet — recorded here as the open
questions to work through, not a spec.

**Open questions:**
- **The UI-description protocol.** How does an IC10 script actually
  describe "here's my button layout, here are my pages" — a reserved
  LogicType/Setting convention written by the script? A block of
  `Batch`-addressed writes? Something else? This is the actual hard
  design problem — everything else is plumbing around it.
- **The rendering layer.** Once a layout is described, how does the
  Console housing (`ThingStructureConsole` — confirmed to expose
  almost nothing over LogicTypes itself, see `device-index.md`) turn
  that into an actual on-screen UI? Likely means hooking the same
  `ButtonCommands`/`MotherboardCommand` path vanilla's own circuit
  cards use, per `motherboards.md`'s trace.
- **Player input back into IC10.** A button click on the new card's UI
  needs to reach the IC10 chip's own registers somehow — direct
  reflection into the chip's memory, or a LogicType write the script
  can poll for, or something else.
- **Scope of "any IC10 build."** Is this single-page/fixed-button-count
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
