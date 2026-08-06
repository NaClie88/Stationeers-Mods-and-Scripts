# Shelved Display Ideas

Not deleted, not currently buildable as-is — filed away per project
policy (see root `README.md`'s "Repo organization": work built on a
wrong premise but still conceptually useful gets kept, not thrown
away, since the design/implementation cost is already spent). Revisit
either of these if their blocking unknown ever gets resolved.

## Idea 1: Raw-number LED Display readout (reverted 2026-08-07)

**What it was:** a `d5`-wired device showing `GasIndex` as a plain
number every tick, `Mode 0` set once at init. Exact code as it existed
before being reverted (commit `a93b095`, this repo's history):

```
alias Display d5
...
init:
...
brdns Display skipDisplayInit
s Display Mode 0
s Display On 1
skipDisplayInit:
...
loop:
...
brdns Display skipDisplay
s Display Setting GasIndex
skipDisplay:
...
```

**Why shelved, not deleted:** built against a device called "LED
Display" from a community source that itself admitted it "doesn't
specify an official in-game name." Follow-up research found the real
Community Wiki entries are titled "Kit (Consoles) LED Display
(Small/Medium/Large)" — almost certainly a card that slots into a
Console (the `ButtonCommands`/`Motherboard` system, not reachable by a
plain IC10 pin write), not a freestanding pin device. The project
owner independently confirmed seeing exactly this kind of
Console-slotted display card in an automated build. See
`two_chamber_notes.md`'s "Display" section for the full account.

**What would revive this exact approach:** confirmation that some
device — this one, or a different one entirely — genuinely accepts a
live `Setting` write from a plain IC10 pin (not routed through a
Console/Motherboard) and renders it as a number. If that's confirmed,
this code is ready to drop back in as-is.

## Idea 2: Color-coded LED, general-purpose status indicator

**Not written as code yet — a design worth keeping in reach, not just
for this script.** The Watcher chip's Tier LED
(`../../../airlock-ic10-scripts/watcher.ic10`) already proves a
Diode's `Color` field is a real, confirmed-working, freestanding
pin-wired status indicator — no Console, no card, works exactly the
way a plain LED should. The reason this wasn't built alongside the
raw-number attempt: this repo's own Color enum research only ever
confirmed 3 values (green/yellow/red, from `watcher.ic10`'s own still-
open TODO) — not enough to confidently assign a distinct color to each
of 11 gases here, and asserting a color count that isn't confirmed
would have repeated the exact mistake made with Idea 1.

**Broader than this one script:** project owner's own observation —
this is worth having "in the back pocket" for *any* future automated
system that wants a cheap, definitely-works status readout, not just
gas selection here. A Diode's `Color` field is proven infrastructure;
the only blocker is knowing how many distinct values it actually
supports.

**What would unblock this:** confirming the real `Color` LogicType's
full value range — in-game (cycle through values on a real Diode and
note where colors stop changing/repeat) or via decompilation the same
way `logic-network-reference` resolved other open questions. This
would also close the matching open item already sitting in
`../../../airlock-ic10-scripts/ic10_airlock_code_notes.md` for the
Watcher chip's own `ColorGreen`/`ColorYellow`/`ColorRed` values — one
piece of research serves both.

Once confirmed: if the real count is ≥11, gas index maps directly to
one color each. If it's fewer, a reasonable fallback is grouping gases
into color *bands* (e.g. by how demanding their condensation
conditions are) rather than 1:1 — still useful, just coarser.
