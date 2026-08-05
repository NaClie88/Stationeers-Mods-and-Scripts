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
   Needed only if the card needs its own custom 3D model/icon. Requires
   Unity 2022.3.62f3 (must match the game's editor version) plus a
   Unity export plugin (`stationeers.modding.exporter`). Heavier setup,
   more failure points before anything is visible.
2. **BepInEx/Harmony patch path** — `StationeersModding/ExamplePatchMod`.
   No Unity, no Unity Editor version to match. Visual Studio (or VS
   Code + the "Build Tools for Visual Studio" package, which is a much
   lighter install than the full IDE — the template is a classic
   .NET Framework project, so either works) + three DLL references
   that already exist once BepInEx is installed.

**Confirmed by project owner (2026-08-05, in-game observation, same
trust level this project already gives that source category — see
`SOURCES.md`'s "In-game confirmations by project owner" section):**
every Console Circuitboard card in-game shares one visual model —
only the name, recipe, and functionality differ between e.g.
`Circuitboard (Airlock)` and `Circuitboard (Advanced Airlock)`. That
makes path 2 the clear choice: this mod doesn't need any new art, only
a new prefab entry that points at a model the game already has,
config'd with a different name/recipe/behavior — exactly the kind of
thing Harmony (or `LaunchPadBooster`'s prefab registration API) should
be able to do without touching Unity at all.

**Still open, narrower now:** *how* to register that new prefab entry
— clone an existing Circuitboard's prefab data at startup vs. some
other registration hook. Answering this is Milestone 1.5. Unity stays
the fallback only if 1.5 turns up a hard blocker, not the default plan.

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
  database at startup, and specifically whether an existing
  Circuitboard's prefab entry (model, mesh reference, icon) can be
  cloned under a new internal name via code — this is what makes or
  breaks the "no Unity needed" plan above, now that a new model isn't
  required.

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
