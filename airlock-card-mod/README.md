# Airlock Card Mod — Plan

Goal: a plug-and-play Circuitboard ("card") for the Console device that
replicates what `airlock-ic10-scripts/` does with 2–3 IC10 chips, as a single
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

### Milestone 0.5 — CONFIRMED: vanilla's Skip button already solves the trapped-player problem (no installs, in-game only)

**Done, 2026-08-05.** Project owner ran the test in-game: stalled a
Pressurize/Evacuate phase on purpose on a vanilla Advanced Airlock,
traditional layout, and confirmed Skip cancels it from the Console
that's already inside the chamber. It works — reachable and
functional from inside, no extra hardware needed. See
`GAP_ANALYSIS.md`'s "Reusing vanilla's Skip instead of custom Button C
hardware" section for the full reasoning and what this does and
doesn't confirm — short version: this project's Button C hardware
(from the original IC10 design) is likely unnecessary for the
mod-card version entirely, the override comes free from vanilla's own
layout. Still open (Milestone 1.5 territory): whether the mod's own
`ForceEvacuate()` carries the same Skip affordance once it's actually
patched into vanilla's evacuate method — this test exercised vanilla's
naturally-stalled cycle, not a mod-triggered call, which doesn't exist
as running code yet.

### Milestone 1 — CONFIRMED: the toolchain works end to end (no real logic yet)

**Done, 2026-08-05.** Renamed the `ExamplePatchMod` template to
`AirlockCardMod` (namespace, assembly, plugin GUID, file names — see
`airlock-card-mod/AirlockCardMod/`), pointed its DLL references at the
real local Stationeers install, and built it with a freshly installed
VS 2022 Build Tools (`.NET desktop build tools` workload) — clean
build, 0 errors, first try. Installed into `BepInEx/plugins/` and
launched: BepInEx loaded it (`Loading [AirlockCardMod 1.0]`) and
`Awake()` ran Harmony's `PatchAll()` without throwing.

One wrinkle worth recording: the success line didn't show up in
`BepInEx/LogOutput.log` at first, which looked like a possible
failure. Root cause was `BepInEx.cfg`'s `[Logging.Disk]
WriteUnityLog = false` — it excludes plain `Debug.Log` calls (what the
template's `Log()` helper uses) from the disk log specifically. Unity's
own separate `Player.log`
(`%USERPROFILE%\AppData\LocalLow\Rocketwerkz\rocketstation\Player.log`)
isn't subject to that filter and had the line:
`[AirlockCardMod]: Patch succeeded`. Worth checking that file first if
a future plugin's log line ever seems to be missing from
`LogOutput.log` — it may just be this setting, not a real failure.
(The display name shown in that log line changed to `Salty's Advanced
Airlock` shortly after this test, same day — see below; the project
folder/namespace/GUID are still `AirlockCardMod` internally.)

Also confirmed: the in-game Workshop browser never lists BepInEx
plugins (it only shows Steam Workshop content) — not seeing the plugin
there is expected, not a sign anything's wrong.

**Display name updated, same day.** The plugin's player-visible name
(`pluginName` in `AirlockCardMod.cs`, plus `AssemblyTitle`/
`AssemblyProduct` in `Properties/AssemblyInfo.cs`) is now
**"Salty's Advanced Airlock"** — rebuilt and reinstalled, confirmed
via a fresh log line the same way as above. Everything else (the
project folder, `.csproj`/`.sln` file names, C# namespace/class name,
plugin GUID, git branch name) intentionally stays `AirlockCardMod` for
now — project owner's call, to avoid re-triggering the same file-lock
churn seen renaming the template folder earlier while still mid-
development. **Planned before a 1.0 publish to GitHub/Steam
Workshop:** a full rename sweep — folder, namespace, GUID, and every
doc reference — so nothing internal-facing still says `AirlockCardMod`
by release.

This milestone deliberately tested nothing about airlocks,
Circuitboards, or game internals — only "does my build → install →
load pipeline work at all." It does. See `GETTING_STARTED.md` for the
checklist that was followed.

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

### Milestone 1.5 — mostly CONFIRMED: found the real classes

**Done, 2026-08-05**, and faster than expected — turns out this step
didn't need the manual dnSpy workflow below at all. Claude has direct
shell access to this machine this session, so rather than walking
through a GUI, `ilspycmd` (ILSpy's command-line decompiler) was
installed as a `dotnet` global tool and used to decompile
`Assembly-CSharp.dll` directly and grep/read the real source. Full
findings and updated code samples are in `PATCH_PLAN.md`; short
version:

- **Real class confirmed**:
  `Assets.Scripts.Objects.Motherboards.AdvancedAirlockControl` extends
  `AirlockControlBase` extends `Circuitboard` extends `Motherboard`.
- **Real per-tick Postfix target confirmed**: `OnThreadUpdate()`, not
  `UpdateEachFrame()` — the latter stops running whenever the Console
  isn't on-screen (`IsOccluded` check), which would silently break
  failsafe monitoring the moment nobody's looking at it.
- **No vanilla equivalent exists** for a dedicated battery/Power
  Controller reference, an Area Power Controller reference, or
  physical E/I/C wake buttons — all three need new fields/wiring,
  confirmed by reading the full decompiled source rather than guessed.
- **`ButtonEmergencyOverride()`** looked like the obvious vanilla
  analog for Button C by name — decompiling both its overrides shows
  it's a genuine no-op in the shipped game. Reusing vanilla's Skip
  (Milestone 0.5, confirmed working) remains the only real override
  path that already exists.
- **How vanilla's own Skip mechanism works internally** is now
  understood in detail (`Motherboard.UseComputer(SetFlag, ...)` →
  state reassignment → the old async cycle task notices and exits on
  its own next poll) — see `PATCH_PLAN.md` for the full trace.

**Still open**: `OnThreadUpdate()`'s actual call frequency (needed to
set `TicksPerCheck`), and the door-open attachment point / cross-
network-visibility questions in `PATCH_PLAN.md` — those need more
decompiling, not in-game testing, so they're a natural next session's
work rather than something blocking Milestone 2 from starting.

<details>
<summary>Original manual-decompiler instructions (kept for reference / if working on a machine without shell access)</summary>

Open the game's `Assembly-CSharp.dll`
(`<Stationeers install>/rocketstation_Data/Managed/Assembly-CSharp.dll`)
in a free decompiler — dnSpy or ILSpy, either works (dnSpy is already
installed on this machine, `BepInEx`/`Harmony` DLLs are in its plugins
list). Browse to `Assets.Scripts.Objects.Motherboards` in the assembly
tree and start with `AdvancedAirlockControl`.

</details>

### Milestone 2 — patch the existing card in place

**First cut written 2026-08-05. Both Harmony patches CONFIRMED working
in-game after fixing two wrong-target guesses caught by testing.**
`FailsafeController` is now wired into the real vanilla
`AdvancedAirlockControl` class via two Harmony patches, both under
`airlock-card-mod/AirlockCardMod/Patches/`:

- `AdvancedAirlockFailsafePatch.cs` — `Postfix` on `OnThreadUpdate()`.
  Creates one `FailsafeController` per circuit instance (a
  `ConditionalWeakTable`, the standard Harmony pattern for attaching
  new per-instance state to an existing class), calls `UpdateTier()` +
  `ApplyTierEffects()` every `TicksPerCheck` calls (15, calibrated
  below), and logs once on first attachment. **Hit a real bug on
  first test**: patching `typeof(AdvancedAirlockControl)` directly
  threw `HarmonyException: Undefined target method` — that class
  never overrides `OnThreadUpdate` itself, it just inherits
  `AirlockControlBase`'s override, so Harmony had no compiled method
  body on the more specific type to attach to. Fixed by patching
  `AirlockControlBase` and filtering to `AdvancedAirlockControl`
  manually inside the `Postfix` (see `PATCH_PLAN.md` for the general
  rule this establishes for any future patch on an inherited-but-not-
  overridden method). **Retested clean**: `Patch succeeded`,
  `Failsafe layer attached, Tier=Normal`, no exceptions. Also
  delivered the real `OnThreadUpdate` call-rate measurement
  `PATCH_PLAN.md` flagged as unrecoverable via static decompilation —
  **~17.2ms average per call** on this machine — so `TicksPerCheck` is
  now `15` (~258ms, matching the quarter-second target) instead of a
  placeholder `1`.
- `DoorOpenPatch.cs` — first version targeted `Thing.IsOpen`'s setter;
  tested in-game (manual door cycling at the airlock) and **never
  fired**, no exception either. Traced the real call chain
  (`OnServer.Interact` → `Interactable.Interact` → `Interactable.State`'s
  setter → `Thing.OnInteractableStateChanged`) and corrected the patch
  to target `OnInteractableStateChanged` instead — see `PATCH_PLAN.md`'s
  "Where `OnDoorOpened` attaches" for the full trace. **Retested clean**:
  fired on the first door opened, and once the airlock controller was
  registered, correctly resolved a second door open through the full
  pipeline (`OnDoorOpened fired, side=Interior`) — `DoorSide` resolution
  against `ExteriorAirlock`/`InteriorAirlock` works as designed.

**Deliberately still a no-op behaviorally**, by design, not by
accident: `AdvancedAirlockControlHost.cs` implements `IAirlockHost`
with every optional member at its documented safe default (see each
member's own doc comment in `src/FailsafeController.cs`) — no
dedicated battery, buttons, downstream APC, presence sensors, or
temperature sensor wired to anything real yet, none of those have a
confirmed vanilla hook. `DedicatedBatteryChargeRatio` hardcoded to
`100` means Tier can never leave `Normal`, so `ApplyTierEffects()`
only ever calls `SetWarningIndicator(Normal)` and
`SetDownstreamPower(true)` — both currently silent no-ops on the host
side. **The point of this first cut is proving the two Harmony
attachments themselves run cleanly in-game** — actually firing every
tick, not crashing, not interfering with vanilla's own cycling —
before spending effort wiring real sensors/buttons on top. Testing
this needs a save with an Advanced Airlock Circuitboard actually
built and installed (`OnThreadUpdate()` only runs on existing
instances); watch the BepInEx log for `Failsafe layer attached,
Tier=Normal` and the `OnThreadUpdate avg interval` line.

Patched directly onto the vanilla `AdvancedAirlockControl` class
itself, not a separate item yet — every Advanced Airlock Circuitboard
a player has already built gets this. Deliberately the fastest path
to proving the whole attachment works end-to-end, at the cost of
changing vanilla's own class while testing.

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

## Roadmap to 1.0 (spans branches — agreed with project owner 2026-08-06)

Two separate 1.0s: the IC10-only path (no mod required, `main`) and
this mod. Agreed order, next session onward:

1. **Close out IC10 loose ends first, on `main`.** Status as of
   2026-08-06: the Charge/Ratio bug, `brdns`/stall-handling gaps,
   Propped-Open exit ordering, and the Power Controller output-gating
   question are all resolved now (`logic-network-reference` has been
   merged into `main`, and the folder itself renamed to
   `airlock-ic10-scripts/`). What's left, per
   `airlock-ic10-scripts/ic10_airlock_code_notes.md`'s "Genuinely still
   open" list: `BtnHash`, the LED Color enum, and an in-game test of
   the real wireless Transmitter/Receiver link — all three need the
   project owner at the keyboard in-game (or re-running `ilspycmd` for
   the first two), not further research from this branch. Goal: a
   genuinely no-known-issues IC10-only 1.0.
2. **Real hardware wiring, back on this branch — IN PROGRESS.** Button
   and downstream-Power-Controller discovery are confirmed working
   in-game (2026-08-06/07). **Revised 2026-08-07**: the original plan
   to wire a second, dedicated Power Controller as a "battery reference"
   was scrapped — see GAP_ANALYSIS.md's "Power architecture" section
   for why. No custom power infrastructure needed at all: the airlock's
   always-on backbone just needs to share a network with the player's
   ordinary **Station Battery** (`ThingStructureBattery`, the normal
   base backup-power device most builds already have), and a Cable
   Analyser on that same backbone is what the mod actually reads
   (`Required > Potential` = a real brownout) — not the battery's own
   charge, which reads as artificially healthy for too long to give any
   real advance warning. See `src/FailsafeController.cs`'s
   `IAirlockHost.BasePowerBrownout`. Door/vent control (evacuate, lock,
   open a specific side) is written from decompiled evidence but not
   yet confirmed in-game — next session's first task.
3. **Full rename sweep, right before this branch's own 1.0 publish**
   to GitHub/Steam Workshop specifically — folder, namespace, `.csproj`/
   `.sln`, plugin GUID, branch name, every doc reference. Deliberately
   saved for last so it happens once, not mid-development (see
   Milestone 1's display-name-only rename, done 2026-08-05, for why
   this was split out).

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
- `airlock-ic10-scripts/ic10_failsafe_airlock_requirements.md`,
  `ic10_airlock_code_notes.md`, `watcher.ic10`, `cycle.ic10` — the
  design being ported.
