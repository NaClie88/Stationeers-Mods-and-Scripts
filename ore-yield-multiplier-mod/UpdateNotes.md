# Update Notes / Troubleshooting

**Getting a bug report from the wild?** Ask the reporter to enable
`VerboseLogging` in LaunchPad's config UI (off by default — it logs every
single smelt/melt event with exact before/after quantities, so it's noisy
by design, opt-in only), reproduce the issue, and share
`BepInEx/LogOutput.log`. This gives the same per-action diagnostic detail
this project relied on during development, without needing a special
Debug build for every report — see `SaltysYieldMultiplier.LogVerbose`.

## Design: Multiplier snapping (whole numbers ≥1, tenths below 1)
Checked LaunchPad's own config-rendering code (`ConfigPanel.cs`) before
building this: a float `ConfigEntry` without an `AcceptableValueRange`
renders as free-text (`ImGui.InputScalar`, no restriction at all); with
one, it renders as a continuous slider (`ImGui.SliderScalar`) bounded by
Min/Max, but still allows any value in between — LaunchPad has no rendering
path for `AcceptableValueList` (discrete choices) on scalar types, so a
"only these exact values" dropdown isn't achievable through its UI as it
exists today.

Given that, the actual guarantee against "the game bugging out at a weird
rounding amount" lives in code, not UI config: `SnapMultiplier(float raw)`
in `SaltysYieldMultiplier.cs` rounds to the nearest whole number at/above
1, or the nearest tenth (0.1-0.9) below 1. Every patch reads
`SaltysYieldMultiplier.IngotMultiplier` / `.IceMultiplier` (which apply
this snap) rather than the raw `ConfigEntry.Value` directly — so no matter
what a slider gets dragged to, what someone types via a text-editable
`.cfg`, or float-precision noise from dragging, the game logic only ever
sees a clean value. `AcceptableValueRange<float>(0.1f, 20f)` is still set
on both multipliers too, purely so the UI at least shows a bounded slider
instead of unrestricted free-text — a nice-to-have layered on top of the
snap, not the thing actually doing the work.
**If this needs revisiting:** the snap boundaries (whole numbers vs.
tenths, the 1.0 cutoff) are a design choice, not something forced by the
game — change `SnapMultiplier` directly if the desired granularity changes.

## Symptom: `VerboseLogging` checkbox hover makes it hard to see checked
## state (blue glow obscures the check mark)
**Confirmed out of scope for this mod.** `ConfigPanel.DrawBoolEntry` in
`StationeersLaunchPad.dll` renders every bool config as a plain
`ImGui.Checkbox(...)` — the hover highlight is Dear ImGui's own default
styling for that widget, not something this mod's `ConfigEntry<bool>`
binding controls or can influence. Nothing to fix on this mod's side; this
would need to be raised with LaunchPad itself if it's worth pursuing.

## Symptom: mod doesn't appear in LaunchPad or the in-game mod list at all
**Root cause (hit this during initial development, confirmed by decompiling
`StationeersLaunchPad.dll`):** a raw BepInEx plugin dropped into
`BepInEx/plugins/<ModName>/` gets loaded and patched by BepInEx itself (its
`Awake()` runs fine), but **LaunchPad never learns it exists**. LaunchPad's
own mod list only comes from `LocalModSource.ListMods()`
(`StationeersLaunchPad.Sources/LocalModSource.cs`), which recursively scans
`Documents\My Games\Stationeers\mods\` for `About/About.xml` files — it
never looks in `BepInEx/plugins/` at all. Since LaunchPad's entrypoint
dispatch (what calls `OnLoaded(ConfigFile config)`) only runs for mods it
has discovered this way, a `BepInEx/plugins`-only install means
`OnLoaded` is **never called** — `IngotYieldMultiplier`/
`IceGasYieldMultiplier` stay `null` forever, and the Harmony patches would
throw `NullReferenceException` the first time anyone actually smelts ore or
melts ice.
**Fix (already applied):** deploy to
`Documents\My Games\Stationeers\mods\SaltysYieldMultiplier\` instead, with
`About/About.xml` (root element `ModMetadata`) alongside the DLL. LaunchPad
auto-discovers any `.dll` recursively inside that folder
(`ModInfo.cs`: `Directory.GetFiles(DirectoryPath, "*.dll", SearchOption.AllDirectories)`)
— no explicit assembly list needed in About.xml. `build-and-install.sh`
deploys here now, not `BepInEx/plugins`.
**Do not also leave a copy in `BepInEx/plugins/`** — that risks the same
plugin GUID being loaded twice via two different loaders (BepInEx's own
chainloader scan vs. LaunchPad's separate `Assembly.LoadFrom`).

This mod hooks five things that a Stationeers game update can change
without warning: ingot yield (Furnace/AdvancedFurnace), ingot yield (Arc
Furnace), ice-melt gas yield, and a PureIce freeze/melt pair (see
`Awake()` in `SaltysYieldMultiplier.cs`). The first three are applied
independently, so a break in one cannot silently take the others down too
— check the BepInEx log for which one actually failed
(`"Ingot yield patch failed"` / `"Ice melt yield patch failed"` /
`"Arc Furnace yield patch failed"`) before assuming all are broken. The
PureIce pair is different: it's applied and rolled back *together*
(`"PureIce freeze/melt pair failed -- rolling back both..."`) — see the
dedicated section below for why a half-applied version of that pair would
be worse than neither.

Neither patch hardcodes any prefab name strings (e.g. `"ItemIronIngot"`) —
all of them work generically off the game's own recipe-matching/gas-
composition data, so a renamed prefab shouldn't break anything by itself.
What *can* break is the method signatures below.

## Symptom: ingot multiplier stopped working
**Patch target:** `Assets.Scripts.Util.IQuantityRecipeComparable.GetOutputScale(Recipe recipe)`
(a Postfix multiplying the returned scale). Called from
`Furnace.GetSmelterScale()` and `AdvancedFurnace.GetSmelterScale()`, in turn
called from `FurnaceBase.CreateIngots()`.
**Likely cause:** this method was renamed, moved, or its return semantics
changed in a game update.
**Quick fix:**
1. Re-decompile the current `Assembly-CSharp.dll` with `ilspycmd`
   (`dotnet tool install -g ilspycmd` if not already installed; already on
   this machine as of writing).
2. Find wherever `Furnace`/`AdvancedFurnace` now compute their ingot output
   count — search for `CreateIngots` and whatever it calls to decide "how
   many ingots per matched recipe portion."
3. Update `[HarmonyPatch(typeof(...), nameof(...))]` in
   `Patches/IngotYieldPatch.cs` to the new target. The multiplier logic
   itself (the `baseScale > 0f ? baseScale : 1f` handling) shouldn't need
   to change unless the "unset scale" default value also changed from 0.

## Symptom: ice-melt gas multiplier stopped working
**Patch target:** `Assets.Scripts.Objects.Items.Ore.Smelt(Atmosphere, ReagentMixture)`
(a Prefix/Postfix pair that temporarily scales `SpawnContents[i].Quantity`
around the call, then restores the original values). Covers both in-world
ice melting and melting inside a Furnace, since both route through this one
inherited method (`Ice` does not override `Smelt`).
**Likely cause:** `Ore.Smelt` was refactored, or `SpawnContents`/`SpawnGas`
stopped being how per-unit gas composition is read.
**Quick fix:** same re-decompile-and-retarget process as above, in
`Patches/IceMeltYieldPatch.cs`.
**Reference constants** (confirmed via Stationeers Community Wiki at time
of writing, cross-checked against decompiled `SpawnGas`/`Ore.Smelt`):
- 1 unit Volatile Ice → 20 mol Methane + 2 mol Hydrogen
- 1 unit Oxite → 22.5 mol Oxygen + 2.5 mol Nitrogen
If actual in-game yields no longer match `vanilla_value * multiplier` for
these, the underlying method or field moved — don't assume the multiplier
math is wrong before checking that.

## Symptom: mod doesn't load / BepInEx logs a patch error at startup
**Likely cause:** `LaunchPadBooster`'s `Mod`/`ConfigEntry`/`OnLoaded`
pattern changed between versions, or the referenced
`LaunchPadBooster.dll` at
`BepInEx/plugins/StationeersLaunchPad/LaunchPadBooster.dll` is a newer/older
build than this project compiled against.
**Quick fix:**
1. Check the BepInEx log for the exact exception.
2. Compare against the cloned `LaunchPadBooster` reference repo
   (`../LaunchPadBooster` relative to this mod, in this repo) —
   specifically `LaunchPadBooster/Mod.cs` and the `OnLoaded(ConfigFile)`
   pattern documented in its `README.md`.
3. `git pull` the LaunchPadBooster reference clone to check for upstream
   changes, and re-verify against the actual installed DLL, not just the
   README, since the README can lag behind.

## Symptom: Centrifuge output looks too generous / possible duplication
**Status: structurally impossible in the current design, verified by
decompile, not just assumed.** `Centrifuge` uses `RecyclerRecipeComparable`
(its own `Dictionary<int, ReagentMixture>`, with a hardcoded
`_recycleRatio = 0.5f` loss factor). The ingot patch targets
`IQuantityRecipeComparable`, used exclusively by `Furnace`/`AdvancedFurnace`/
`ArcFurnace` (confirmed: searched the entire decompiled assembly for every
reference to `IQuantityRecipeComparable` — only those three furnace classes
use it). These are completely separate class hierarchies with no shared
data or logic. The Arc Furnace patch (`ArcFurnaceYieldPatch`, targeting
`ArcFurnace.CreateOutput`) is likewise isolated — `ArcFurnace` and
`Centrifuge` are separate classes with no shared method, confirmed the same
way. If this ever stops being true after a game update merges
these systems, that would be a new architectural fact, not a bug in this
mod's patch — re-verify with the same decompile-and-grep approach before
assuming the mod is still safe.
**Still test after every game update anyway:** smelt a mismatched-ratio ore
into "dirty ore," run it through a Centrifuge, confirm net material loss
still holds across a couple of cycles. Cheap insurance regardless of how
confident the static analysis is.
**If it ever does fail:** set both `IngotYieldMultiplier` and
`IceGasYieldMultiplier` to `1` via LaunchPad's config UI immediately (no
rebuild needed) while investigating.

## Symptom: Arc Furnace ingot multiplier stopped working
**Patch target:** `Assets.Scripts.Objects.Pipes.ArcFurnace.CreateOutput(IQuantity orePrefab, int quantity)`
(a Prefix multiplying and re-clamping the `quantity` parameter before the
item is created). `ArcFurnace` computes its ingot output in a private
method (`DropIngots`) with no scale hook analogous to `Furnace`/
`AdvancedFurnace`'s `GetSmelterScale`/`GetOutputScale` — `DropIngots`
already clamps the ingot count to `smelterResult.GetMaxQuantity` *before*
calling `CreateOutput`, so this patch re-clamps against
`orePrefab.GetMaxQuantity` rather than letting the multiplied value exceed
the item's max stack size.
**Why this target and not a Transpiler on `DropIngots` directly:**
`CreateOutput` is public, single-purpose (confirmed via decompile: called
from nowhere else in `ArcFurnace`), and both inputs needed for correct
clamping (`orePrefab`, `quantity`) are right there as parameters — a clean
method-level Prefix, not IL pattern-matching inside a private method's
guts. Meaningfully more fragile alternatives were deliberately avoided.
**Likely cause if broken:** `CreateOutput`'s signature changed, or
`DropIngots` stopped calling it (e.g. inlined, or ArcFurnace's ingot-output
path was refactored to match `FurnaceBase`'s `GetSmelterScale` pattern —
if so, `ArcFurnaceYieldPatch` may become redundant/removable rather than
needing a fix).
**Quick fix:** same re-decompile-and-retarget process as the other
patches, in `Patches/ArcFurnaceYieldPatch.cs`.

## Symptom: PureIce (condensed atmosphere) freeze/melt looks wrong, or a
## duplication/loss loop appears possible
**Design (implemented as a matched pair, not left alone):** ice can't form
inside pipes — a pipe that would freeze bursts instead, and the freeze
happens to the atmosphere that escapes. That freeze event is
`AtmosphereHelper.CreateIcePrefab` (private), which records a frozen gas's
mole quantity onto a new `PureIce` item via `PureIce.AssignSpawnGasValues`.
Separately, `PureIce : Ice : Ore` **overrides** `Smelt()` with its own
implementation rather than inheriting `Ore.Smelt()`, so the regular
`IceMeltYieldPatch` (which targets `Ore.Smelt` specifically) never fires
for it — C# virtual dispatch sends a `PureIce` instance's `.Smelt()` call
straight to `PureIce.Smelt`'s own IL.

Two patches handle this as a pair:
- **`PureIceFreezeYieldPatch`** (Prefix on `AtmosphereHelper.CreateIcePrefab`)
  divides the recorded quantity by `IceGasYieldMultiplier` before it's
  stored — the actual liquid consumed from the world atmosphere is
  unchanged (that removal already happened earlier in
  `FreezeWorldAtmosphere`), so this just means it now takes the multiplier
  times more liquid to freeze into the same *nominal* ice quantity.
- **`PureIceMeltYieldPatch`** (Prefix/Postfix on `PureIce.Smelt`, same
  scale-then-restore shape as `IceMeltYieldPatch`) multiplies that nominal
  quantity back by the same factor on melt.

Together: `(X / multiplier) × multiplier = X` — the freeze/melt cycle nets
out to exactly the original amount. **Neither patch alone is safe** — freeze
without melt is a silent material loss; melt without freeze is a genuine
duplication exploit (cheap freeze, 5x melt). That's why `Awake()` applies
and rolls back this pair together via `PatchPureIcePairSafely`, unlike the
other three independent patches — check the log for
`"PureIce freeze/melt pair failed -- rolling back both..."` specifically;
if you see that, both patches were reverted and PureIce behaves as pure
vanilla (safe, just unboosted) until the underlying break is fixed.
**Likely cause if broken:** `CreateIcePrefab` or `PureIce.Smelt`'s
signature changed, or `PureIce` stopped overriding `Smelt` (see
`Patches/PureIceMeltYieldPatch.cs`'s comment for what that would mean).
**Verify in-game (see `TEST_PLAN.md`):** vent gas into a cold vacuum, let
it freeze into `PureIce`, melt it back, confirm the returned gas matches
the original amount you vented — not more, not less.

**Suspected pre-existing vanilla edge case — downgraded after deeper
investigation, needs in-game confirmation, see `TEST_PLAN.md` item 15d:**
`Stackable.SetQuantity(int)` does `Quantity = Mathf.Min(newQuantity, MaxQuantity)`
— a silent clamp. `PureIce.MaxQuantity` is 50 pieces per stack, and the
piece count comes from `CeilToInt(quantity / 50)` in
`PureIce.AssignSpawnGasValues`, so if that count ever exceeded 50 the
excess mass would be genuinely destroyed (not left in the environment, not
rounded into extra matter — just gone).

**However:** `CreateIcePrefab` (where this would happen) is only ever
reached through `AtmosphereHelper.RemoveStackWorthOfFrozenGas`, which caps
each chunk at exactly `MoleQuantity(2500.0)` per gas type —
`2500 = 50 pieces × 50 mol/piece`, i.e. deliberately bounded at precisely
the piece-count limit. As far as static analysis can tell, this already
prevents the clamp scenario from being reachable via normal gameplay — it
looks like the game was engineered specifically to avoid it, not that it
was missed. The one place it could still occur is float-precision at the
exact `2500.0` boundary (a chunk landing at e.g. `2500.00001` due to
rounding, pushing `CeilToInt` to 51) — narrow and hard to deliberately
trigger, not "vent a lot of gas and it happens."

This is downgraded from "known bug" to "suspected, unconfirmed" —
`PureIceFreezeYieldPatch` dividing the recorded quantity before this math
runs would make the clamp *less* likely either way, so this mod is not the
source of risk here regardless of whether the vanilla edge case turns out
to be real. Do not file or publicize a bug report for this until
`TEST_PLAN.md` item 15d actually reproduces mass loss in vanilla (mods
disabled) — the static-analysis case for it being real weakened
significantly once the chunking safeguard was found.

## Symptom: multiplier is stronger than expected (e.g. ~25x instead of 5x)
**Root cause (hit this during initial development):** LaunchPad loads mod
assemblies once to populate its pre-game config UI and again for the
actual session, calling `Awake()` on a fresh component instance each time.
Harmony happily re-applies the same postfix/prefix a second time if asked
— it does not deduplicate by content, only by exact patch identity — so an
unguarded `Awake()` stacks the multiplier on top of itself each time it
runs (confirmed in testing: BepInEx log showed `"Awake() called"` /
`"...patch succeeded"` twice in a row for a single session).
**Fix (already applied):** a static `_patched` guard in `Awake()` makes
patching idempotent regardless of how many times LaunchPad invokes it —
see `SaltysYieldMultiplier.cs`. If this regresses, check the log for
`"Awake() called"` appearing more than once *without* the following
`"Already patched by an earlier Awake() call -- skipping"` line.

## 2026-10-05: Brought into the monorepo as its own branch

The newest code (the ingot re-melt exemption, Arc Furnace pool tracking, the furnace tooltip, the PureIce pair, plus the test and investigation docs) existed only as an untracked folder in `Stationeers-Mods-and-Scripts`. The standalone private repo `NaClie88/saltys-yield-multiplier` only has the initial version. Decision (project owner): track it here like the repo's other Salty mods, on branch `yield-multiplier-mod` off `main`. Added `README.md` (developer view; `WORKSHOP_DESCRIPTION.md` stays the player view) and a `.gitattributes` keeping shell scripts LF. The Workshop description's Source link now points at this branch: the monorepo is public, the standalone repo isn't. The standalone repo got a README note pointing here; it was not deleted.
