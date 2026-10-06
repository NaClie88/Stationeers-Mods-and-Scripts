# Test Plan — Salty's Yield Multiplier

One play session should cover everything below — nothing here requires a
restart except where noted. Ordered cheap/fast checks first so a real
problem shows up before you've invested time staging materials for the
deeper tests.

## Prep (stage all of this before starting, so you're not pausing mid-run)

- Furnace, Advanced Furnace, Arc Furnace, and Centrifuge all built/accessible.
- Ore stock: Iron, Copper (or any second plain metal), Gold, Silver.
- Enough Iron + Carbon (or Iron + Hydrocarbon) for a Steel recipe.
- Enough Silver + Gold for an Electrum recipe.
- Enough of one ore to exceed an ingot's max stack size in a single batch
  (for the clamp test — a big pile of Iron is easiest).
- Volatile Ice and Oxite, some to melt in-world and some to melt in a Furnace.
- A "junk" handful of mismatched ore ratios (whatever's not a real recipe)
  to produce dirty ore/Slag for the Centrifuge test.
- Empty sealed room for the gas-measurement tests (matches what you used
  for the gold ore test).

## A. Startup sanity (do first — fail fast)

1. [x] Mod appears in LaunchPad's mod list, enabled.
2. [x] `IngotYieldMultiplier`, `IceGasYieldMultiplier`, and
   `VerboseLogging` all appear in LaunchPad's config UI (multipliers
   default `5`, `VerboseLogging` defaults `false`), all editable. Try
   toggling `VerboseLogging` on and confirm per-action log lines (e.g.
   `"IngotYieldPatch fired: ..."`) start appearing on the next smelt —
   this is the hook we'd ask a bug reporter to enable, so worth confirming
   it actually works now rather than discovering it doesn't when it's
   needed for real.
3. [ ] Drag a multiplier slider to an odd in-between value (e.g. 3.7) and
   smelt something. Confirm the actual effect matches the *snapped* value
   (3x here), not the raw slider position — check the log's
   `"Config bound: ... (snapped=...)"` line or a verbose per-action log
   line to see both the raw and snapped numbers side by side. This is
   what actually prevents odd-rounding behavior (see UpdateNotes.md
   "Multiplier snapping") — the slider itself can't be dragged to only
   discrete steps, LaunchPad's UI doesn't support that, so the snap-on-use
   guarantee is what matters here, not what the slider visually shows.
4. [ ] (FYI, not actionable) The `VerboseLogging` checkbox glows blue on
   hover, obscuring whether it's checked. Confirmed via decompile this is
   Dear ImGui's own default checkbox hover styling in LaunchPad's config
   renderer (`ConfigPanel.DrawBoolEntry` calls plain `ImGui.Checkbox`) —
   entirely outside this mod's control, nothing to fix here.
5. [ ] Check `BepInEx/LogOutput.log`: `"Awake() called"` /
   `"...patch succeeded"` (x3) appears once cleanly. If `"Awake() called"`
   appears a second time, confirm it's immediately followed by
   `"Already patched by an earlier Awake() call -- skipping"` — that's the
   idempotency guard working as intended, not a bug.

## B. Ingot yield — basic correctness

6. [x] Smelt Iron in the **basic Furnace** → confirm 5 ingots per what
   vanilla would produce as 1.
7. [ ] Same Iron test in the **Advanced Furnace**.
8. [x] Same test with a *different* ore (e.g. Copper) in the **Arc
   Furnace** — you already confirmed Gold → 5 ingots; this cross-checks
   it wasn't ore-specific.

## C. Multi-recipe / multi-ingredient correctness (higher-risk edge cases)

9. [x] Smelt a **Steel** recipe (Iron+Carbon or Iron+Hydrocarbon) — Steel
   has three separate vanilla recipes; confirm 5x applies regardless of
   which one matched. **Confirmed:** 150 Iron + 50 Coal (3:1, matching the
   Iron+Hydrocarbon recipe's 0.75:0.25 ratio exactly) → vanilla would be
   200 steel (150÷0.75 = 50÷0.25 = 200), got 1000 = 200×5. Matches the
   log's `scale=5` on that exact recipe.
10. [ ] Smelt an **Electrum** recipe (Silver+Gold, a two-ingredient
    recipe) — confirm 5x applies correctly, not just to single-element
    recipes like Iron.

## D. Stack-size clamp edge case

11. [ ] Smelt a big enough batch of one ore that the *un-multiplied*
    output would already be close to (or exceed) that ingot's max stack
    size. Confirm the multiplied output clamps cleanly at the max stack
    size rather than overflowing into an invalid quantity or erroring.

## E. Ice-melt gas yield

12. [ ] Melt Volatile Ice **in-world** (not in a furnace). Check the log
    for the per-gas before/after mol values (Methane, Hydrogen) and
    confirm `after ≈ before × 5`. Cross-check the before values against
    `REFERENCE_TABLES.md` (currently wiki-sourced/unconfirmed for Ice —
    this test is what confirms them against this actual game build).
13. [ ] Melt Volatile Ice **inside a Furnace** — same check, confirm it
    matches the in-world result (both should route through the same
    patched method).
14. [ ] Melt Oxite (Oxygen/Nitrogen) — same before/after check.

## F. Regular-ore trace gas (confirmed side effect, not a bug)

15. [ ] In the same sealed room, smelt a known quantity of one plain ore
    (repeat of the Gold test, but now with the detailed per-gas log
    available) and confirm the room's measured gas increase matches
    `vanilla_trace_value × 5` from the log, not some other multiple.
    Iron/Copper/Coal are already confirmed in `REFERENCE_TABLES.md` —
    Gold/Silver/others still need a pass with `VerboseLogging` on to fill
    in that table.

## G. Freeze/thaw consistency check (do not skip — same category as H)

16. [ ] Vent a known quantity of gas into a cold vacuum room until it
    visibly freezes into `PureIce` (condensed-atmosphere ice — note it
    can't form inside pipes; a pipe that would freeze bursts and the
    freeze happens to the released atmosphere instead). Melt that
    `PureIce` back. Confirm the gas released **matches the original
    vented amount** — not 5x more, not 1/5th less. The log's
    `PureIceFreezeYieldPatch`/`PureIceMeltYieldPatch` lines show the exact
    before/after numbers on each side to check the math against.
    If it comes back at 5x (duplication) or 1/5th (loss), the freeze/melt
    pair didn't apply symmetrically — check the log for
    `"PureIce freeze/melt pair failed -- rolling back both..."` and see
    UpdateNotes.md's PureIce section.
17. [ ] Repeat with a deliberately **small** vented amount (just enough
    to form roughly 1 unit of ice under vanilla rules, not a big batch).
    Confirm it still forms exactly 1 `PureIce` item (not zero, not
    silently dropped) and still melts back to the original amount.
18. [ ] (FYI, not actionable) No dedicated session needed for the
    suspected 50-piece stack-clamp edge case — passive monitoring via a
    log hook instead. `PureIceFreezeYieldPatch` logs an **always-on
    warning** (not gated by `VerboseLogging`) any time it computes a piece
    count that would exceed 50. Just keep an eye out in
    `BepInEx/LogOutput.log` for a line starting `"WARNING: PureIce freeze
    of..."` while doing your regular testing. If it ever appears, note the
    exact quantity, verify the actual mass loss in-game, and only then
    update `BUG_REPORT_DRAFT_pure_ice_stack_clamp.md` with real numbers
    and consider filing it.

## H. Anti-exploit check (do not skip this one)

19. [ ] Smelt a deliberately mismatched ore ratio to produce dirty
    ore/Slag. Run it through a **Centrifuge**. Confirm you get back
    *less* usable material than went in — the vanilla loss is still
    present.
20. [ ] Repeat the smelt→Centrifuge cycle once more. Confirm no net
    material gain across the two cycles combined.

## I. Live config + toggle safety

21. [ ] While still in-game, change `IngotYieldMultiplier` from 5 to a
    different value via LaunchPad's config UI (no restart). Smelt again
    and confirm the *new* value applies immediately.
22. [ ] Set both multipliers to `1` (or disable the mod) via LaunchPad's
    UI. Confirm the game keeps running normally with vanilla output, no
    errors. Re-enable/reset to `5` and confirm it resumes working.

## J. Regression check (only if convenient — not blocking)

23. [ ] If you have quick access to a Droid or Zrilian character, confirm
    respawn still grants the normal vanilla kit (battery / suit). This
    mod never touches that code path by design, but it's a five-second
    check for the regression this whole project started from.

---

Report back per-item as you go (or in batches) rather than saving it all
for the end — if something's wrong early (section A or B), no point
staging the rest of the materials for later sections until it's fixed.
