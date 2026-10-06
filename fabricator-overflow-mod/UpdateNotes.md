# Salty's Fabricator Overflow: running dev log

A running record of what was done and **why**, written as the work happens. Newest entries go at the bottom.

## Before 2026-10-05: v1.0 built and confirmed working

The design is summarized in `README.md` and in the comment header of `Patches/FabricatorOverflowPatch.cs`. The project owner confirmed in-game that the cap works: "the mod to cap fabrication ingots to 500 worked" (2026-10-04). It was confirmed loading cleanly under StationeersLaunchPad 1.0.0 on 2026-10-05 (`Insert tracking patch succeeded`, `Overflow eject patch succeeded`).

## 2026-10-05: Brought into git

Until now the mod existed only as an untracked folder. It now has its own branch (`fabricator-overflow-mod`, off `main`), following the convention of the repo's other Salty mods: `README.md`, this `UpdateNotes.md`, and a `.gitattributes` that keeps shell scripts LF (Bash refuses CRLF scripts, and `core.autocrlf` is on for this machine).

## 2026-10-06: Bug hunt

No bugs found. Checked against the decompile:
- `Eject` mirrors vanilla `Thing.DropReagent(Reagent, Slot)` + `CreateReagent(Ingot, int, Slot)` line for line (`Create<Ingot>(prefab, slot.Location)`, `Quantity`, `CreatedReagentMixture`, `OnServer.MoveToSlotOrWorld`).
- The eject only runs when idle, closed, powered and server-side, with the export slot empty.
- Insert timestamps are weakly keyed, so a deconstructed printer leaves nothing behind.

Documented one design limitation in the README: a cap set *below* what a recipe needs of one ingot type would stop the printer ever holding enough to print it. The default is 500 and the config floor is 50.
