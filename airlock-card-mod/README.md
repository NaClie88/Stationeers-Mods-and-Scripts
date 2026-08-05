# Airlock Card Mod — Plan

Goal: a plug-and-play Circuitboard ("card") for the Console device that
replicates what `ic10-airlock/` does with 2–3 IC10 chips, as a single
hardcoded item — same idea as the vanilla `Circuitboard (Airlock)` and
`Circuitboard (Advanced Airlock)`, which are hardcoded logic, not IC10
scripts, inside the base game.

**Vanilla-first, like the rest of this repo.** This branch doesn't
assume Re-Volt or any other mod — it's a separate track from
`revolt-mod`, not built on top of it.

**This is the project owner's first modding project.** Nothing here
gets built or tested from this session — no Unity, no Stationeers
install, no game assemblies exist in this sandbox. Everything below is
either a documented, sourced fact about the toolchain, or an explicit
"unconfirmed — needs your own local check," same discipline the rest of
this project already holds itself to for IC10 LogicTypes.

## Two possible build paths

1. **Unity asset path** — `StationeersModding/StationeersUnityModdingTemplate`.
   Needed if the card gets its own custom 3D model/icon. Requires Unity
   2022.3.62f3 (must match the game's editor version) plus a Unity
   export plugin (`stationeers.modding.exporter`). Heavier setup, more
   failure points before anything is visible.
2. **BepInEx/Harmony patch path** — `StationeersModding/ExamplePatchMod`.
   No Unity. Visual Studio + three DLL references that already exist
   once BepInEx is installed. This is the recommended starting path —
   see "Milestone 1" below.

**Open question, not yet resolved:** whether a genuinely new craftable
item (distinct from the vanilla Advanced Airlock Circuitboard, not just
a Harmony-patched *behavior* on top of it) is achievable through path 2
alone, or requires path 1's Unity asset pipeline. Answering this is
Milestone 1.5.

## Milestones

### Milestone 1 — prove the toolchain (no real logic yet)

Get the **stock, unmodified** `ExamplePatchMod` template building and
loading in-game. Its `BepInEx.cs` already does everything needed for
this: a `[BepInPlugin]`-decorated class that calls `harmony.PatchAll()`
in `Awake()` and logs "Patch succeeded." Its `Patches/ExamplePatchClass.cs`
is intentionally empty — don't add anything to it yet. Success
criterion: you see the plugin's log line in the BepInEx console/log
when the game loads. See `GETTING_STARTED.md` for the exact checklist.

This deliberately tests nothing about airlocks, Circuitboards, or game
internals — only "does my build → install → load pipeline work at
all." Get this working before anything else.

### Milestone 1.5 — find the real classes (needs a decompiler, your machine only)

Once Milestone 1 works, open the game's `Assembly-CSharp.dll`
(`<Stationeers install>/rocketstation_Data/Managed/Assembly-CSharp.dll`)
in a free decompiler — dnSpy or ILSpy, either works. Find:

- The class behind `Circuitboard (Advanced Airlock)` — likely named
  something like `InternalCircuitAdvancedAirlock` or similar, but the
  exact name is unconfirmed. This is the class whose behavior we'd
  either Harmony-patch or use as a template for a new class.
- How the Console device (`ItemConsole` or similar, unconfirmed) knows
  which Circuitboard is inserted and delegates to it — this is the
  actual "plug and play" mechanism we need to hook.
- How/where new items get registered into the game's prefab/recipe
  database at startup — this answers the Milestone 1 open question
  above (whether a real new item needs Unity or not).

**Report back what you find** — class names, method signatures,
whatever's visible — so the next step is real code, not a guess. I
can't do this step myself; I have no access to that DLL from this
sandbox.

### Milestone 2 — a minimal real patch

Once Milestone 1.5 gives us real class names: the smallest possible
change that does *something* visible and airlock-related — e.g.
patching the Advanced Airlock Circuitboard's tooltip/description, or a
single behavioral tweak — before attempting the full state machine.
Same "small, verifiable step" discipline as Milestone 1.

### Milestone 3 — port the Watcher/Cycle logic to C#

The actual goal: reimplement the `ic10-airlock/watcher.ic10` +
`cycle.ic10` state machine (Tier monitoring, staged fail-safe response,
button-driven cycling, Propped-Open) as the new card's hardcoded C#
logic. This is a straightforward port once Milestone 1.5's classes are
known — the state machine itself is already fully designed and
dry-run verified in `ic10-airlock/ic10_failsafe_airlock_requirements.md`
and `ic10_airlock_code_notes.md`; this milestone is "translate it,"
not "redesign it."

### Milestone 4 — parity testing

Confirm the card's in-game behavior matches the documented IC10 design
at every Tier transition and edge case already enumerated in the
requirements doc's verification checklist.

## Sources

- `StationeersModding/StationeersUnityModdingTemplate` — Unity asset
  path template, wizard-driven setup, three Player Settings fields
  (Company Name, Product Name, Bundle version) required.
- `StationeersModding/ExamplePatchMod` — BepInEx/Harmony path template.
  Fetched directly: `BepInEx.cs` (plugin entry point, `harmony.PatchAll()`
  in `Awake()`), `ExamplePatchMod.csproj` (references `0Harmony.dll`,
  `BepInEx.dll`, `Assembly-CSharp.dll`, `UnityEngine.dll`,
  `UnityEngine.CoreModule.dll` — all via `HintPath` pointing at a
  Stationeers install folder, .NET Framework 4.7.2), and its 5-step
  rename procedure in its own README.
- `StationeersLaunchPad/LaunchPadBooster` — has a `Mod` class with
  `AddPrefabs()`/`SetupPrefabs()` for registering prefabs once the game
  data is loaded; not yet confirmed whether this covers adding a
  genuinely new item vs. only configuring existing ones. Worth
  revisiting once Milestone 1.5 is done.
- `ic10-airlock/ic10_failsafe_airlock_requirements.md`,
  `ic10_airlock_code_notes.md`, `watcher.ic10`, `cycle.ic10` — the
  design being ported.
