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

## Requirements (confirmed 2026-08-05, not just implementation detail)

**BepInEx is a hard requirement for this mod to function.** Every piece
of actual fail-safe *behavior* this project adds (Tier monitoring, Deep
Idle, Propped-Open, the Critical-tier evacuation sequence) is C# code,
not data — and the only mechanism to inject C# logic into a running
Stationeers instance is Harmony patching, which ships inside BepInEx
(`BepInEx/core/0Harmony.dll`). There's no code-mod path in this game's
ecosystem that doesn't route through BepInEx, including the Unity-asset
path (`StationeersMods` is itself loaded as a BepInEx plugin). This
isn't a choice made along the way that could be swapped later — it's
structural. Milestone 0's native XML path is the one exception, but
it's data-only (recipes, maybe a cloned item) and can't carry any of
the behavior that's the actual point of this project.

**StationeersLaunchPad is recommended, not required** — a load-order
and installation convenience layer (same category as its role for
Re-Volt, see `database/mods.json`), not load-bearing. A player could
drop the compiled DLL straight into `BepInEx/plugins/` manually (as
`GETTING_STARTED.md` has you doing for Milestone 1) with zero LaunchPad
involved and it would work.

## Three possible build paths

0. **Native XML mod path — no code, no installs at all.** Stationeers
   has its own built-in mod format, no BepInEx/Unity/Visual Studio
   required: a folder under
   `%USERPROFILE%/Documents/my games/Stationeers/mods/<ModName>/`
   containing an `About/About.xml` manifest plus a `GameData/` folder
   of XML files that override the game's own data files (the game's
   real ones live at
   `<Stationeers install>/rocketstation_Data/StreamingAssets/Data/`,
   27 files, recipes/traders/start-conditions/etc.). **Confirmed**: this
   can change data on an *existing* item (recipe costs, etc.) — this
   project found a real example overriding `ItemKitBattery`'s recipe,
   see "Sources." **Not yet confirmed:** whether it can introduce a
   brand-new `PrefabName` that doesn't already exist, vs. only
   reconfiguring one that does. This is the one open question worth
   resolving before anything else — see Milestone 0.
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
   that already exist once BepInEx is installed. **Still required for
   the actual failsafe behavior regardless of how path 0 resolves** —
   XML modding changes data, not logic; the Tier-monitoring state
   machine has to be real code somewhere.

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

### Milestone 0 — resolve the native-XML open question (no installs, whenever you're next at the PC)

Needs nothing beyond what's already on the machine you play Stationeers
on — no downloads, no Visual Studio, no BepInEx. See
`NATIVE_XML_CHECKLIST.md` for the exact steps. Point: open the game's
own `StreamingAssets/Data/` XML files, find the entry for
`Circuitboard (Advanced Airlock)`, and see whether that entry *defines*
the item (mesh, icon, everything) or just supplies recipe costs for an
item defined elsewhere. That answer decides whether path 0 alone can
ever produce a genuinely new card, or whether it's only useful for
tweaking the vanilla one's costs. Report back the real `PrefabName` and
whatever structure you find — same as Milestone 1.5 below, this turns
a guess into a fact.

### Milestone 0.5 — test whether vanilla's Skip button already solves the trapped-player problem (no installs, in-game only)

Also needs nothing beyond the base game. See `GAP_ANALYSIS.md`'s
"Reusing vanilla's Skip instead of custom Button C hardware" section
for the full reasoning — short version: the traditional layout for
this build already puts the Console *inside* the chamber (confirmed
2026-08-05, corrects an earlier assumption in this doc that it sat
outside), so someone trapped inside already has direct UI access with
nothing extra to build. Stall a Pressurize/Evacuate phase on purpose
and confirm Skip cancels it from the Console that's already there. If
it does, this project's Button C hardware (from the original IC10
design) is likely unnecessary for the mod-card version entirely — the
override comes free from vanilla's own layout. Worth doing before or
alongside Milestone 0, same "no installs needed" category.

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

**Strategy change (this session):** rather than build a new item from
scratch, patch *extra* behavior onto the existing vanilla Advanced
Airlock Circuitboard first, prove it works, and only fork it into a
separate item afterward. Vanilla already does most of the cycling work
correctly — see `GAP_ANALYSIS.md` for exactly what it covers and what's
actually new. This let two things get written in this session without
needing your machine at all:

- **`GAP_ANALYSIS.md`** — what vanilla's Advanced Airlock Circuitboard
  already does (cycling, stall/cancel, lock persistence, Console
  Slaves) vs. what this design actually adds (Tier-based power
  monitoring, the Button C trapped-player override, Propped-Open).
- **`src/FailsafeController.cs`** — a complete, game-independent C#
  port of `watcher.ic10` + `cycle.ic10`'s Tier/Button-C/Propped-Open
  logic. No Stationeers/Unity/BepInEx dependency at all, so it didn't
  need to wait on a decompiler — it's ready to attach the moment real
  hooks are known.

### Milestone 1.5 — find the real classes (needs a decompiler, your machine only)

Once Milestone 1 works, open the game's `Assembly-CSharp.dll`
(`<Stationeers install>/rocketstation_Data/Managed/Assembly-CSharp.dll`)
in a free decompiler — dnSpy or ILSpy, either works. **`PATCH_PLAN.md`
has the exact checklist**, one item per thing `FailsafeController.cs`
needs from the real game: the Advanced Airlock Circuitboard's real
class name, its per-tick update method, and where (if anywhere) it
already exposes button input, a Power Controller reference, and
door/vent control methods.

**Report back what you find** — class names, method signatures,
whatever's visible — so the next step is real code, not a guess. I
can't do this step myself; I have no access to that DLL from this
sandbox.

### Milestone 2 — patch the existing card in place

Wire `FailsafeController` into the real vanilla class via a Harmony
`Postfix` patch (shape sketched in `PATCH_PLAN.md`, not written yet —
it needs Milestone 1.5's real names to mean anything). Test this
**as a patch on the vanilla `Circuitboard (Advanced Airlock)` item
itself** — every one a player builds gets the new fail-safe behavior,
nothing separate yet. This is deliberately the fastest path to seeing
the whole design work end-to-end in-game, at the cost of changing
vanilla's own item while testing.

### Milestone 3 — fork into its own item

Once Milestone 2 is proven working, split it off: a distinct card
(new name, own recipe) carrying this behavior, vanilla's own Advanced
Airlock Circuitboard left untouched for anyone who doesn't want it.
Whether this reuses Milestone 0's native-XML findings, `LaunchPadBooster`'s
prefab registration API, or something else entirely depends on what
those two milestones turn up — deliberately not decided yet.

### Milestone 4 — parity testing

Confirm the card's in-game behavior matches the documented IC10 design
at every Tier transition and edge case already enumerated in the
requirements doc's verification checklist.

## Sources

- Stationeers Community Wiki, "Modding:XMLMods" / "Guide (Modding)" —
  native XML mod format: mods folder location, `About/About.xml` +
  `GameData/` structure, and a real recipe-override example (search
  snippet, not a direct fetch — the live wiki pages block automated
  fetches for this project, same issue noted throughout `SOURCES.md`
  for the IC10 side of this repo):
  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <GameData xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
    <ElectronicsPrinterRecipes>
      <RecipeData>
        <PrefabName>ItemKitBattery</PrefabName>
        <Recipe>
          <Time>5</Time>
          <Energy>100</Energy>
          <Iron>0</Iron>
          <Gold>20</Gold>
          <Carbon>0</Carbon>
          <Copper>40</Copper>
          <Steel>0</Steel>
          <Uranium>0</Uranium>
          <Hydrocarbon>0</Hydrocarbon>
        </Recipe>
      </RecipeData>
    </ElectronicsPrinterRecipes>
  </GameData>
  ```
  Real game data lives at
  `<Stationeers install>/rocketstation_Data/StreamingAssets/Data/`
  (27 XML files) — this is the authoritative source to check against,
  not this write-up. The game also ships two working example mods at
  `<Stationeers install>/rocketstation_Data/StreamingAssets/`
  (`ExampleMod.zip`, `AttributesExampleMod`) — better to copy the exact
  `About.xml` schema from those directly than trust this doc's
  paraphrase of it.
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
