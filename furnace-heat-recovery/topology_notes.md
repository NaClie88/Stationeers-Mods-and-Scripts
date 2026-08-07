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
    waste gas actually is, however high that is. See "Reaching high
    working-gas temperatures" below for why this may end up mattering
    more than AC staging for the big temperature lift.
- **Zero or more AC stages, in series** (2026-08-07 — see "Reaching
  high working-gas temperatures" below) — active heat-pump boost/trim
  on top of what the passive Heat Exchanger delivers, chained the same
  way players already stack ACs for large cooling gaps: each stage's
  hot output feeds the next stage's target loop.
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

## Reaching high working-gas temperatures: AC staging

Project owner's question (2026-08-07): how do we move heat into an
*already-hotter* pool — do we stack AC units in a push-pull/cascade
configuration? **Cascading is confirmed as a real, established
technique** — the Community Wiki documents players chaining multiple
Air Conditioners in series for large cooling gaps ("each air
conditioner cools the waste of the previous one... aim for one AC per
every 50°C difference"), same heat-pump mechanism working in either
direction, not cooling-specific.

**But a real, unresolved catch turned up alongside it.** The wiki lists
a single AC's output range as **-270°C to 999°C (~3K to ~1272K)** —
specific enough to read like a real per-unit ceiling, not a rough
guideline (moderate confidence, search-snippet sourced, not
decompiled). Several targets in `alloy_reference.md` sit above that:
Stellite needs 1700K, Invar up to 2000K. **Genuinely unknown from what
I could find:** when AC stages are chained, does each stage's own
`Setting` get to climb *another* ~1272K on top of what the previous
stage delivered (cascading works, same as the cooling case) — or is
999°C an absolute ceiling no AC-touched gas can ever exceed regardless
of how many stages (cascading caps out, doesn't help past that point)?
This needs an in-game test, not another web search — try pushing a
single AC's `Setting` past 999°C and see if it clamps.

**This reframes where the real temperature lift is likely coming
from.** If furnace waste gas is already hotter than 1272K on its own
(plausible — unmeasured so far), the *passive* Heat Exchanger has no
AC-style ceiling and could get the working loop close to or above
several alloy targets for free, with AC stages doing fine
regulation/trimming (the same job `air-conditioner/ac_thermostat.ic10`
already does) rather than bulk heating from near-ambient. The real
open unknown isn't "how many AC stages" so much as "how hot does
furnace waste gas actually run" — nobody's measured that yet either.

**Cross-branch flag, not yet acted on:** `phase-change-separator/
two-chamber-system/separator_ac_driver.ic10` (on `main`, already
shipped) targets **2500K for Sodium Chloride** (gas index 1). If the
999°C/1272K ceiling turns out to be real and absolute, that target is
unreachable by a single AC as currently wired — that script's
Sodium Chloride stage would just permanently pin at max output. Not
touched pending the same in-game verification; flagged here so it
isn't lost.

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
6. **Whether a single AC's `Setting` is capped around 999°C/1272K, and
   whether chaining AC stages can climb past that ceiling or not** —
   see "Reaching high working-gas temperatures" above. Needs an
   in-game test (push `Setting` past 999°C on one AC, see if it
   clamps), not more web research.
7. How hot furnace waste gas actually runs, unmeasured — decides
   whether AC staging is doing the primary heat lift or just fine
   trimming on top of what the passive Heat Exchanger already
   delivers for free.
8. **Cross-branch flag**: `phase-change-separator/two-chamber-system/
   separator_ac_driver.ic10` (on `main`) targets 2500K for Sodium
   Chloride — possibly unreachable if item 6 confirms a hard 1272K
   ceiling. Not touched yet, pending that same verification.
