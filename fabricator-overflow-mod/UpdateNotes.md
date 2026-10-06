# Salty's Fabricator Overflow: running dev log

A running record of what was done and **why**, written as the work happens. Newest entries go at the bottom.

## Before 2026-10-05: v1.0 built and confirmed working

The design is summarized in `README.md` and in the comment header of `Patches/FabricatorOverflowPatch.cs`. The project owner confirmed in-game that the cap works: "the mod to cap fabrication ingots to 500 worked" (2026-10-04). It was confirmed loading cleanly under StationeersLaunchPad 1.0.0 on 2026-10-05 (`Insert tracking patch succeeded`, `Overflow eject patch succeeded`).

## 2026-10-05: Brought into git

Until now the mod existed only as an untracked folder. It now has its own branch (`fabricator-overflow-mod`, off `main`), following the convention of the repo's other Salty mods: `README.md`, this `UpdateNotes.md`, and a `.gitattributes` that keeps shell scripts LF (Bash refuses CRLF scripts, and `core.autocrlf` is on for this machine).
