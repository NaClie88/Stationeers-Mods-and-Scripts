# Furnace Waste-Heat Recovery — Topology & Design Notes

**Status: topology design phase, no code written yet.** Project owner's
own instruction (2026-08-07): figure out the device topology before
writing any script. This doc records that discussion; see
`alloy_reference.md` for the temperature/pressure table backing the
control-scheme decisions below.

## Goal

Capture a furnace's waste gas, recover its heat instead of losing it,
use that recovered heat to run the furnace with reduced or zero fuel,
and route the cooled waste gas into the existing
`phase-change-separator/` scripts to harvest condensable byproducts
(Pollutant first, per that folder's own existing research) instead of
just venting it.

Confirmed as a real, established player technique (not something being
invented from scratch) — multiple independent Steam Community threads
describe furnaces running on recycled exhaust heat with no fuel, using
isolated tanks and a dedicated Heat Exchanger, once the loop is
established. Search-snippet confidence, not decompiled — worth an
in-game cross-check, same caveat as everywhere else in this doc.

## Devices

- **Furnace** (or Advanced Furnace) — the smelting reaction chamber.
  **Unconfirmed whether it exposes `Temperature`/`Pressure` as readable
  LogicTypes directly** — couldn't pin this down via search. If it
  doesn't, fall back to an external Gas Sensor plumbed into its gas
  volume, same pattern already used for the phase separator's chamber.
  Check Stationpedia in-game before committing to either path.
- **Heat Exchanger** — Kit (Counterflow) or Kit (Direct), both real
  devices, two independent pipe networks with no gas mixing. **Both
  work for this design** (project owner, 2026-08-07) — the decision is
  rate of heat exchange vs. materials cost, a build-time tradeoff, not
  a correctness question. **Reminder: set both up in-game for real
  testing once at the workstation**, per project owner's request —
  this doc is the place that reminder lives until that happens.
  - Hot side: furnace waste gas, drawn out by the exhaust pump below.
  - Cool side: the Nitrogen working-gas loop, returning from wherever
    it gave up its heat.
  - **Passive — no ceiling of its own.** Unlike the AC stages below, a
    Heat Exchanger just equalizes toward whatever temperature the
    waste gas actually is, however high that is.
- **One or more AC units, active second stage** (corrected 2026-08-07
  — see "Reaching high working-gas temperatures" below) — **wired
  backwards from the first draft of this doc.** The AC's *controlled*
  side (`Setting`/`TemperatureOutput`) goes on the **furnace waste
  gas**, driven toward a cool target well within its real ~999°C/1272K
  ceiling. The AC's *other*, uncapped side goes on the **N2 working
  loop** — it just accumulates whatever heat gets pulled off the waste
  gas, no ceiling of its own. Multiple units in series may still help
  move more total heat faster (throughput), not for climbing past a
  temperature ceiling.
- **Nitrogen working-gas loop** — see "Why Nitrogen" below. Needs:
  - A **hot reservoir tank** (heated N2, post-heat-exchanger) —
    mole-capped for safety, see "Mole-based safety regulation" below.
  - A **cold reservoir tank** (unheated N2, never routed through the
    heat exchanger) — the cooling-injection supply. Deliberately kept
    as the *same* gas as the hot side (project owner, 2026-08-07:
    "lets keep using N2") rather than introducing Pollutant into the
    furnace control loop — one gas to source and reason about, and
    Pollutant's easy-liquefaction property (useful for harvesting) is
    exactly the property that makes it a liability inside a gas
    control loop (see "Why Nitrogen" below).
- **Pipe Analyzer** — confirmed by project owner (2026-08-07) to expose
  `TotalMoles`. This is the primary source for the mole-cap safety
  logic. Kit (Tank)'s own logic support for `TotalMoles` is
  wiki-undocumented/unconfirmed — plausible it has it too, worth
  testing, but don't depend on it; the Pipe Analyzer path is confirmed
  and the locally-computed `PV=nRT` fallback (see below) doesn't depend
  on either.
- **Three actuators into the furnace's gas line** (project owner,
  2026-08-07 — the configurable control scheme):
  1. **Hot-N2 valve/pump** — injects heated N2, raises furnace
     temperature.
  2. **Cold-N2 valve/pump** — injects cold N2, lowers furnace
     temperature *and* raises pressure (it's still adding moles).
  3. **Exhaust pump** — pulls gas out of the furnace, lowers pressure.
     This is the same draw that feeds the heat exchanger's hot side —
     the furnace's own pressure-relief step doubles as the waste-heat
     source, nothing duplicated.
- **A Logic Dial** (or similar) — selects the target alloy, same
  index-into-lookup-table idiom the phase separator already uses for
  gas selection. Looks up that alloy's `[MinTemp,MaxTemp]` and
  `[MinPressure,MaxPressure]` from `alloy_reference.md`'s table.

## Reaching high working-gas temperatures: AC staging (corrected 2026-08-07)

Project owner's question: how do we move heat into an *already-hotter*
pool — do we stack AC units in a push-pull/cascade configuration?
**Cascading is a real technique**, but project owner's own correction
(2026-08-07, hands-on experience) resolves the open question from the
first pass more directly than any web source could:

**The ~999°C/1272K ceiling only applies to the AC's own controlled
side — `Setting`/`TemperatureOutput`, the pin `ac_thermostat.ic10`
already drives. It does NOT apply to the AC's other pipe connection.**
An AC is a two-network heat pump: it drives one side toward `Setting`
(capped ~999°C/1272K) and moves whatever heat that takes into or out
of the *other* side to make it happen — and that other side has no
equivalent cap, it just accumulates however much heat gets dumped into
it. Project owner's own example (illustrative numbers, not exact):
controlled side arrives at 100°C, `Setting`=60°C, the ~40°C difference
the AC pulls out gets added to the other pipe, which exits hotter by
roughly that same amount.

**This flips the design, and simplifies it.** Don't put the N2 working
loop on the AC's *controlled* side trying to push `Setting` up toward
1700-2000K — that's the side with the real ceiling, and it tops out
well below Stellite's floor. Instead:

- **Furnace waste gas → AC's controlled side**, `Setting` driven
  toward some cool target (near-ambient, e.g. ~300K) — comfortably
  inside the ~999°C ceiling in either direction, since cooling never
  approaches that limit to begin with.
- **N2 working loop → the AC's other, uncapped side.** Whatever heat
  gets pulled out of the waste gas lands here. No ~1272K ceiling — the
  achievable temperature is just however much total heat gets moved,
  which scales with how aggressively the waste gas is cooled and how
  much flow passes through, not a fixed per-unit maximum.

**This also converges two goals that looked separate before**: cooling
the waste gas harder to extract more heat for N2 is *also* exactly
what helps the phase-separator handoff — a colder waste gas condenses
Pollutant more readily, per this project's own condensation data.
Pushing the AC's controlled side colder isn't a tradeoff against the
downstream separator step, it directly improves it.

**Updated device picture**: the passive Heat Exchanger can still do a
free first-pass equalization; the AC becomes the active second stage
that squeezes further heat out of the waste gas past where passive
exchange plateaus, landing all of that extra heat on N2's uncapped
side. Chaining multiple ACs may still matter for *throughput* (moving
a lot of heat fast enough, per the "~50°C per unit" efficiency
guidance found earlier) — but no longer for climbing past a hard
temperature ceiling, since the side this design actually needs to get
hot doesn't have one.

### AC efficiency vs. temperature differential — real, but curve unconfirmed

Project owner's follow-up (2026-08-07): AC units also have a real
limitation on how much heat each one can move efficiently, remembered
as roughly a 40° gap before major inefficiency sets in. **Confirmed
real** — the AC exposes three efficiency multipliers on its own
in-game info panel: **OTE** (Operational Temperature Efficiency),
**TDE** (Temperature Differential Efficiency), **PE** (Pressure
Efficiency). If any hits near zero, the AC does essentially nothing.
TDE specifically is the input-vs-waste temperature gap — and can
exceed 100% when the gap is already working in the AC's favor, so
it's a real curve, not a flat cutoff. Community guidance: **~50°C per
unit** as a sizing rule of thumb (close to the ~40° remembered here),
with one patch note mentioning the useful curve was extended out to
"~200 degrees difference."

**The exact formula/curve shape is NOT confirmed** — wiki pages and a
Steam Workshop code listing that might have had it are both blocked
in this environment the same way they've been all session, and
there's no decompiled assembly available here to check directly.
This one genuinely needs in-game observation (the OTE/TDE/PE readouts
are visible on the unit itself) rather than more web research.

**Sizing implication worth flagging now, before any build happens:**
if furnace waste gas arrives a few hundred K above whatever cool
target the controlled side is driven toward, bridging that *entire*
gap through AC stages alone at ~40-50°C per unit could mean a large
number of units — a real materials/space cost, not a minor detail.
This reinforces leaning on the **passive** Heat Exchanger (no
differential penalty, since it's not an active pump) for the bulk of
the temperature drop, with AC stages reserved for the final trim past
where passive exchange plateaus, rather than trying to cover the
whole gap with AC units. Exact stage count depends on the still-
unmeasured furnace waste-gas temperature (see Open Items) and the
still-unconfirmed efficiency curve above — not computable yet, flagged
so it isn't assumed away.

**Cross-branch flag, sharper now than the first pass:** `phase-change-
separator/two-chamber-system/separator_ac_driver.ic10` (on `main`,
already shipped) targets 2500K for Sodium Chloride by driving `Self
Setting`/`Self TemperatureOutput` — i.e. the AC's *controlled* side,
the one with the real ceiling. Under this corrected model that still
looks like a real problem for that one gas (not the whole script),
not a false alarm — the chamber's heat-exchange loop may need to be
on the AC's *other* side for high-temperature targets, the same fix
this furnace design just found for itself. Not touched yet, still
wants in-game confirmation, but now a concrete lead rather than a
vague "might be capped."

## Why Nitrogen (confirmed, 2026-08-07)

Project owner's original guess was Pollutant, for a real reason worth
recording precisely since it came up twice in this discussion: **picked
because it liquefies easily, which makes it easy to collect again** —
correct, and exactly why it's already the phase separator's default
target gas. But that same property is a liability *inside* a gas
control loop specifically — if any leg of a hot/cold N2-style loop
dipped into Pollutant's condensation range, that's liquid slugging a
gas pump/pipe, a malfunction. Nitrogen was picked instead because,
per `phase-change-separator/condensation_reference.md`'s own primary-
source data, it needs ~190K even at 6000 kPa to condense — the most
condensation-resistant common gas this project has documented, plus
fully inert (won't react/ignite) and abundant. Specific heat capacity
(moderate-confidence, community-sourced, not decompiled) is
respectable but not exceptional (~20.6 J/mol·K vs. CO2's ~28.2) —
stability was weighted over raw heat capacity, since the gap between
candidates was modest to begin with. **Confirmed, final: "lets keep
using N2"** (project owner, 2026-08-07).

## Control scheme (configurable, not fixed-hot)

**Deliberately not just "run the hottest gas available"** (project
owner, 2026-08-07) — `alloy_reference.md` shows real per-alloy ceilings
(Hastelloy/Waspaloy cap at 1000K, Inconel at 1250K) that a loop tuned
for Stellite (1700K+) would blow straight through. Instead:

1. Dial selects target alloy → look up its temp/pressure window.
2. Read current furnace Temperature/Pressure.
3. Temperature too low → open hot-N2 valve.
4. Temperature too high → open cold-N2 valve (also raises pressure —
   expected, not a side effect to fight).
5. Pressure too high → run the exhaust pump (works regardless of which
   temperature branch fired above; the vented gas heads to the heat
   exchanger either way).
6. **Open design question, not yet resolved:** pressure too low while
   temperature is already within its window — neither hot-N2 nor
   cold-N2 injection is "correct" on temperature grounds alone in that
   case, since both add moles/pressure but pull temperature in
   opposite directions. Needs a real decision once this becomes code
   (e.g. split the injection to keep temperature centered in its
   band, or default to whichever side is closer to their tolerance
   edge) — flagging now rather than picking arbitrarily.

## Mole-based safety regulation

Same principle as the airlock/phase-separator scripts' existing
hysteresis patterns, applied to gas moles instead: cap the working-gas
reservoir tanks by **moles**, not live pressure, so the tank stays safe
even once the gas reaches its hottest expected temperature — pressure
measured cold doesn't tell you what it becomes once hot.

```
n_max = (P_safety_ceiling * V_tank) / (R * T_max_working)
```

`kPa * L = J` exactly, so this works directly in-game units with no
conversion: `P` in kPa, `V` in L, `R = 8.314 J/(mol*K)`, `T` in K.

- `T_max_working` — now grounded at **~1700K+ (Stellite's floor)**,
  target design ceiling probably ~1800-2000K for margin — see
  `alloy_reference.md`.
- `P_safety_ceiling` — recommend well under the ~60,795 kPa burst limit
  found for pipes/tanks (moderate-confidence, search-sourced) — target
  50-60%, not the ~80% audible-stress threshold, for real margin.
- `V_tank` — fixed per whichever tank gets built (e.g. ~50,000 L for a
  Large Tank, moderate confidence, worth an in-game check).

Read moles from the **Pipe Analyzer's `TotalMoles`** (confirmed,
2026-08-07). Once computed/read moles approach `n_max`, stop importing
more gas into that tank rather than venting it — venting a hot working
gas is wasteful; just hold off adding more until it's drawn down by the
furnace or cools.

## Open items before this becomes code

1. Furnace's own `Temperature`/`Pressure` LogicType support —
   unconfirmed, check Stationpedia in-game.
2. Kit (Tank)'s `TotalMoles` support — unconfirmed, worth testing, not
   depended on.
3. Heat Exchanger type (Counterflow vs. Direct) — both work
   functionally; pick by materials cost / exchange rate once tested
   in-game (reminder tracked above).
4. The pressure-low/temperature-fine control case (#6 above) — needs a
   real decision, not yet made.
5. Exact peak achievable loop temperature depends on the real build
   (furnace exhaust temp, heat exchanger efficiency) — `T_max_working`
   above is a design floor grounded in the alloy table, not yet a
   measured real number.
6. **Resolved (2026-08-07, project owner):** the ~999°C/1272K ceiling
   only applies to the AC's controlled side (`Setting`/
   `TemperatureOutput`); the other pipe connection has no cap. N2 goes
   on that uncapped side, waste gas on the controlled side — see
   "Reaching high working-gas temperatures" above. Still worth an
   in-game sanity check of the general mechanism, but no longer an
   open question blocking the design.
7. How hot furnace waste gas actually runs, and how much of it can be
   drawn through per tick, unmeasured — now the real driver of how
   fast N2 heats up on the uncapped side (not a ceiling question
   anymore, a throughput one).
8. **Cross-branch flag, deprioritized (2026-08-07, project owner):**
   `phase-change-separator/two-chamber-system/separator_ac_driver.ic10`
   (on `main`) targets 2500K for Sodium Chloride by driving the AC's
   *controlled* side — under the corrected model above, that's still
   very likely unreachable as currently wired. But Sodium Chloride's
   freezing point (~600K, `condensation_reference.md`) is a dramatic
   outlier versus every other gas in that table (next highest is
   ~274K) — it solidifies well above where the other 10 gases would
   even still be liquid, so a setup built around their operating range
   probably never keeps it liquid long enough to matter in practice.
   Real bug, low practical impact — not worth fixing ahead of things
   that actually get used. Left as-is; see `condensation_reference.md`
   for the same note added there.
9. **AC efficiency-vs-differential curve (OTE/TDE/PE) — real, exact
   shape unconfirmed.** See "AC efficiency vs. temperature
   differential" above. Needs in-game observation of the AC's own
   info panel at various differentials, not more web research — wiki
   and a Workshop code listing that might have had the formula are
   both blocked in this environment. Directly determines how many AC
   stages the design actually needs, which isn't computable until
   this and item 7 (furnace waste-gas temperature) are both measured.
